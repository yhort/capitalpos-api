using CapitalPos.Domain;

namespace CapitalPos.Application.Compras;

public interface ICompraRepository
{
    Task AgregarAsync(Compra compra, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Compra>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<Compra?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);
}
