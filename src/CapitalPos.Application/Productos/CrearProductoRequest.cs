using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed record CrearProductoRequest(
    string Nombre,
    decimal PrecioVenta,
    string? CodigoSku = null,
    string? CodigoBarras = null,
    decimal? Costo = null,
    bool Activo = true)
{
    public Producto CrearProducto(Guid empresaId)
    {
        return new Producto(
            Guid.NewGuid(),
            empresaId,
            Nombre,
            PrecioVenta,
            CodigoSku,
            CodigoBarras,
            Costo,
            Activo);
    }
}
