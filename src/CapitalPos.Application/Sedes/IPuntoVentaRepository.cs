using CapitalPos.Domain;

namespace CapitalPos.Application.Sedes;

public interface IPuntoVentaRepository
{
    Task AgregarAsync(PuntoVenta puntoVenta, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PuntoVenta>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PuntoVenta>> ListarPorSedeAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken = default);

    Task<PuntoVenta?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);
}
