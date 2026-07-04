namespace CapitalPos.Domain;

public sealed class Usuario
{
    public Usuario(
        Guid id,
        string nombre,
        string apellido,
        string correo,
        bool activo = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(id));
        }

        var correoNormalizado = NormalizarCorreo(correo);
        if (string.IsNullOrWhiteSpace(correoNormalizado))
        {
            throw new ArgumentException("El correo es obligatorio.", nameof(correo));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        Nombre = NormalizarTexto(nombre);
        Apellido = NormalizarTexto(apellido);
        Correo = correoNormalizado;
        Activo = activo;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public string Nombre { get; private set; }

    public string Apellido { get; private set; }

    public string Correo { get; private set; }

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

    private static string NormalizarCorreo(string? correo)
    {
        return NormalizarTexto(correo).ToLowerInvariant();
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
