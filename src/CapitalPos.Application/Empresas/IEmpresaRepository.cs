using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public interface IEmpresaRepository
{
    Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Empresa>> ListarAsync(CancellationToken cancellationToken = default);
}
