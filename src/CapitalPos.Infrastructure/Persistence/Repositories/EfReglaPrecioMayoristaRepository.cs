using CapitalPos.Application.Productos;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfReglaPrecioMayoristaRepository : IReglaPrecioMayoristaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfReglaPrecioMayoristaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarActivasPorProductosAsync(
        Guid empresaId,
        IReadOnlyCollection<Guid> productoIds,
        CancellationToken cancellationToken = default)
    {
        if (productoIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.ReglasPreciosMayoristas
            .AsNoTracking()
            .Where(regla =>
                regla.EmpresaId == empresaId &&
                regla.Activa &&
                productoIds.Contains(regla.ProductoId))
            .OrderBy(regla => regla.ProductoId)
            .ThenByDescending(regla => regla.CantidadMinima)
            .ToListAsync(cancellationToken);
    }
}
