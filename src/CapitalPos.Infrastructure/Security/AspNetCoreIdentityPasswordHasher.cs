using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;
using Microsoft.AspNetCore.Identity;
using AppPasswordVerificationResult = CapitalPos.Application.Seguridad.PasswordVerificationResult;
using IdentityPasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace CapitalPos.Infrastructure.Security;

public sealed class AspNetCoreIdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<UsuarioCredencial> _passwordHasher = new();

    public string GenerarHash(UsuarioCredencial credencial, string password)
    {
        ArgumentNullException.ThrowIfNull(credencial);
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("La contrasena es obligatoria.", nameof(password));
        }

        return _passwordHasher.HashPassword(credencial, password);
    }

    public AppPasswordVerificationResult Verificar(UsuarioCredencial credencial, string password)
    {
        ArgumentNullException.ThrowIfNull(credencial);
        if (string.IsNullOrWhiteSpace(password))
        {
            return new AppPasswordVerificationResult(EsValida: false, RequiereRehash: false);
        }

        var resultado = _passwordHasher.VerifyHashedPassword(
            credencial,
            credencial.PasswordHash,
            password);

        return resultado switch
        {
            IdentityPasswordVerificationResult.Success =>
                new AppPasswordVerificationResult(EsValida: true, RequiereRehash: false),
            IdentityPasswordVerificationResult.SuccessRehashNeeded =>
                new AppPasswordVerificationResult(EsValida: true, RequiereRehash: true),
            _ => new AppPasswordVerificationResult(EsValida: false, RequiereRehash: false)
        };
    }
}
