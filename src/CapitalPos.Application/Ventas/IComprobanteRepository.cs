using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public interface IComprobanteRepository
{
    Task AgregarAsync(Comprobante comprobante, CancellationToken cancellationToken = default);

    Task<bool> ExistePorVentaAsync(
        Guid empresaId,
        Guid ventaId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
