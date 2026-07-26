using CapitalPos.Application.Ventas;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfVentaRepository : IVentaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfVentaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Venta venta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(venta);

        await _dbContext.Ventas.AddAsync(venta, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Ventas
            .AsNoTracking()
            .Include(venta => venta.Detalles)
            .Include(venta => venta.Pagos)
            .Where(venta => venta.EmpresaId == empresaId)
            .OrderByDescending(venta => venta.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Venta>> ListarRegistradasPorEmpresaYFechaAsync(
        Guid empresaId,
        DateTimeOffset desde,
        DateTimeOffset hastaExclusivo,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Ventas
            .AsNoTracking()
            .Include(venta => venta.Detalles)
            .Include(venta => venta.Pagos)
            .Where(venta =>
                venta.EmpresaId == empresaId &&
                venta.Estado == EstadoVenta.Registrada &&
                venta.Fecha >= desde &&
                venta.Fecha < hastaExclusivo)
            .OrderByDescending(venta => venta.Fecha)
            .ToListAsync(cancellationToken);
    }

    public Task<Venta?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Ventas
            .Include(venta => venta.Detalles)
            .Include(venta => venta.Pagos)
            .SingleOrDefaultAsync(
                venta => venta.EmpresaId == empresaId && venta.Id == id,
            cancellationToken);
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
