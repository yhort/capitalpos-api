using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed record CrearReglaPrecioMayoristaRequest(
    Guid ProductoId,
    int CantidadMinima,
    decimal PrecioUnitarioMayorista)
{
    public ReglaPrecioMayorista CrearRegla(Guid empresaId)
    {
        return new ReglaPrecioMayorista(
            Guid.NewGuid(),
            empresaId,
            ProductoId,
            CantidadMinima,
            PrecioUnitarioMayorista);
    }
}
