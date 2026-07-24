using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public interface IReglaPrecioMayoristaRepository
{
    Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarActivasPorProductosAsync(
        Guid empresaId,
        IReadOnlyCollection<Guid> productoIds,
        CancellationToken cancellationToken = default);
}
