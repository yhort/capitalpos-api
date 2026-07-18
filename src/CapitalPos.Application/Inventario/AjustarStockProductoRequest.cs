namespace CapitalPos.Application.Inventario;

public sealed record AjustarStockProductoRequest(
    Guid SedeId,
    Guid ProductoId,
    Guid? ProductoVarianteId,
    decimal CantidadDisponible);
