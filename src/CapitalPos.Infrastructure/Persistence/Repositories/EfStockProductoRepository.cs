using CapitalPos.Application.Inventario;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfStockProductoRepository : IStockProductoRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfStockProductoRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StockProducto?> ObtenerPorProductoAsync(
        Guid empresaId,
        Guid productoId,
        Guid? productoVarianteId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.StocksProductos.SingleOrDefaultAsync(
            stock =>
                stock.EmpresaId == empresaId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId == productoVarianteId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<StockProducto>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StocksProductos
            .AsNoTracking()
            .Where(stock => stock.EmpresaId == empresaId)
            .ToListAsync(cancellationToken);
    }

    public async Task GuardarAsync(
        StockProducto stock,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stock);

        if (_dbContext.Entry(stock).State != EntityState.Detached)
        {
            return;
        }

        var existe = await _dbContext.StocksProductos.AnyAsync(
            actual => actual.Id == stock.Id,
            cancellationToken);
        if (existe)
        {
            _dbContext.StocksProductos.Update(stock);
        }
        else
        {
            await _dbContext.StocksProductos.AddAsync(stock, cancellationToken);
        }
    }
}
