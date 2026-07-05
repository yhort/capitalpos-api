using CapitalPos.Application.Empresas;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfEmpresaRepository : IEmpresaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfEmpresaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empresa);

        await _dbContext.Empresas.AddAsync(empresa, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Empresa>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Empresas
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Empresas
            .SingleOrDefaultAsync(empresa => empresa.Id == id, cancellationToken);
    }

    public async Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empresa);

        _dbContext.Empresas.Update(empresa);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExisteRucAsync(string ruc, CancellationToken cancellationToken = default)
    {
        return _dbContext.Empresas
            .AsNoTracking()
            .AnyAsync(empresa => empresa.Ruc == ruc, cancellationToken);
    }
}
