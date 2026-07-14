using CapitalPos.Domain;

namespace CapitalPos.Application.Inventario;

public interface IStockProductoRepository
{
    Task<StockProducto?> ObtenerPorProductoAsync(
        Guid empresaId,
        Guid productoId,
        Guid? productoVarianteId = null,
        CancellationToken cancellationToken = default);

    Task GuardarAsync(
        StockProducto stock,
        CancellationToken cancellationToken = default);
}
