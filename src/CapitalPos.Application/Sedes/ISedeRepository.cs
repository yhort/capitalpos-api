using CapitalPos.Domain;

namespace CapitalPos.Application.Sedes;

public interface ISedeRepository
{
    Task AgregarAsync(Sede sede, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Sede>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<Sede?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);
}
