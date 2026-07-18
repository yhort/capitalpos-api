namespace CapitalPos.Domain;

public sealed class SerieComprobante
{
    private SerieComprobante()
    {
        TipoComprobante = string.Empty;
        Serie = string.Empty;
    }

    public SerieComprobante(
        Guid id,
        Guid empresaId,
        Guid sedeId,
        string tipoComprobante,
        string serie,
        int correlativoActual,
        bool activa = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la serie es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
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

        if (correlativoActual < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(correlativoActual), "El correlativo actual no puede ser negativo.");
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        SedeId = sedeId;
        TipoComprobante = tipoComprobanteNormalizado;
        Serie = serieNormalizada;
        CorrelativoActual = correlativoActual;
        Activa = activa;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid SedeId { get; private set; }

    public string TipoComprobante { get; private set; }

    public string Serie { get; private set; }

    public int CorrelativoActual { get; private set; }

    public bool Activa { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public int ObtenerSiguienteCorrelativo()
    {
        return CorrelativoActual + 1;
    }

    public int IncrementarCorrelativo()
    {
        CorrelativoActual++;

        return CorrelativoActual;
    }

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
