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

    Task<ProductoVariante?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        ProductoVariante variante,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteSkuAsync(
        Guid empresaId,
        string codigoSku,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteCodigoBarrasAsync(
        Guid empresaId,
        string codigoBarras,
        CancellationToken cancellationToken = default);
}
