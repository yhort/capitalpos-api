using CapitalPos.Application.Series;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfSerieComprobanteRepository : ISerieComprobanteRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfSerieComprobanteRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(SerieComprobante serie, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serie);

        await _dbContext.SeriesComprobante.AddAsync(serie, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SerieComprobante>> ListarPorSedeAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SeriesComprobante
            .AsNoTracking()
            .Where(serie => serie.EmpresaId == empresaId && serie.SedeId == sedeId)
            .OrderBy(serie => serie.TipoComprobante)
            .ThenBy(serie => serie.Serie)
            .ToListAsync(cancellationToken);
    }

    public Task<SerieComprobante?> ObtenerActivaAsync(
        Guid empresaId,
        Guid sedeId,
        string tipoComprobante,
        string serie,
        CancellationToken cancellationToken = default)
    {
        var tipoNormalizado = NormalizarTexto(tipoComprobante).ToUpperInvariant();
        var serieNormalizada = NormalizarTexto(serie).ToUpperInvariant();

        return _dbContext.SeriesComprobante
            .SingleOrDefaultAsync(
                serieComprobante =>
                    serieComprobante.EmpresaId == empresaId &&
                    serieComprobante.SedeId == sedeId &&
                    serieComprobante.TipoComprobante == tipoNormalizado &&
                    serieComprobante.Serie == serieNormalizada &&
                    serieComprobante.Activa,
                cancellationToken);
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
