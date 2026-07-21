using CapitalPos.Application.Productos;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfUnidadMedidaRepository : IUnidadMedidaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfUnidadMedidaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unidadMedida);

        await _dbContext.UnidadesMedida.AddAsync(unidadMedida, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UnidadMedida>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UnidadesMedida
            .AsNoTracking()
            .OrderBy(unidadMedida => unidadMedida.Codigo)
            .ToListAsync(cancellationToken);
    }

    public Task<UnidadMedida?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UnidadesMedida
            .SingleOrDefaultAsync(unidadMedida => unidadMedida.Id == id, cancellationToken);
    }

    public Task<UnidadMedida?> ObtenerPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = codigo.Trim().ToUpperInvariant();

        return _dbContext.UnidadesMedida
            .SingleOrDefaultAsync(unidadMedida => unidadMedida.Codigo == codigoNormalizado, cancellationToken);
    }
}
