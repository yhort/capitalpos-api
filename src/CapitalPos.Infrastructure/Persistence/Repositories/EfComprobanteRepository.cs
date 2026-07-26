using CapitalPos.Application.Ventas;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfComprobanteRepository : IComprobanteRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfComprobanteRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(
        Comprobante comprobante,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comprobante);

        await _dbContext.Comprobantes.AddAsync(comprobante, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistePorVentaAsync(
        Guid empresaId,
        Guid ventaId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Comprobantes.AnyAsync(
            comprobante => comprobante.EmpresaId == empresaId && comprobante.VentaId == ventaId,
            cancellationToken);
    }
}
