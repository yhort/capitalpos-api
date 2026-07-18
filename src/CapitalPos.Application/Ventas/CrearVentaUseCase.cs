using CapitalPos.Application.Clientes;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed class CrearVentaUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IPuntoVentaRepository _puntoVentaRepository;
    private readonly IStockProductoRepository _stockRepository;
    private readonly IVentaRepository _ventaRepository;

    public CrearVentaUseCase(
        IVentaRepository ventaRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IClienteRepository clienteRepository,
        IStockProductoRepository stockRepository,
        IPuntoVentaRepository puntoVentaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _clienteRepository = clienteRepository;
        _stockRepository = stockRepository;
        _puntoVentaRepository = puntoVentaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<Venta> EjecutarAsync(
        CrearVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();
        var canalVenta = NormalizarCanalVenta(request.CanalVenta);

        var empresaId = _empresaActiva.EmpresaId;
        var puntoVenta = await ObtenerPuntoVentaAsync(empresaId, request.PuntoVentaId, cancellationToken);
        await ValidarClienteAsync(empresaId, request.ClienteId, cancellationToken);

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            throw new ArgumentException("La venta debe tener al menos un detalle.", nameof(request));
        }

        var ventaId = Guid.NewGuid();
        var detalles = new List<VentaDetalle>();
        foreach (var detalleRequest in request.Detalles)
        {
            await ValidarProductoAsync(empresaId, detalleRequest, cancellationToken);
            detalles.Add(detalleRequest.CrearDetalle(empresaId, ventaId));
        }

        var stocksADescontar = await ValidarStockAsync(empresaId, detalles, cancellationToken);

        var total = detalles.Sum(detalle => detalle.Total);
        var igv = detalles.Sum(detalle => detalle.Igv);
        var subtotal = total - igv;
        var venta = new Venta(
            ventaId,
            empresaId,
            request.Fecha ?? DateTimeOffset.UtcNow,
            subtotal,
            igv,
            total,
            detalles,
            puntoVenta.SedeId,
            puntoVenta.Id,
            request.ClienteId,
            canalVenta,
            request.VendedorId);

        try
        {
            foreach (var stockADescontar in stocksADescontar)
            {
                stockADescontar.Stock.Descontar(stockADescontar.Cantidad);
                await _stockRepository.GuardarAsync(stockADescontar.Stock, cancellationToken);
            }

            await _ventaRepository.AgregarAsync(venta, cancellationToken);
        }
        catch
        {
            foreach (var stockADescontar in stocksADescontar)
            {
                stockADescontar.Restaurar();
            }

            throw;
        }

        return venta;
    }

    private async Task ValidarClienteAsync(
        Guid empresaId,
        Guid? clienteId,
        CancellationToken cancellationToken)
    {
        if (clienteId is null)
        {
            return;
        }

        if (clienteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del cliente no puede estar vacio.", nameof(clienteId));
        }

        var cliente = await _clienteRepository.ObtenerPorEmpresaAsync(
            empresaId,
            clienteId.Value,
            cancellationToken);
        if (cliente is null)
        {
            throw new InvalidOperationException("El cliente no pertenece a la empresa activa.");
        }
    }

    private async Task<PuntoVenta> ObtenerPuntoVentaAsync(
        Guid empresaId,
        Guid puntoVentaId,
        CancellationToken cancellationToken)
    {
        if (puntoVentaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta es obligatorio.", nameof(puntoVentaId));
        }

        var puntoVenta = await _puntoVentaRepository.ObtenerPorEmpresaAsync(
            empresaId,
            puntoVentaId,
            cancellationToken);
        if (puntoVenta is null)
        {
            throw new InvalidOperationException("El punto de venta no pertenece a la empresa activa.");
        }

        if (!puntoVenta.Activo)
        {
            throw new InvalidOperationException("El punto de venta no esta activo.");
        }

        return puntoVenta;
    }

    private static CanalVenta NormalizarCanalVenta(string? canalVenta)
    {
        if (string.IsNullOrWhiteSpace(canalVenta))
        {
            return CanalVenta.TIENDA;
        }

        if (Enum.TryParse<CanalVenta>(
            canalVenta.Trim(),
            ignoreCase: true,
            out var canalNormalizado) &&
            Enum.IsDefined(canalNormalizado))
        {
            return canalNormalizado;
        }

        throw new ArgumentException("El canal de venta no es valido.", nameof(canalVenta));
    }

    private async Task ValidarProductoAsync(
        Guid empresaId,
        CrearVentaDetalleRequest detalleRequest,
        CancellationToken cancellationToken)
    {
        var producto = await _productoRepository.ObtenerPorEmpresaAsync(
            empresaId,
            detalleRequest.ProductoId,
            cancellationToken);
        if (producto is null)
        {
            throw new InvalidOperationException("El producto no pertenece a la empresa activa.");
        }

        if (detalleRequest.ProductoVarianteId is null)
        {
            return;
        }

        var variante = await _productoVarianteRepository.ObtenerPorEmpresaAsync(
            empresaId,
            detalleRequest.ProductoVarianteId.Value,
            cancellationToken);
        if (variante is null || variante.ProductoId != detalleRequest.ProductoId)
        {
            throw new InvalidOperationException("La variante no pertenece al producto y empresa activos.");
        }
    }

    private async Task<IReadOnlyCollection<StockADescontar>> ValidarStockAsync(
        Guid empresaId,
        IReadOnlyCollection<VentaDetalle> detalles,
        CancellationToken cancellationToken)
    {
        var stocks = new List<StockADescontar>();
        var cantidadesPorStock = detalles
            .GroupBy(detalle => new StockKey(detalle.ProductoId, detalle.ProductoVarianteId))
            .Select(grupo => new StockADescontarRequest(
                grupo.Key.ProductoId,
                grupo.Key.ProductoVarianteId,
                grupo.Sum(detalle => detalle.Cantidad)))
            .ToArray();

        foreach (var item in cantidadesPorStock)
        {
            var stock = await _stockRepository.ObtenerPorProductoAsync(
                empresaId,
                item.ProductoId,
                item.ProductoVarianteId,
                cancellationToken);
            if (stock is null)
            {
                throw new InvalidOperationException(
                    CrearMensajeStockNoDisponible(item.ProductoId, item.ProductoVarianteId));
            }

            if (stock.CantidadLibre < item.Cantidad)
            {
                throw new InvalidOperationException(
                    CrearMensajeStockInsuficiente(item.ProductoId, item.ProductoVarianteId));
            }

            stocks.Add(new StockADescontar(stock, item.Cantidad, stock.CantidadDisponible));
        }

        return stocks;
    }

    private static string CrearMensajeStockNoDisponible(Guid productoId, Guid? productoVarianteId)
    {
        return productoVarianteId is null
            ? $"No existe stock registrado para el producto {productoId}."
            : $"No existe stock registrado para el producto {productoId} y variante {productoVarianteId}.";
    }

    private static string CrearMensajeStockInsuficiente(Guid productoId, Guid? productoVarianteId)
    {
        return productoVarianteId is null
            ? $"Stock insuficiente para el producto {productoId}."
            : $"Stock insuficiente para el producto {productoId} y variante {productoVarianteId}.";
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar ventas.");
        }
    }

    private sealed record StockKey(Guid ProductoId, Guid? ProductoVarianteId);

    private sealed record StockADescontarRequest(
        Guid ProductoId,
        Guid? ProductoVarianteId,
        decimal Cantidad);

    private sealed record StockADescontar(
        StockProducto Stock,
        decimal Cantidad,
        decimal CantidadDisponibleOriginal)
    {
        public void Restaurar()
        {
            Stock.AjustarCantidadDisponible(CantidadDisponibleOriginal);
        }
    }
}
