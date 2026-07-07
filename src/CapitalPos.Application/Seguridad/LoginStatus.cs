namespace CapitalPos.Application.Seguridad;

public enum LoginStatus
{
    CredencialesValidas = 1,
    UsuarioNoEncontrado = 2,
    PasswordIncorrecto = 3,
    CredencialInactiva = 4,
    CredencialBloqueada = 5,
    CredencialNoEncontrada = 6
}
