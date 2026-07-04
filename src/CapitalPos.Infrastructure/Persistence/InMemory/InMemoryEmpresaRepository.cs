using CapitalPos.Application.Empresas;
using CapitalPos.Domain;

namespace CapitalPos.Infrastructure.Persistence.InMemory;

public sealed class InMemoryEmpresaRepository : IEmpresaRepository
{
    private readonly List<Empresa> _empresas = new();

    public IReadOnlyCollection<Empresa> Empresas => _empresas.AsReadOnly();

    public Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empresa);

        _empresas.Add(empresa);

        return Task.CompletedTask;
    }
}
