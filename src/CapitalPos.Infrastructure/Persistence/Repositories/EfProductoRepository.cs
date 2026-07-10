using CapitalPos.Application.Productos;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfProductoRepository : IProductoRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfProductoRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(producto);

        await _dbContext.Productos.AddAsync(producto, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Productos
            .AsNoTracking()
            .Where(producto => producto.EmpresaId == empresaId)
            .OrderBy(producto => producto.Nombre)
            .ToListAsync(cancellationToken);
    }
}
