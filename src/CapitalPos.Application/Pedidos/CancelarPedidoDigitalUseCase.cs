using CapitalPos.Application.Inventario;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Pedidos;

public sealed record CancelarPedidoDigitalRequest(string? Observacion = null);

public sealed class CancelarPedidoDigitalUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IMovimientoInventarioRepository? _movimientos;
    private readonly IPedidoDigitalRepository _pedidoRepository;
    private readonly IStockProductoRepository _stockRepository;

    public CancelarPedidoDigitalUseCase(
        IPedidoDigitalRepository pedidoRepository,
        IStockProductoRepository stockRepository,
        IEmpresaActivaContext empresaActiva,
        IMovimientoInventarioRepository? movimientos = null)
    {
        _pedidoRepository = pedidoRepository;
        _stockRepository = stockRepository;
        _empresaActiva = empresaActiva;
        _movimientos = movimientos;
    }

    public async Task<PedidoDigital?> EjecutarAsync(
        Guid pedidoId,
        CancelarPedidoDigitalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (pedidoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del pedido digital es obligatorio.", nameof(pedidoId));
        }

        ValidarEmpresaActiva();
        var empresaId = _empresaActiva.EmpresaId;
        var pedido = await _pedidoRepository.ObtenerPorEmpresaAsync(empresaId, pedidoId, cancellationToken);
        if (pedido is null)
        {
            return null;
        }

        var stocksALiberar = await PrepararLiberacionesAsync(empresaId, pedido, cancellationToken);
        var usuarioId = _empresaActiva.UsuarioId == Guid.Empty
            ? (Guid?)null
            : _empresaActiva.UsuarioId;
        pedido.Cancelar(usuarioId, request.Observacion);

        foreach (var stockALiberar in stocksALiberar)
        {
            var stockLibreAnterior = stockALiberar.Stock.CantidadLibre;
            stockALiberar.Stock.LiberarReserva(stockALiberar.Cantidad);
            await _stockRepository.GuardarAsync(stockALiberar.Stock, cancellationToken);

            if (_movimientos is not null)
            {
                await _movimientos.AgregarAsync(
                    new MovimientoInventario(
                        Guid.NewGuid(),
                        empresaId,
                        pedido.SedeId,
                        stockALiberar.Stock.ProductoId,
                        stockALiberar.Stock.ProductoVarianteId,
                        TipoMovimientoInventario.LIBERACION_RESERVA,
                        stockALiberar.Cantidad,
                        stockLibreAnterior,
                        stockALiberar.Stock.CantidadLibre,
                        "PEDIDO_DIGITAL",
                        pedido.Id,
                        motivo: request.Observacion ?? "Liberacion de reserva por cancelacion de pedido digital",
                        usuarioId: usuarioId),
                    cancellationToken);
            }
        }

        await _pedidoRepository.GuardarCambiosAsync(cancellationToken);
        return pedido;
    }

    private async Task<IReadOnlyCollection<StockALiberar>> PrepararLiberacionesAsync(
        Guid empresaId,
        PedidoDigital pedido,
        CancellationToken cancellationToken)
    {
        var liberaciones = new List<StockALiberar>();
        var cantidadesPorStock = pedido.Detalles
            .GroupBy(detalle => new StockKey(detalle.ProductoId, detalle.ProductoVarianteId))
            .Select(grupo => new
            {
                grupo.Key.ProductoId,
                grupo.Key.ProductoVarianteId,
                Cantidad = grupo.Sum(detalle => detalle.CantidadBase)
            })
            .ToArray();

        foreach (var item in cantidadesPorStock)
        {
            var stock = await _stockRepository.ObtenerPorProductoAsync(
                empresaId,
                pedido.SedeId,
                item.ProductoId,
                item.ProductoVarianteId,
                cancellationToken);
            if (stock is null)
            {
                throw new InvalidOperationException(
                    item.ProductoVarianteId is null
                        ? $"No se encontro el stock para liberar la reserva del producto {item.ProductoId}."
                        : $"No se encontro el stock para liberar la reserva del producto {item.ProductoId} y variante {item.ProductoVarianteId}.");
            }

            if (stock.CantidadReservada < item.Cantidad)
            {
                throw new InvalidOperationException(
                    item.ProductoVarianteId is null
                        ? $"La reserva del producto {item.ProductoId} es insuficiente para liberar."
                        : $"La reserva del producto {item.ProductoId} y variante {item.ProductoVarianteId} es insuficiente para liberar.");
            }

            liberaciones.Add(new StockALiberar(stock, item.Cantidad));
        }

        return liberaciones;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para cancelar pedidos digitales.");
        }
    }

    private sealed record StockKey(Guid ProductoId, Guid? ProductoVarianteId);

    private sealed record StockALiberar(StockProducto Stock, decimal Cantidad);
}
