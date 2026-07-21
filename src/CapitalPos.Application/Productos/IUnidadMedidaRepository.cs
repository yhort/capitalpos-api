using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public interface IUnidadMedidaRepository
{
    Task AgregarAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UnidadMedida>> ListarAsync(CancellationToken cancellationToken = default);

    Task<UnidadMedida?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UnidadMedida?> ObtenerPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default);
}
