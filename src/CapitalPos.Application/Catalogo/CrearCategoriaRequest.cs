using CapitalPos.Domain;

namespace CapitalPos.Application.Catalogo;

public sealed record CrearCategoriaRequest(
    string Nombre,
    Guid? CategoriaPadreId = null,
    bool Activa = true)
{
    public Categoria CrearCategoria(Guid empresaId)
    {
        return new Categoria(
            Guid.NewGuid(),
            empresaId,
            Nombre,
            CategoriaPadreId,
            Activa);
    }
}
