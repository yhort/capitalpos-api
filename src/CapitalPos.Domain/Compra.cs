namespace CapitalPos.Domain;

public sealed class Compra
{
    private readonly List<CompraDetalle> _detalles = new();

    private Compra()
    {
        Proveedor = string.Empty;
        TipoComprobante = string.Empty;
        Serie = string.Empty;
        Correlativo = string.Empty;
    }

    public Compra(
        Guid id,
        Guid empresaId,
        Guid sedeId,
        string proveedor,
        string tipoComprobante,
        string serie,
        string correlativo,
        DateTimeOffset fechaCompra,
        IReadOnlyCollection<CompraDetalle> detalles,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la compra es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        var proveedorNormalizado = NormalizarTexto(proveedor);
        if (string.IsNullOrWhiteSpace(proveedorNormalizado))
        {
            throw new ArgumentException("El proveedor es obligatorio.", nameof(proveedor));
        }

        if (proveedorNormalizado.Length > 200)
        {
            throw new ArgumentException("El proveedor no debe exceder 200 caracteres.", nameof(proveedor));
        }

        var tipoComprobanteNormalizado = NormalizarTexto(tipoComprobante).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(tipoComprobanteNormalizado))
        {
            throw new ArgumentException("El tipo de comprobante es obligatorio.", nameof(tipoComprobante));
        }

        if (tipoComprobanteNormalizado.Length > 30)
        {
            throw new ArgumentException("El tipo de comprobante no debe exceder 30 caracteres.", nameof(tipoComprobante));
        }

        var serieNormalizada = NormalizarTexto(serie).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(serieNormalizada))
        {
            throw new ArgumentException("La serie del comprobante es obligatoria.", nameof(serie));
        }

        if (serieNormalizada.Length > 20)
        {
            throw new ArgumentException("La serie no debe exceder 20 caracteres.", nameof(serie));
        }

        var correlativoNormalizado = NormalizarTexto(correlativo);
        if (string.IsNullOrWhiteSpace(correlativoNormalizado))
        {
            throw new ArgumentException("El correlativo del comprobante es obligatorio.", nameof(correlativo));
        }

        if (correlativoNormalizado.Length > 20)
        {
            throw new ArgumentException("El correlativo no debe exceder 20 caracteres.", nameof(correlativo));
        }

        if (fechaCompra == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCompra), "La fecha de compra no es valida.");
        }

        ArgumentNullException.ThrowIfNull(detalles);
        if (detalles.Count == 0)
        {
            throw new ArgumentException("La compra debe tener al menos un detalle.", nameof(detalles));
        }

        if (detalles.Any(detalle => detalle.EmpresaId != empresaId || detalle.CompraId != id))
        {
            throw new ArgumentException("Todos los detalles deben pertenecer a la misma compra y empresa.", nameof(detalles));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        SedeId = sedeId;
        Proveedor = proveedorNormalizado;
        TipoComprobante = tipoComprobanteNormalizado;
        Serie = serieNormalizada;
        Correlativo = correlativoNormalizado;
        FechaCompra = fechaCompra;
        Total = detalles.Sum(detalle => detalle.Total);
        FechaCreacion = fechaCreacionNormalizada;
        _detalles.AddRange(detalles);
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid SedeId { get; private set; }

    public string Proveedor { get; private set; }

    public string TipoComprobante { get; private set; }

    public string Serie { get; private set; }

    public string Correlativo { get; private set; }

    public DateTimeOffset FechaCompra { get; private set; }

    public decimal Total { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public IReadOnlyCollection<CompraDetalle> Detalles => _detalles;

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
