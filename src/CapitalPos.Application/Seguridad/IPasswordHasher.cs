using CapitalPos.Domain;

namespace CapitalPos.Application.Seguridad;

public interface IPasswordHasher
{
    string GenerarHash(UsuarioCredencial credencial, string password);

    PasswordVerificationResult Verificar(UsuarioCredencial credencial, string password);
}
