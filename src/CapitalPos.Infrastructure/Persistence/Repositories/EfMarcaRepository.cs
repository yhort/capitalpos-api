using CapitalPos.Application.Catalogo;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfMarcaRepository : IMarcaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfMarcaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Marca marca, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marca);

        await _dbContext.Marcas.AddAsync(marca, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Marca>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Marcas
            .AsNoTracking()
            .Where(marca => marca.EmpresaId == empresaId)
            .OrderBy(marca => marca.Nombre)
            .ToListAsync(cancellationToken);
    }

    public Task<Marca?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Marcas
            .AsNoTracking()
            .SingleOrDefaultAsync(
                marca => marca.EmpresaId == empresaId && marca.Id == id,
                cancellationToken);
    }

    public Task<bool> ExisteNombreAsync(
        Guid empresaId,
        string nombre,
        CancellationToken cancellationToken = default)
    {
        var nombreNormalizado = nombre.Trim();

        return _dbContext.Marcas.AnyAsync(
            marca => marca.EmpresaId == empresaId && marca.Nombre == nombreNormalizado,
            cancellationToken);
    }
}
