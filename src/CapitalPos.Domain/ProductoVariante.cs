namespace CapitalPos.Domain;

public sealed class ProductoVariante
{
    private ProductoVariante()
    {
        Talla = string.Empty;
        Color = string.Empty;
        CodigoSku = string.Empty;
        CodigoBarras = string.Empty;
    }

    public ProductoVariante(
        Guid id,
        Guid empresaId,
        Guid productoId,
        string? talla = null,
        string? color = null,
        string? codigoSku = null,
        string? codigoBarras = null,
        decimal stockActual = 0,
        bool activo = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la variante es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (productoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto es obligatorio.", nameof(productoId));
        }

        if (stockActual < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockActual), "El stock de la variante no puede ser negativo.");
        }

        var tallaNormalizada = NormalizarTexto(talla);
        var colorNormalizado = NormalizarTexto(color);
        var codigoSkuNormalizado = NormalizarTexto(codigoSku);
        var codigoBarrasNormalizado = NormalizarTexto(codigoBarras);
        if (string.IsNullOrWhiteSpace(tallaNormalizada)
            && string.IsNullOrWhiteSpace(colorNormalizado)
            && string.IsNullOrWhiteSpace(codigoSkuNormalizado)
            && string.IsNullOrWhiteSpace(codigoBarrasNormalizado))
        {
            throw new ArgumentException(
                "La variante debe tener talla, color, SKU o codigo de barras.",
                nameof(talla));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        ProductoId = productoId;
        Talla = tallaNormalizada;
        Color = colorNormalizado;
        CodigoSku = codigoSkuNormalizado;
        CodigoBarras = codigoBarrasNormalizado;
        StockActual = stockActual;
        Activo = activo;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid ProductoId { get; private set; }

    public string Talla { get; private set; }

    public string Color { get; private set; }

    public string CodigoSku { get; private set; }

    public string CodigoBarras { get; private set; }

    public decimal StockActual { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public void ActualizarDatosBasicos(
        string? talla = null,
        string? color = null,
        string? codigoSku = null,
        string? codigoBarras = null)
    {
        var tallaNormalizada = NormalizarTexto(talla);
        var colorNormalizada = NormalizarTexto(color);
        var codigoSkuNormalizado = NormalizarTexto(codigoSku);
        var codigoBarrasNormalizado = NormalizarTexto(codigoBarras);
        if (string.IsNullOrWhiteSpace(tallaNormalizada)
            && string.IsNullOrWhiteSpace(colorNormalizada)
            && string.IsNullOrWhiteSpace(codigoSkuNormalizado)
            && string.IsNullOrWhiteSpace(codigoBarrasNormalizado))
        {
            throw new ArgumentException(
                "La variante debe tener talla, color, SKU o codigo de barras.",
                nameof(talla));
        }

        Talla = tallaNormalizada;
        Color = colorNormalizada;
        CodigoSku = codigoSkuNormalizado;
        CodigoBarras = codigoBarrasNormalizado;
    }

    public void ActualizarStock(decimal stockActual)
    {
        if (stockActual < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockActual), "El stock de la variante no puede ser negativo.");
        }

        StockActual = stockActual;
    }

    public void Desactivar()
    {
        Activo = false;
    }

    public void Activar()
    {
        Activo = true;
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
