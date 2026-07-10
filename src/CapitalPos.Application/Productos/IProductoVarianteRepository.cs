using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public interface IProductoVarianteRepository
{
    Task AgregarAsync(
        ProductoVariante variante,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
        Guid empresaId,
        Guid productoId,
        CancellationToken cancellationToken = default);
}
