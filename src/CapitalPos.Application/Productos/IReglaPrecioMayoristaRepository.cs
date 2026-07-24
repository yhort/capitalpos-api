using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public interface IReglaPrecioMayoristaRepository
{
    Task AgregarAsync(
        ReglaPrecioMayorista regla,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarPorProductoAsync(
        Guid empresaId,
        Guid productoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarActivasPorProductosAsync(
        Guid empresaId,
        IReadOnlyCollection<Guid> productoIds,
        CancellationToken cancellationToken = default);

    Task<ReglaPrecioMayorista?> ObtenerPorEmpresaYProductoAsync(
        Guid empresaId,
        Guid productoId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteActivaPorCantidadMinimaAsync(
        Guid empresaId,
        Guid productoId,
        int cantidadMinima,
        Guid? excluirId = null,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        ReglaPrecioMayorista regla,
        CancellationToken cancellationToken = default);
}
