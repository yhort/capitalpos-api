namespace CapitalPos.Application.Seguridad;

public sealed record AccessTokenRequest(
    Guid UsuarioId,
    string Correo,
    string? Nombre = null);
