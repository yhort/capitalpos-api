namespace CapitalPos.Domain;

public sealed class PuntoVenta
{
    private PuntoVenta()
    {
        Nombre = string.Empty;
    }

    public PuntoVenta(
        Guid id,
        Guid empresaId,
        Guid sedeId,
        string nombre,
        bool activo = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        var nombreNormalizado = NormalizarTexto(nombre);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ArgumentException("El nombre del punto de venta es obligatorio.", nameof(nombre));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        SedeId = sedeId;
        Nombre = nombreNormalizado;
        Activo = activo;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid SedeId { get; private set; }

    public string Nombre { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

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
