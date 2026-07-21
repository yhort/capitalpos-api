using CapitalPos.Domain;

namespace CapitalPos.Application.Caja;

public interface ISesionCajaRepository
{
    Task AgregarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default);

    Task<SesionCaja?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SesionCaja?> ObtenerAbiertaPorPuntoVentaAsync(
        Guid empresaId,
        Guid puntoVentaId,
        CancellationToken cancellationToken = default);

    Task GuardarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default);
}
