namespace CapitalPos.Application.Seguridad;

public sealed record LoginResult(
    LoginStatus Status,
    Guid? UsuarioId = null,
    string? Correo = null,
    bool RequiereRehash = false)
{
    public bool EsValido => Status == LoginStatus.CredencialesValidas;
}
