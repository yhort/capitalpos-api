using CapitalPos.Application.Productos;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfProductoPresentacionRepository : IProductoPresentacionRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfProductoPresentacionRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(
        ProductoPresentacion presentacion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presentacion);

        await _dbContext.ProductosPresentaciones.AddAsync(presentacion, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductoPresentacion>> ListarPorProductoAsync(
        Guid empresaId,
        Guid productoId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductosPresentaciones
            .AsNoTracking()
            .Where(presentacion =>
                presentacion.EmpresaId == empresaId &&
                presentacion.ProductoId == productoId)
            .OrderByDescending(presentacion => presentacion.EsUnidadBase)
            .ThenBy(presentacion => presentacion.FactorConversion)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductoPresentacion?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductosPresentaciones
            .SingleOrDefaultAsync(
                presentacion => presentacion.EmpresaId == empresaId && presentacion.Id == id,
                cancellationToken);
    }

    public Task<bool> ExisteCodigoBarrasAsync(
        Guid empresaId,
        string codigoBarras,
        CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = codigoBarras.Trim();

        return _dbContext.ProductosPresentaciones.AnyAsync(
            presentacion =>
                presentacion.EmpresaId == empresaId &&
                presentacion.CodigoBarras == codigoNormalizado,
            cancellationToken);
    }
}
