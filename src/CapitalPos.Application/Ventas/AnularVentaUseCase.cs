using CapitalPos.Application.Inventario;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed record AnularVentaRequest(string? Observacion = null);

public sealed class AnularVentaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IComprobanteRepository _comprobanteRepository;
    private readonly IStockProductoRepository _stockRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly IMovimientoInventarioRepository? _movimientos;

    public AnularVentaUseCase(
        IVentaRepository ventaRepository,
        IStockProductoRepository stockRepository,
        IComprobanteRepository comprobanteRepository,
        IEmpresaActivaContext empresaActiva, IMovimientoInventarioRepository? movimientos = null)
    {
        _ventaRepository = ventaRepository;
        _stockRepository = stockRepository;
        _comprobanteRepository = comprobanteRepository;
        _empresaActiva = empresaActiva;
        _movimientos = movimientos;
    }

    public async Task<Venta?> EjecutarAsync(
        Guid ventaId,
        AnularVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (ventaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la venta es obligatorio.", nameof(ventaId));
        }

        ValidarEmpresaActiva();
        var empresaId = _empresaActiva.EmpresaId;
        var venta = await _ventaRepository.ObtenerPorEmpresaAsync(empresaId, ventaId, cancellationToken);
        if (venta is null)
        {
            return null;
        }

        if (venta.Estado == EstadoVenta.Anulada)
        {
            throw new InvalidOperationException("La venta ya se encuentra anulada.");
        }

        if (await _comprobanteRepository.ExistePorVentaAsync(empresaId, venta.Id, cancellationToken))
        {
            throw new InvalidOperationException("No se puede anular una venta con comprobante emitido; requiere nota de credito.");
        }

        var stocks = new List<(StockProducto Stock, decimal Cantidad)>();
        foreach (var detalle in venta.Detalles)
        {
            var stock = await _stockRepository.ObtenerPorProductoAsync(
                empresaId,
                venta.SedeId,
                detalle.ProductoId,
                detalle.ProductoVarianteId,
                cancellationToken);
            if (stock is null)
            {
                throw new InvalidOperationException("No se encontro el stock para revertir la venta.");
            }

            stocks.Add((stock, detalle.CantidadBaseDescontada));
        }

        venta.Anular(request.Observacion);
        foreach (var (stock, cantidad) in stocks)
        {
            var anterior = stock.CantidadDisponible;
            stock.Incrementar(cantidad);
            await _stockRepository.GuardarAsync(stock, cancellationToken);
            if (_movimientos is not null) await _movimientos.AgregarAsync(new MovimientoInventario(Guid.NewGuid(), empresaId, venta.SedeId, stock.ProductoId, stock.ProductoVarianteId, TipoMovimientoInventario.ANULACION_VENTA, cantidad, anterior, stock.CantidadDisponible, "VENTA", venta.Id, request.Observacion, usuarioId: _empresaActiva.UsuarioId), cancellationToken);
        }

        await _ventaRepository.GuardarCambiosAsync(cancellationToken);
        return venta;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para anular ventas.");
        }
    }
}
