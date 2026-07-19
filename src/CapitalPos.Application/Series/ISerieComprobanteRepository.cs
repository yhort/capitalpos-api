using CapitalPos.Domain;

namespace CapitalPos.Application.Series;

public interface ISerieComprobanteRepository
{
    Task AgregarAsync(SerieComprobante serie, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SerieComprobante>> ListarPorSedeAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken = default);

    Task<SerieComprobante?> ObtenerActivaAsync(
        Guid empresaId,
        Guid sedeId,
        string tipoComprobante,
        string serie,
        CancellationToken cancellationToken = default);

    Task<SerieComprobante?> ObtenerActivaPorSedeYTipoAsync(
        Guid empresaId,
        Guid sedeId,
        string tipoComprobante,
        CancellationToken cancellationToken = default);

    Task GuardarAsync(SerieComprobante serie, CancellationToken cancellationToken = default);
}
