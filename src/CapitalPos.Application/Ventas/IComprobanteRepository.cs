using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public interface IComprobanteRepository
{
    Task AgregarAsync(Comprobante comprobante, CancellationToken cancellationToken = default);
}
