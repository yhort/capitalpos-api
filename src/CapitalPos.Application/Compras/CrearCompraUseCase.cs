using CapitalPos.Application.Inventario;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Compras;

public sealed class CrearCompraUseCase
{
    private readonly ICompraRepository _compraRepository;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly ISedeRepository _sedeRepository;
    private readonly IStockProductoRepository _stockRepository;
    private readonly IMovimientoInventarioRepository? _movimientos;

    public CrearCompraUseCase(
        ICompraRepository compraRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        ISedeRepository sedeRepository,
        IStockProductoRepository stockRepository,
        IEmpresaActivaContext empresaActiva,
        IMovimientoInventarioRepository? movimientos = null)
    {
        _compraRepository = compraRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _sedeRepository = sedeRepository;
        _stockRepository = stockRepository;
        _empresaActiva = empresaActiva;
        _movimientos = movimientos;
    }

    public async Task<Compra> EjecutarAsync(
        CrearCompraRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var empresaId = _empresaActiva.EmpresaId;
        await ValidarSedeAsync(empresaId, request.SedeId, cancellationToken);

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            throw new ArgumentException("La compra debe tener al menos un detalle.", nameof(request));
        }

        var compraId = Guid.NewGuid();
        var detalles = new List<CompraDetalle>();
        foreach (var detalleRequest in request.Detalles)
        {
            await ValidarProductoAsync(empresaId, detalleRequest, cancellationToken);
            detalles.Add(new CompraDetalle(
                Guid.NewGuid(),
                empresaId,
                compraId,
                detalleRequest.ProductoId,
                detalleRequest.Cantidad,
                detalleRequest.CostoUnitario,
                detalleRequest.ProductoVarianteId));
        }

        var compra = new Compra(
            compraId,
            empresaId,
            request.SedeId,
            request.Proveedor,
            request.TipoComprobante,
            request.Serie,
            request.Correlativo,
            request.FechaCompra ?? DateTimeOffset.UtcNow,
            detalles);

        foreach (var detalle in compra.Detalles)
        {
            await InyectarIngresoCompraAsync(
                empresaId,
                compra.SedeId,
                compra.Id,
                detalle,
                cancellationToken);
        }

        await _compraRepository.AgregarAsync(compra, cancellationToken);
        return compra;
    }

    private async Task InyectarIngresoCompraAsync(
        Guid empresaId,
        Guid sedeId,
        Guid compraId,
        CompraDetalle detalle,
        CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.ObtenerPorProductoAsync(
            empresaId,
            sedeId,
            detalle.ProductoId,
            detalle.ProductoVarianteId,
            cancellationToken);

        decimal anterior;
        if (stock is null)
        {
            stock = new StockProducto(
                Guid.NewGuid(),
                empresaId,
                sedeId,
                detalle.ProductoId,
                detalle.ProductoVarianteId,
                detalle.Cantidad);
            anterior = 0m;
        }
        else
        {
            anterior = stock.CantidadDisponible;
            stock.Incrementar(detalle.Cantidad);
        }

        await _stockRepository.GuardarAsync(stock, cancellationToken);

        if (_movimientos is not null)
        {
            await _movimientos.AgregarAsync(
                new MovimientoInventario(
                    Guid.NewGuid(),
                    empresaId,
                    sedeId,
                    detalle.ProductoId,
                    detalle.ProductoVarianteId,
                    TipoMovimientoInventario.INGRESO_COMPRA,
                    detalle.Cantidad,
                    anterior,
                    stock.CantidadDisponible,
                    "COMPRA",
                    compraId,
                    motivo: $"Ingreso por compra {compraId}",
                    usuarioId: _empresaActiva.UsuarioId),
                cancellationToken);
        }
    }

    private async Task ValidarSedeAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken)
    {
        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        var sede = await _sedeRepository.ObtenerPorEmpresaAsync(empresaId, sedeId, cancellationToken);
        if (sede is null)
        {
            throw new InvalidOperationException("La sede no pertenece a la empresa activa.");
        }

        if (!sede.Activa)
        {
            throw new InvalidOperationException("La sede no esta activa.");
        }
    }

    private async Task ValidarProductoAsync(
        Guid empresaId,
        CrearCompraDetalleRequest detalle,
        CancellationToken cancellationToken)
    {
        if (detalle.ProductoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto es obligatorio.");
        }

        var producto = await _productoRepository.ObtenerPorEmpresaAsync(
            empresaId,
            detalle.ProductoId,
            cancellationToken);
        if (producto is null)
        {
            throw new InvalidOperationException("El producto no pertenece a la empresa activa.");
        }

        if (!producto.Activo)
        {
            throw new InvalidOperationException("El producto no esta activo.");
        }

        if (detalle.ProductoVarianteId is null)
        {
            return;
        }

        if (detalle.ProductoVarianteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la variante no puede estar vacio.");
        }

        var variante = await _productoVarianteRepository.ObtenerPorEmpresaAsync(
            empresaId,
            detalle.ProductoVarianteId.Value,
            cancellationToken);
        if (variante is null || variante.ProductoId != detalle.ProductoId)
        {
            throw new InvalidOperationException("La variante no pertenece al producto y empresa activos.");
        }

        if (!variante.Activo)
        {
            throw new InvalidOperationException("La variante no esta activa.");
        }
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para registrar compras.");
        }
    }
}
