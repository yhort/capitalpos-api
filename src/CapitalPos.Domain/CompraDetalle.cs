namespace CapitalPos.Domain;

public sealed class CompraDetalle
{
    private CompraDetalle()
    {
    }

    public CompraDetalle(
        Guid id,
        Guid empresaId,
        Guid compraId,
        Guid productoId,
        decimal cantidad,
        decimal costoUnitario,
        Guid? productoVarianteId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del detalle de compra es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (compraId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la compra es obligatorio.", nameof(compraId));
        }

        if (productoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto es obligatorio.", nameof(productoId));
        }

        if (productoVarianteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la variante no puede estar vacio.", nameof(productoVarianteId));
        }

        if (cantidad <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
        }

        if (costoUnitario < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costoUnitario), "El costo unitario no puede ser negativo.");
        }

        Id = id;
        EmpresaId = empresaId;
        CompraId = compraId;
        ProductoId = productoId;
        ProductoVarianteId = productoVarianteId;
        Cantidad = cantidad;
        CostoUnitario = Redondear(costoUnitario);
        Total = Redondear(cantidad * CostoUnitario);
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid CompraId { get; private set; }

    public Guid ProductoId { get; private set; }

    public Guid? ProductoVarianteId { get; private set; }

    public decimal Cantidad { get; private set; }

    public decimal CostoUnitario { get; private set; }

    public decimal Total { get; private set; }

    private static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
