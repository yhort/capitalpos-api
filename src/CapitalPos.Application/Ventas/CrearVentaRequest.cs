using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed record CrearVentaRequest(
    DateTimeOffset? Fecha,
    Guid? ClienteId,
    IReadOnlyCollection<CrearVentaDetalleRequest> Detalles,
    string? CanalVenta = null,
    Guid? PuntoVentaId = null,
    Guid? VendedorId = null);

public sealed record CrearVentaDetalleRequest(
    Guid ProductoId,
    Guid? ProductoVarianteId,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Igv,
    decimal Total)
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
            ProductoVarianteId);
    }
}
