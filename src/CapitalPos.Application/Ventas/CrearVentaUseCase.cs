using CapitalPos.Application.Caja;
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
    private readonly IProductoPresentacionRepository _productoPresentacionRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IReglaPrecioMayoristaRepository _reglaPrecioMayoristaRepository;
    private readonly IPuntoVentaRepository _puntoVentaRepository;
    private readonly IStockProductoRepository _stockRepository;
    private readonly ISesionCajaRepository _sesionCajaRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly IMovimientoInventarioRepository? _movimientos;

    public CrearVentaUseCase(
        IVentaRepository ventaRepository,
        IProductoRepository productoRepository,
        IProductoPresentacionRepository productoPresentacionRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IReglaPrecioMayoristaRepository reglaPrecioMayoristaRepository,
        IClienteRepository clienteRepository,
        IStockProductoRepository stockRepository,
        ISesionCajaRepository sesionCajaRepository,
        IPuntoVentaRepository puntoVentaRepository,
        IEmpresaActivaContext empresaActiva, IMovimientoInventarioRepository? movimientos = null)
    {
        _ventaRepository = ventaRepository;
        _productoRepository = productoRepository;
        _productoPresentacionRepository = productoPresentacionRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _reglaPrecioMayoristaRepository = reglaPrecioMayoristaRepository;
        _clienteRepository = clienteRepository;
        _stockRepository = stockRepository;
        _sesionCajaRepository = sesionCajaRepository;
        _puntoVentaRepository = puntoVentaRepository;
        _empresaActiva = empresaActiva;
        _movimientos = movimientos;
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
        await ValidarSesionCajaAbiertaAsync(empresaId, puntoVenta.Id, cancellationToken);
        await ValidarClienteAsync(empresaId, request.ClienteId, cancellationToken);

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            throw new ArgumentException("La venta debe tener al menos un detalle.", nameof(request));
        }

        var ventaId = Guid.NewGuid();
        var detallesPreparados = new List<DetalleVentaPreparado>();
        foreach (var detalleRequest in request.Detalles)
        {
            detallesPreparados.Add(await CrearDetallePreparadoAsync(
                empresaId,
                ventaId,
                detalleRequest,
                cancellationToken));
        }

        await AplicarPrecioMayoristaAsync(empresaId, detallesPreparados, cancellationToken);

        var stocksADescontar = await ValidarStockAsync(
            empresaId,
            puntoVenta.SedeId,
            detallesPreparados,
            cancellationToken);

        var detalles = detallesPreparados
            .Select(detallePreparado => detallePreparado.Detalle)
            .ToArray();
        var total = detalles.Sum(detalle => detalle.Total);
        var igv = detalles.Sum(detalle => detalle.Igv);
        var subtotal = total - igv;
        var pagos = CrearPagos(request.Pagos, empresaId, ventaId, total);
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
            request.VendedorId,
            pagos: pagos);

        try
        {
            foreach (var stockADescontar in stocksADescontar)
            {
                stockADescontar.Stock.Descontar(stockADescontar.Cantidad);
                await _stockRepository.GuardarAsync(stockADescontar.Stock, cancellationToken);
                if (_movimientos is not null) await _movimientos.AgregarAsync(new MovimientoInventario(Guid.NewGuid(), empresaId, puntoVenta.SedeId, stockADescontar.Stock.ProductoId, stockADescontar.Stock.ProductoVarianteId, TipoMovimientoInventario.VENTA, stockADescontar.Cantidad, stockADescontar.CantidadDisponibleOriginal, stockADescontar.Stock.CantidadDisponible, "VENTA", ventaId, usuarioId: _empresaActiva.UsuarioId), cancellationToken);
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

    private static IReadOnlyCollection<VentaPago> CrearPagos(
        IReadOnlyCollection<CrearVentaPagoRequest>? pagosRequest,
        Guid empresaId,
        Guid ventaId,
        decimal total)
    {
        if (pagosRequest is null || pagosRequest.Count == 0)
        {
            return
            [
                new VentaPago(
                    Guid.NewGuid(),
                    empresaId,
                    ventaId,
                    MetodoPago.EFECTIVO,
                    total)
            ];
        }

        var pagos = pagosRequest
            .Select(pago => new VentaPago(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                NormalizarMetodoPago(pago.MetodoPago),
                pago.Monto,
                pago.CodigoOperacion,
                pago.Observacion))
            .ToArray();

        if (pagos.Sum(pago => pago.Monto) != total)
        {
            throw new ArgumentException("La suma de los pagos debe ser igual al total de la venta.", nameof(pagosRequest));
        }

        return pagos;
    }

    private static MetodoPago NormalizarMetodoPago(string? metodoPago)
    {
        if (!string.IsNullOrWhiteSpace(metodoPago)
            && Enum.TryParse<MetodoPago>(metodoPago.Trim(), true, out var metodoNormalizado)
            && Enum.IsDefined(metodoNormalizado))
        {
            return metodoNormalizado;
        }

        throw new ArgumentException("El metodo de pago no es valido.", nameof(metodoPago));
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

    private async Task ValidarSesionCajaAbiertaAsync(
        Guid empresaId,
        Guid puntoVentaId,
        CancellationToken cancellationToken)
    {
        var sesionCaja = await _sesionCajaRepository.ObtenerAbiertaPorPuntoVentaAsync(
            empresaId,
            puntoVentaId,
            cancellationToken);
        if (sesionCaja is null)
        {
            throw new InvalidOperationException("Debe abrir una sesion de caja antes de registrar ventas.");
        }
    }

    private async Task<DetalleVentaPreparado> CrearDetallePreparadoAsync(
        Guid empresaId,
        Guid ventaId,
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

        await ValidarVarianteAsync(empresaId, detalleRequest, cancellationToken);

        if (detalleRequest.ProductoPresentacionId is null)
        {
            return new DetalleVentaPreparado(
                detalleRequest.CrearDetalle(empresaId, ventaId));
        }

        var presentacion = await _productoPresentacionRepository.ObtenerPorEmpresaAsync(
            empresaId,
            detalleRequest.ProductoPresentacionId.Value,
            cancellationToken);
        if (presentacion is null)
        {
            throw new InvalidOperationException("La presentacion no pertenece a la empresa activa.");
        }

        if (presentacion.ProductoId != detalleRequest.ProductoId)
        {
            throw new InvalidOperationException("La presentacion no pertenece al producto activo.");
        }

        if (!presentacion.Activa)
        {
            throw new InvalidOperationException("La presentacion del producto no esta activa.");
        }

        var cantidadBaseDescontada = detalleRequest.Cantidad * presentacion.FactorConversion;
        var total = Redondear(detalleRequest.Cantidad * presentacion.PrecioVenta);
        var subtotal = Redondear(total / 1.18m);
        var igv = Redondear(total - subtotal);
        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            detalleRequest.ProductoId,
            detalleRequest.Cantidad,
            presentacion.PrecioVenta,
            igv,
            total,
            detalleRequest.ProductoVarianteId,
            presentacion.Id,
            presentacion.FactorConversion,
            cantidadBaseDescontada);

        return new DetalleVentaPreparado(detalle);
    }

    private async Task ValidarVarianteAsync(
        Guid empresaId,
        CrearVentaDetalleRequest detalleRequest,
        CancellationToken cancellationToken)
    {
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
        Guid sedeId,
        IReadOnlyCollection<DetalleVentaPreparado> detalles,
        CancellationToken cancellationToken)
    {
        var stocks = new List<StockADescontar>();
        var cantidadesPorStock = detalles
            .GroupBy(detalle => new StockKey(detalle.Detalle.ProductoId, detalle.Detalle.ProductoVarianteId))
            .Select(grupo => new StockADescontarRequest(
                grupo.Key.ProductoId,
                grupo.Key.ProductoVarianteId,
                grupo.Sum(detalle => detalle.Detalle.CantidadBaseDescontada)))
            .ToArray();

        foreach (var item in cantidadesPorStock)
        {
            var stock = await _stockRepository.ObtenerPorProductoAsync(
                empresaId,
                sedeId,
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

    private async Task AplicarPrecioMayoristaAsync(
        Guid empresaId,
        IReadOnlyCollection<DetalleVentaPreparado> detalles,
        CancellationToken cancellationToken)
    {
        var productosUnitarios = detalles
            .Select(detalle => detalle.Detalle)
            .Where(detalle => detalle.ProductoPresentacionId is null)
            .GroupBy(detalle => detalle.ProductoId)
            .Select(grupo => new
            {
                ProductoId = grupo.Key,
                Cantidad = grupo.Sum(detalle => detalle.Cantidad),
                Detalles = grupo.ToArray()
            })
            .ToArray();

        if (productosUnitarios.Length == 0)
        {
            return;
        }

        var reglas = await _reglaPrecioMayoristaRepository.ListarActivasPorProductosAsync(
            empresaId,
            productosUnitarios.Select(producto => producto.ProductoId).Distinct().ToArray(),
            cancellationToken);

        if (reglas.Count == 0)
        {
            return;
        }

        foreach (var producto in productosUnitarios)
        {
            var reglaAplicable = reglas
                .Where(regla =>
                    regla.ProductoId == producto.ProductoId &&
                    regla.CantidadMinima <= producto.Cantidad)
                .OrderByDescending(regla => regla.CantidadMinima)
                .FirstOrDefault();

            if (reglaAplicable is null)
            {
                continue;
            }

            foreach (var detalle in producto.Detalles)
            {
                detalle.AplicarPrecioMayorista(reglaAplicable.PrecioUnitarioMayorista);
            }
        }
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

    private sealed record DetalleVentaPreparado(
        VentaDetalle Detalle);

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

    private static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
