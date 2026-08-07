namespace CapitalPos.Application.Compras;

public sealed record CrearCompraRequest(
    Guid SedeId,
    string Proveedor,
    string TipoComprobante,
    string Serie,
    string Correlativo,
    DateTimeOffset? FechaCompra,
    IReadOnlyCollection<CrearCompraDetalleRequest> Detalles);

public sealed record CrearCompraDetalleRequest(
    Guid ProductoId,
    Guid? ProductoVarianteId,
    decimal Cantidad,
    decimal CostoUnitario);
