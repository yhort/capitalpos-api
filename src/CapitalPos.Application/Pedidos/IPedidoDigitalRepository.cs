using CapitalPos.Domain;

namespace CapitalPos.Application.Pedidos;

public interface IPedidoDigitalRepository
{
    Task AgregarAsync(PedidoDigital pedido, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PedidoDigital>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<PedidoDigital?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
