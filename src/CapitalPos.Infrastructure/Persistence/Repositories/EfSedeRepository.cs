using CapitalPos.Application.Sedes;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfSedeRepository : ISedeRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfSedeRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Sede sede, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sede);

        await _dbContext.Sedes.AddAsync(sede, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Sede>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sedes
            .AsNoTracking()
            .Where(sede => sede.EmpresaId == empresaId)
            .OrderBy(sede => sede.Nombre)
            .ToListAsync(cancellationToken);
    }

    public Task<Sede?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Sedes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                sede => sede.EmpresaId == empresaId && sede.Id == id,
                cancellationToken);
    }
}
