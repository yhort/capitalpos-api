using CapitalPos.Application.Sedes;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfPuntoVentaRepository : IPuntoVentaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfPuntoVentaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(PuntoVenta puntoVenta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(puntoVenta);

        await _dbContext.PuntosVenta.AddAsync(puntoVenta, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PuntoVenta>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PuntosVenta
            .AsNoTracking()
            .Where(puntoVenta => puntoVenta.EmpresaId == empresaId)
            .OrderBy(puntoVenta => puntoVenta.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PuntoVenta>> ListarPorSedeAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PuntosVenta
            .AsNoTracking()
            .Where(puntoVenta =>
                puntoVenta.EmpresaId == empresaId &&
                puntoVenta.SedeId == sedeId)
            .OrderBy(puntoVenta => puntoVenta.Nombre)
            .ToListAsync(cancellationToken);
    }

    public Task<PuntoVenta?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PuntosVenta
            .AsNoTracking()
            .SingleOrDefaultAsync(
                puntoVenta => puntoVenta.EmpresaId == empresaId && puntoVenta.Id == id,
                cancellationToken);
    }
}
