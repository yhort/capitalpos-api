namespace CapitalPos.Application.Pedidos;

public sealed record CrearPedidoDigitalRequest(
    Guid? ClienteId,
    Guid SedeId,
    Guid? PuntoVentaId,
    string CanalPedido,
    DateTimeOffset? FechaPedido,
    IReadOnlyCollection<CrearPedidoDigitalDetalleRequest> Detalles,
    string? ReferenciaExterna = null,
    string? Observacion = null);

public sealed record CrearPedidoDigitalDetalleRequest(
    Guid ProductoId,
    Guid? ProductoVarianteId,
    Guid? ProductoPresentacionId,
    string? Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario);
