namespace CapitalPos.Domain;

public sealed class UsuarioCredencial
{
    private UsuarioCredencial()
    {
        PasswordHash = string.Empty;
        Algoritmo = string.Empty;
    }

    public UsuarioCredencial(
        Guid usuarioId,
        string passwordHash,
        string algoritmo,
        DateTimeOffset? fechaCambio = null,
        bool activo = true,
        bool bloqueado = false)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(usuarioId));
        }

        var passwordHashNormalizado = NormalizarTexto(passwordHash);
        if (string.IsNullOrWhiteSpace(passwordHashNormalizado))
        {
            throw new ArgumentException("El hash de la contrasena es obligatorio.", nameof(passwordHash));
        }

        var algoritmoNormalizado = NormalizarTexto(algoritmo);
        if (string.IsNullOrWhiteSpace(algoritmoNormalizado))
        {
            throw new ArgumentException("El algoritmo de credencial es obligatorio.", nameof(algoritmo));
        }

        var fechaCambioNormalizada = fechaCambio ?? DateTimeOffset.UtcNow;
        if (fechaCambioNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCambio), "La fecha de cambio no es valida.");
        }

        UsuarioId = usuarioId;
        PasswordHash = passwordHashNormalizado;
        Algoritmo = algoritmoNormalizado;
        FechaCambio = fechaCambioNormalizada;
        Activo = activo;
        Bloqueado = bloqueado;
    }

    public Guid UsuarioId { get; private set; }

    public string PasswordHash { get; private set; }

    public string Algoritmo { get; private set; }

    public DateTimeOffset FechaCambio { get; private set; }

    public bool Activo { get; private set; }

    public bool Bloqueado { get; private set; }

    public void CambiarPasswordHash(
        string passwordHash,
        string algoritmo,
        DateTimeOffset? fechaCambio = null)
    {
        var passwordHashNormalizado = NormalizarTexto(passwordHash);
        if (string.IsNullOrWhiteSpace(passwordHashNormalizado))
        {
            throw new ArgumentException("El hash de la contrasena es obligatorio.", nameof(passwordHash));
        }

        var algoritmoNormalizado = NormalizarTexto(algoritmo);
        if (string.IsNullOrWhiteSpace(algoritmoNormalizado))
        {
            throw new ArgumentException("El algoritmo de credencial es obligatorio.", nameof(algoritmo));
        }

        var fechaCambioNormalizada = fechaCambio ?? DateTimeOffset.UtcNow;
        if (fechaCambioNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCambio), "La fecha de cambio no es valida.");
        }

        PasswordHash = passwordHashNormalizado;
        Algoritmo = algoritmoNormalizado;
        FechaCambio = fechaCambioNormalizada;
    }

    public void Desactivar()
    {
        Activo = false;
    }

    public void Activar()
    {
        Activo = true;
    }

    public void Bloquear()
    {
        Bloqueado = true;
    }

    public void Desbloquear()
    {
        Bloqueado = false;
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
