namespace CapitalPos.Domain;

public sealed class Comprobante
{
    private Comprobante()
    {
        TipoComprobante = string.Empty;
        Serie = string.Empty;
        EstadoCpe = string.Empty;
        Mensaje = string.Empty;
        Hash = string.Empty;
        NombreXml = string.Empty;
        NombreZip = string.Empty;
        NombreCdr = string.Empty;
    }

    public Comprobante(
        Guid id,
        Guid empresaId,
        Guid ventaId,
        string tipoComprobante,
        string serie,
        int correlativo,
        string estadoCpe,
        string? mensaje = null,
        string? hash = null,
        string? nombreXml = null,
        string? nombreZip = null,
        string? nombreCdr = null,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del comprobante es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (ventaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la venta es obligatorio.", nameof(ventaId));
        }

        var tipoComprobanteNormalizado = NormalizarTexto(tipoComprobante).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(tipoComprobanteNormalizado))
        {
            throw new ArgumentException("El tipo de comprobante es obligatorio.", nameof(tipoComprobante));
        }

        var serieNormalizada = NormalizarTexto(serie).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(serieNormalizada))
        {
            throw new ArgumentException("La serie del comprobante es obligatoria.", nameof(serie));
        }

        if (correlativo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(correlativo), "El correlativo debe ser mayor que cero.");
        }

        var estadoNormalizado = NormalizarTexto(estadoCpe).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(estadoNormalizado))
        {
            throw new ArgumentException("El estado CPE es obligatorio.", nameof(estadoCpe));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        VentaId = ventaId;
        TipoComprobante = tipoComprobanteNormalizado;
        Serie = serieNormalizada;
        Correlativo = correlativo;
        EstadoCpe = estadoNormalizado;
        Mensaje = NormalizarTexto(mensaje);
        Hash = NormalizarTexto(hash);
        NombreXml = NormalizarTexto(nombreXml);
        NombreZip = NormalizarTexto(nombreZip);
        NombreCdr = NormalizarTexto(nombreCdr);
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid VentaId { get; private set; }

    public string TipoComprobante { get; private set; }

    public string Serie { get; private set; }

    public int Correlativo { get; private set; }

    public string EstadoCpe { get; private set; }

    public string Mensaje { get; private set; }

    public string Hash { get; private set; }

    public string NombreXml { get; private set; }

    public string NombreZip { get; private set; }

    public string NombreCdr { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
