namespace CapitalPos.Application.Inventario;

public sealed record AjustarStockProductoRequest(
    Guid ProductoId,
    Guid? ProductoVarianteId,
    decimal CantidadDisponible);
