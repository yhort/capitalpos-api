using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public interface IVentaRepository
{
    Task AgregarAsync(Venta venta, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<Venta?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);
}
