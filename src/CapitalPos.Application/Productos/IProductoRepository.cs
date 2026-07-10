using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public interface IProductoRepository
{
    Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<Producto?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        Producto producto,
        CancellationToken cancellationToken = default);
}
