using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public interface IEmpresaRepository
{
    Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Empresa>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default);
}
