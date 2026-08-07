using CapitalPos.Application.Ventas;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfComprobanteRepository : IComprobanteRepository
{
    private static readonly string[] TiposEmision = { "01", "03" };
    private static readonly string[] EstadosAceptados = { "ACEPTADO", "SIMULADO" };

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

    public Task<Comprobante?> ObtenerEmisionAceptadaPorVentaAsync(
        Guid empresaId,
        Guid ventaId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Comprobantes
            .Where(comprobante =>
                comprobante.EmpresaId == empresaId &&
                comprobante.VentaId == ventaId &&
                TiposEmision.Contains(comprobante.TipoComprobante) &&
                EstadosAceptados.Contains(comprobante.EstadoCpe))
            .OrderByDescending(comprobante => comprobante.FechaCreacion)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Comprobante?> ObtenerNotaCreditoAceptadaPorComprobanteAfectadoAsync(
        Guid empresaId,
        Guid comprobanteAfectadoId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Comprobantes
            .Where(comprobante =>
                comprobante.EmpresaId == empresaId &&
                comprobante.TipoComprobante == "07" &&
                comprobante.ComprobanteAfectadoId == comprobanteAfectadoId &&
                EstadosAceptados.Contains(comprobante.EstadoCpe))
            .OrderByDescending(comprobante => comprobante.FechaCreacion)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
