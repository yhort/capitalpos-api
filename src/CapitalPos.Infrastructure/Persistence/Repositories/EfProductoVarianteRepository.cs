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

    public Task<ProductoVariante?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductosVariantes
            .SingleOrDefaultAsync(
                variante => variante.EmpresaId == empresaId && variante.Id == id,
                cancellationToken);
    }

    public async Task ActualizarAsync(
        ProductoVariante variante,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(variante);

        _dbContext.ProductosVariantes.Update(variante);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExisteSkuAsync(
        Guid empresaId,
        string codigoSku,
        CancellationToken cancellationToken = default)
    {
        var skuNormalizado = codigoSku.Trim();
        if (string.IsNullOrWhiteSpace(skuNormalizado))
        {
            return Task.FromResult(false);
        }

        return _dbContext.ProductosVariantes.AnyAsync(
            variante =>
                variante.EmpresaId == empresaId &&
                variante.CodigoSku == skuNormalizado,
            cancellationToken);
    }

    public Task<bool> ExisteCodigoBarrasAsync(
        Guid empresaId,
        string codigoBarras,
        CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = codigoBarras.Trim();
        if (string.IsNullOrWhiteSpace(codigoNormalizado))
        {
            return Task.FromResult(false);
        }

        return _dbContext.ProductosVariantes.AnyAsync(
            variante =>
                variante.EmpresaId == empresaId &&
                variante.CodigoBarras == codigoNormalizado,
            cancellationToken);
    }
}
