namespace CapitalPos.Domain;

public sealed class ReglaPrecioMayorista
{
    private ReglaPrecioMayorista()
    {
    }

    public ReglaPrecioMayorista(
        Guid id,
        Guid empresaId,
        Guid productoId,
        int cantidadMinima,
        decimal precioUnitarioMayorista,
        bool activa = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la regla de precio mayorista es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (productoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto es obligatorio.", nameof(productoId));
        }

        if (cantidadMinima <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadMinima), "La cantidad minima debe ser mayor que cero.");
        }

        if (precioUnitarioMayorista <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioUnitarioMayorista), "El precio unitario mayorista debe ser mayor que cero.");
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        ProductoId = productoId;
        CantidadMinima = cantidadMinima;
        PrecioUnitarioMayorista = precioUnitarioMayorista;
        Activa = activa;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid ProductoId { get; private set; }

    public int CantidadMinima { get; private set; }

    public decimal PrecioUnitarioMayorista { get; private set; }

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
}
