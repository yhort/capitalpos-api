using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed record CrearVentaRequest(
    DateTimeOffset? Fecha,
    Guid? ClienteId,
    IReadOnlyCollection<CrearVentaDetalleRequest> Detalles,
    Guid PuntoVentaId,
    string? CanalVenta = null,
    Guid? VendedorId = null);

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
