using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed record CrearProductoPresentacionRequest(
    Guid ProductoId,
    Guid UnidadMedidaId,
    decimal FactorConversion,
    bool EsUnidadBase,
    decimal PrecioVenta,
    string? CodigoBarras = null)
{
    public ProductoPresentacion CrearPresentacion(Guid empresaId)
    {
        return new ProductoPresentacion(
            Guid.NewGuid(),
            empresaId,
            ProductoId,
            UnidadMedidaId,
            FactorConversion,
            EsUnidadBase,
            PrecioVenta,
            CodigoBarras);
    }
}
