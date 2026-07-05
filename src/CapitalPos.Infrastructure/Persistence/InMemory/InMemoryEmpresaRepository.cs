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

    public Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empresa);

        if (_empresas.All(empresaGuardada => empresaGuardada.Id != empresa.Id))
        {
            throw new InvalidOperationException("La empresa no existe en el repositorio.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExisteRucAsync(string ruc, CancellationToken cancellationToken = default)
    {
        var existe = _empresas.Any(empresa => empresa.Ruc == ruc);

        return Task.FromResult(existe);
    }
}
