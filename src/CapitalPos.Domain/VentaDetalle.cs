namespace CapitalPos.Domain;

public sealed class VentaDetalle
{
    private VentaDetalle()
    {
    }

    public VentaDetalle(
        Guid id,
        Guid empresaId,
        Guid ventaId,
        Guid productoId,
        decimal cantidad,
        decimal precioUnitario,
        decimal igv,
        decimal total,
        Guid? productoVarianteId = null,
        Guid? productoPresentacionId = null,
        decimal factorConversionAplicado = 1m,
        decimal? cantidadBaseDescontada = null,
        bool precioMayoristaAplicado = false)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del detalle de venta es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (ventaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la venta es obligatorio.", nameof(ventaId));
        }

        if (productoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto es obligatorio.", nameof(productoId));
        }

        if (productoVarianteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la variante no puede estar vacio.", nameof(productoVarianteId));
        }

        if (productoPresentacionId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la presentacion no puede estar vacio.", nameof(productoPresentacionId));
        }

        if (cantidad <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
        }

        if (precioUnitario <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioUnitario), "El precio unitario debe ser mayor que cero.");
        }

        if (igv < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(igv), "El IGV no puede ser negativo.");
        }

        if (total <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "El total del detalle debe ser mayor que cero.");
        }

        if (factorConversionAplicado <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factorConversionAplicado), "El factor de conversion aplicado debe ser mayor que cero.");
        }

        var cantidadBaseDescontadaNormalizada = cantidadBaseDescontada ?? cantidad * factorConversionAplicado;
        if (cantidadBaseDescontadaNormalizada <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadBaseDescontada), "La cantidad base descontada debe ser mayor que cero.");
        }

        Id = id;
        EmpresaId = empresaId;
        VentaId = ventaId;
        ProductoId = productoId;
        ProductoVarianteId = productoVarianteId;
        ProductoPresentacionId = productoPresentacionId;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
        Igv = igv;
        Total = total;
        FactorConversionAplicado = factorConversionAplicado;
        CantidadBaseDescontada = cantidadBaseDescontadaNormalizada;
        PrecioMayoristaAplicado = precioMayoristaAplicado;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid VentaId { get; private set; }

    public Guid ProductoId { get; private set; }

    public Guid? ProductoVarianteId { get; private set; }

    public Guid? ProductoPresentacionId { get; private set; }

    public decimal Cantidad { get; private set; }

    public decimal PrecioUnitario { get; private set; }

    public decimal Igv { get; private set; }

    public decimal Total { get; private set; }

    public decimal FactorConversionAplicado { get; private set; }

    public decimal CantidadBaseDescontada { get; private set; }

    public bool PrecioMayoristaAplicado { get; private set; }

    public void AplicarPrecioMayorista(decimal precioUnitarioMayorista)
    {
        if (precioUnitarioMayorista <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioUnitarioMayorista), "El precio unitario mayorista debe ser mayor que cero.");
        }

        PrecioUnitario = precioUnitarioMayorista;
        Total = Redondear(Cantidad * precioUnitarioMayorista);
        var subtotal = Redondear(Total / 1.18m);
        Igv = Redondear(Total - subtotal);
        PrecioMayoristaAplicado = true;
    }

    private static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
