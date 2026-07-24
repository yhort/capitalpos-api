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

    public async Task AgregarAsync(
        ReglaPrecioMayorista regla,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(regla);

        await _dbContext.ReglasPreciosMayoristas.AddAsync(regla, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarPorProductoAsync(
        Guid empresaId,
        Guid productoId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ReglasPreciosMayoristas
            .AsNoTracking()
            .Where(regla =>
                regla.EmpresaId == empresaId &&
                regla.ProductoId == productoId)
            .OrderBy(regla => regla.CantidadMinima)
            .ThenBy(regla => regla.Id)
            .ToListAsync(cancellationToken);
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

    public Task<ReglaPrecioMayorista?> ObtenerPorEmpresaYProductoAsync(
        Guid empresaId,
        Guid productoId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ReglasPreciosMayoristas
            .SingleOrDefaultAsync(
                regla =>
                    regla.EmpresaId == empresaId &&
                    regla.ProductoId == productoId &&
                    regla.Id == id,
                cancellationToken);
    }

    public Task<bool> ExisteActivaPorCantidadMinimaAsync(
        Guid empresaId,
        Guid productoId,
        int cantidadMinima,
        Guid? excluirId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ReglasPreciosMayoristas
            .AnyAsync(
                regla =>
                    regla.EmpresaId == empresaId &&
                    regla.ProductoId == productoId &&
                    regla.CantidadMinima == cantidadMinima &&
                    regla.Activa &&
                    (!excluirId.HasValue || regla.Id != excluirId.Value),
                cancellationToken);
    }

    public async Task ActualizarAsync(
        ReglaPrecioMayorista regla,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(regla);

        _dbContext.ReglasPreciosMayoristas.Update(regla);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
