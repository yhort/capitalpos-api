namespace CapitalPos.Domain;

public sealed class VentaPago
{
    private VentaPago()
    {
    }

    public VentaPago(
        Guid id,
        Guid empresaId,
        Guid ventaId,
        MetodoPago metodoPago,
        decimal monto,
        string? codigoOperacion = null,
        string? observacion = null,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del pago es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (ventaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la venta es obligatorio.", nameof(ventaId));
        }

        if (!Enum.IsDefined(metodoPago))
        {
            throw new ArgumentOutOfRangeException(nameof(metodoPago), "El metodo de pago no es valido.");
        }

        if (monto <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto del pago debe ser mayor que cero.");
        }

        var codigoOperacionNormalizado = NormalizarTexto(codigoOperacion);
        var observacionNormalizada = NormalizarTexto(observacion);
        if (codigoOperacionNormalizado?.Length > 100)
        {
            throw new ArgumentException("El codigo de operacion no puede superar 100 caracteres.", nameof(codigoOperacion));
        }

        if (observacionNormalizada?.Length > 500)
        {
            throw new ArgumentException("La observacion del pago no puede superar 500 caracteres.", nameof(observacion));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        VentaId = ventaId;
        MetodoPago = metodoPago;
        Monto = monto;
        CodigoOperacion = codigoOperacionNormalizado;
        Observacion = observacionNormalizada;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid VentaId { get; private set; }

    public MetodoPago MetodoPago { get; private set; }

    public decimal Monto { get; private set; }

    public string? CodigoOperacion { get; private set; }

    public string? Observacion { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    private static string? NormalizarTexto(string? valor)
    {
        var normalizado = valor?.Trim();
        return string.IsNullOrWhiteSpace(normalizado) ? null : normalizado;
    }
}
