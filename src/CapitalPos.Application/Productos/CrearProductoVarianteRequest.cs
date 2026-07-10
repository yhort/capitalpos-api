using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed record CrearProductoVarianteRequest(
    Guid ProductoId,
    string? Talla = null,
    string? Color = null,
    string? CodigoSku = null,
    string? CodigoBarras = null,
    decimal StockActual = 0,
    bool Activo = true)
{
    public ProductoVariante CrearProductoVariante(Guid empresaId)
    {
        return new ProductoVariante(
            Guid.NewGuid(),
            empresaId,
            ProductoId,
            Talla,
            Color,
            CodigoSku,
            CodigoBarras,
            StockActual,
            Activo);
    }
}
