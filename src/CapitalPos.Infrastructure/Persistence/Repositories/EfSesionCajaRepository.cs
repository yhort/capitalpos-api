using CapitalPos.Application.Caja;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfSesionCajaRepository : ISesionCajaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfSesionCajaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sesionCaja);

        await _dbContext.SesionesCaja.AddAsync(sesionCaja, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<SesionCaja?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SesionesCaja
            .SingleOrDefaultAsync(
                sesion => sesion.EmpresaId == empresaId && sesion.Id == id,
                cancellationToken);
    }

    public Task<SesionCaja?> ObtenerAbiertaPorPuntoVentaAsync(
        Guid empresaId,
        Guid puntoVentaId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SesionesCaja
            .AsNoTracking()
            .SingleOrDefaultAsync(
                sesion =>
                    sesion.EmpresaId == empresaId &&
                    sesion.PuntoVentaId == puntoVentaId &&
                    sesion.Estado == EstadoSesionCaja.Abierta,
                cancellationToken);
    }

    public async Task GuardarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sesionCaja);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
