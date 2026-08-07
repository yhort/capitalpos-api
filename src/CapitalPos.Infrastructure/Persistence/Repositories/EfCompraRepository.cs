using CapitalPos.Application.Compras;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfCompraRepository : ICompraRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfCompraRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Compra compra, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compra);

        await _dbContext.Compras.AddAsync(compra, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Compra>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Compras
            .AsNoTracking()
            .Include(compra => compra.Detalles)
            .Where(compra => compra.EmpresaId == empresaId)
            .OrderByDescending(compra => compra.FechaCompra)
            .ToListAsync(cancellationToken);
    }

    public Task<Compra?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Compras
            .AsNoTracking()
            .Include(compra => compra.Detalles)
            .SingleOrDefaultAsync(
                compra => compra.EmpresaId == empresaId && compra.Id == id,
                cancellationToken);
    }
}
