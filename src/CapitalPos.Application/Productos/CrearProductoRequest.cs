using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed record CrearProductoRequest(
    string Nombre,
    decimal PrecioVenta,
    string? CodigoSku = null,
    string? CodigoBarras = null,
    decimal? Costo = null,
    bool Activo = true,
    Guid? CategoriaId = null,
    Guid? MarcaId = null,
    ModoManejoProducto ModoManejo = ModoManejoProducto.SIMPLE)
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
            Activo,
            categoriaId: CategoriaId,
            marcaId: MarcaId,
            modoManejo: ModoManejo);
    }
}
