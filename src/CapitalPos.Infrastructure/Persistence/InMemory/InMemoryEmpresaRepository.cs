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

    public Task<IReadOnlyCollection<Empresa>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Empresas);
    }

    public Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var empresa = _empresas.SingleOrDefault(empresa => empresa.Id == id);

        return Task.FromResult(empresa);
    }
}
