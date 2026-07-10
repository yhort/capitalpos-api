namespace CapitalPos.Domain;

public sealed class Producto
{
    private Producto()
    {
        Nombre = string.Empty;
        CodigoSku = string.Empty;
        CodigoBarras = string.Empty;
    }

    public Producto(
        Guid id,
        Guid empresaId,
        string nombre,
        decimal precioVenta,
        string? codigoSku = null,
        string? codigoBarras = null,
        decimal? costo = null,
        bool activo = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        var nombreNormalizado = NormalizarTexto(nombre);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(nombre));
        }

        if (precioVenta <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioVenta), "El precio de venta debe ser mayor que cero.");
        }

        if (costo < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costo), "El costo no puede ser negativo.");
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        Nombre = nombreNormalizado;
        CodigoSku = NormalizarTexto(codigoSku);
        CodigoBarras = NormalizarTexto(codigoBarras);
        PrecioVenta = precioVenta;
        Costo = costo;
        Activo = activo;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public string Nombre { get; private set; }

    public string CodigoSku { get; private set; }

    public string CodigoBarras { get; private set; }

    public decimal PrecioVenta { get; private set; }

    public decimal? Costo { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public void ActualizarDatosBasicos(
        string nombre,
        decimal precioVenta,
        string? codigoSku = null,
        string? codigoBarras = null,
        decimal? costo = null)
    {
        var nombreNormalizado = NormalizarTexto(nombre);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(nombre));
        }

        if (precioVenta <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioVenta), "El precio de venta debe ser mayor que cero.");
        }

        if (costo < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costo), "El costo no puede ser negativo.");
        }

        Nombre = nombreNormalizado;
        CodigoSku = NormalizarTexto(codigoSku);
        CodigoBarras = NormalizarTexto(codigoBarras);
        PrecioVenta = precioVenta;
        Costo = costo;
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
