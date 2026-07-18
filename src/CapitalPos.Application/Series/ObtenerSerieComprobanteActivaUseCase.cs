using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Series;

public sealed class ObtenerSerieComprobanteActivaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly ISedeRepository _sedeRepository;
    private readonly ISerieComprobanteRepository _serieRepository;

    public ObtenerSerieComprobanteActivaUseCase(
        ISerieComprobanteRepository serieRepository,
        ISedeRepository sedeRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _serieRepository = serieRepository;
        _sedeRepository = sedeRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<SerieComprobante?> EjecutarAsync(
        Guid sedeId,
        string tipoComprobante,
        string serie,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        if (string.IsNullOrWhiteSpace(tipoComprobante))
        {
            throw new ArgumentException("El tipo de comprobante es obligatorio.", nameof(tipoComprobante));
        }

        if (string.IsNullOrWhiteSpace(serie))
        {
            throw new ArgumentException("La serie del comprobante es obligatoria.", nameof(serie));
        }

        var sede = await _sedeRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            sedeId,
            cancellationToken);
        if (sede is null || !sede.Activa)
        {
            return null;
        }

        return await _serieRepository.ObtenerActivaAsync(
            _empresaActiva.EmpresaId,
            sedeId,
            tipoComprobante,
            serie,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar series de comprobante.");
        }
    }
}
