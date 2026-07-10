using CapitalPos.Application.Productos;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfProductoVarianteRepository : IProductoVarianteRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfProductoVarianteRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(
        ProductoVariante variante,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(variante);

        await _dbContext.ProductosVariantes.AddAsync(variante, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
        Guid empresaId,
        Guid productoId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductosVariantes
            .AsNoTracking()
            .Where(variante => variante.EmpresaId == empresaId && variante.ProductoId == productoId)
            .OrderBy(variante => variante.Talla)
            .ThenBy(variante => variante.Color)
            .ToListAsync(cancellationToken);
    }
}
