using CapitalPos.Domain;

namespace CapitalPos.Application.Catalogo;

public sealed record CrearMarcaRequest(
    string Nombre,
    bool Activa = true)
{
    public Marca CrearMarca(Guid empresaId)
    {
        return new Marca(
            Guid.NewGuid(),
            empresaId,
            Nombre,
            Activa);
    }
}
