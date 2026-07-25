using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed record CrearVentaRequest(
    DateTimeOffset? Fecha,
    Guid? ClienteId,
    IReadOnlyCollection<CrearVentaDetalleRequest> Detalles,
    Guid PuntoVentaId,
    string? CanalVenta = null,
    Guid? VendedorId = null,
    IReadOnlyCollection<CrearVentaPagoRequest>? Pagos = null);

public sealed record CrearVentaPagoRequest(
    string MetodoPago,
    decimal Monto,
    string? CodigoOperacion = null,
    string? Observacion = null);

public sealed record CrearVentaDetalleRequest(
    Guid ProductoId,
    Guid? ProductoVarianteId,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Igv,
    decimal Total,
    Guid? ProductoPresentacionId = null)
{
    public VentaDetalle CrearDetalle(Guid empresaId, Guid ventaId)
    {
        return new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            ProductoId,
            Cantidad,
            PrecioUnitario,
            Igv,
            Total,
            ProductoVarianteId,
            ProductoPresentacionId);
    }
}
