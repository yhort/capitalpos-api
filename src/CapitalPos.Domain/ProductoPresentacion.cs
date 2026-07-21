namespace CapitalPos.Domain;

public sealed class ProductoPresentacion
{
    private ProductoPresentacion()
    {
        CodigoBarras = string.Empty;
    }

    public ProductoPresentacion(
        Guid id,
        Guid empresaId,
        Guid productoId,
        Guid unidadMedidaId,
        decimal factorConversion,
        bool esUnidadBase,
        decimal precioVenta,
        string? codigoBarras = null,
        bool activa = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la presentacion es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (productoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto es obligatorio.", nameof(productoId));
        }

        if (unidadMedidaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la unidad de medida es obligatorio.", nameof(unidadMedidaId));
        }

        if (factorConversion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factorConversion), "El factor de conversion debe ser mayor que cero.");
        }

        if (precioVenta <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioVenta), "El precio de venta debe ser mayor que cero.");
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        ProductoId = productoId;
        UnidadMedidaId = unidadMedidaId;
        FactorConversion = factorConversion;
        EsUnidadBase = esUnidadBase;
        PrecioVenta = precioVenta;
        CodigoBarras = NormalizarTexto(codigoBarras);
        Activa = activa;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid ProductoId { get; private set; }

    public Guid UnidadMedidaId { get; private set; }

    public decimal FactorConversion { get; private set; }

    public bool EsUnidadBase { get; private set; }

    public decimal PrecioVenta { get; private set; }

    public string CodigoBarras { get; private set; }

    public bool Activa { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public void Desactivar()
    {
        Activa = false;
    }

    public void Activar()
    {
        Activa = true;
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
