namespace CapitalPos.Domain;

public sealed class PedidoDigitalDetalle
{
    private PedidoDigitalDetalle()
    {
        Descripcion = string.Empty;
    }

    public PedidoDigitalDetalle(
        Guid id,
        Guid empresaId,
        Guid pedidoDigitalId,
        Guid productoId,
        string descripcion,
        decimal cantidad,
        decimal precioUnitario,
        Guid? productoVarianteId = null,
        Guid? productoPresentacionId = null,
        decimal factorConversionAplicado = 1m,
        decimal? cantidadBase = null,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del detalle de pedido digital es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (pedidoDigitalId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del pedido digital es obligatorio.", nameof(pedidoDigitalId));
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

        var descripcionNormalizada = NormalizarTexto(descripcion);
        if (string.IsNullOrWhiteSpace(descripcionNormalizada))
        {
            throw new ArgumentException("La descripcion del detalle es obligatoria.", nameof(descripcion));
        }

        if (cantidad <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
        }

        if (precioUnitario <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioUnitario), "El precio unitario debe ser mayor que cero.");
        }

        if (factorConversionAplicado <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factorConversionAplicado), "El factor de conversion aplicado debe ser mayor que cero.");
        }

        var cantidadBaseNormalizada = cantidadBase ?? cantidad * factorConversionAplicado;
        if (cantidadBaseNormalizada <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadBase), "La cantidad base debe ser mayor que cero.");
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        PedidoDigitalId = pedidoDigitalId;
        ProductoId = productoId;
        ProductoVarianteId = productoVarianteId;
        ProductoPresentacionId = productoPresentacionId;
        Descripcion = descripcionNormalizada;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
        FactorConversionAplicado = factorConversionAplicado;
        CantidadBase = cantidadBaseNormalizada;
        Total = Redondear(cantidad * precioUnitario);
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid PedidoDigitalId { get; private set; }

    public Guid ProductoId { get; private set; }

    public Guid? ProductoVarianteId { get; private set; }

    public Guid? ProductoPresentacionId { get; private set; }

    public string Descripcion { get; private set; }

    public decimal Cantidad { get; private set; }

    public decimal PrecioUnitario { get; private set; }

    public decimal FactorConversionAplicado { get; private set; }

    public decimal CantidadBase { get; private set; }

    public decimal Total { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }

    private static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
