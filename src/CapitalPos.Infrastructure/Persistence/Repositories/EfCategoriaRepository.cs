using CapitalPos.Application.Catalogo;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfCategoriaRepository : ICategoriaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfCategoriaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Categoria categoria, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(categoria);

        await _dbContext.Categorias.AddAsync(categoria, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Categoria>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categorias
            .AsNoTracking()
            .Where(categoria => categoria.EmpresaId == empresaId)
            .OrderBy(categoria => categoria.Nombre)
            .ToListAsync(cancellationToken);
    }

    public Task<Categoria?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Categorias
            .AsNoTracking()
            .SingleOrDefaultAsync(
                categoria => categoria.EmpresaId == empresaId && categoria.Id == id,
                cancellationToken);
    }
}
