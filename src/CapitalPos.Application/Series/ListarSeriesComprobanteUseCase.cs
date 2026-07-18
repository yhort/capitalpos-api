using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Series;

public sealed class ListarSeriesComprobanteUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly ISedeRepository _sedeRepository;
    private readonly ISerieComprobanteRepository _serieRepository;

    public ListarSeriesComprobanteUseCase(
        ISerieComprobanteRepository serieRepository,
        ISedeRepository sedeRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _serieRepository = serieRepository;
        _sedeRepository = sedeRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<IReadOnlyCollection<SerieComprobante>?> EjecutarAsync(
        Guid sedeId,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        var sede = await _sedeRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            sedeId,
            cancellationToken);
        if (sede is null || !sede.Activa)
        {
            return null;
        }

        var series = await _serieRepository.ListarPorSedeAsync(
            _empresaActiva.EmpresaId,
            sedeId,
            cancellationToken);

        return series
            .Where(serie => serie.Activa)
            .OrderBy(serie => serie.TipoComprobante, StringComparer.Ordinal)
            .ThenBy(serie => serie.Serie, StringComparer.Ordinal)
            .ToArray();
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar series de comprobante.");
        }
    }
}
