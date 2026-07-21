using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public interface IProductoPresentacionRepository
{
    Task AgregarAsync(
        ProductoPresentacion presentacion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductoPresentacion>> ListarPorProductoAsync(
        Guid empresaId,
        Guid productoId,
        CancellationToken cancellationToken = default);

    Task<ProductoPresentacion?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);
}
