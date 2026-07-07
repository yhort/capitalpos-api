namespace CapitalPos.Infrastructure.Security;

public static class JwtTokenOptionsValidator
{
    public static void Validar(JwtTokenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("La configuracion 'Jwt:Issuer' es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("La configuracion 'Jwt:Audience' es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new InvalidOperationException(
                "La configuracion segura 'Jwt:SigningKey' es obligatoria. Configurela con dotnet user-secrets o variables de entorno.");
        }

        if (options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "La configuracion segura 'Jwt:SigningKey' debe tener al menos 32 caracteres.");
        }

        if (options.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException("La configuracion 'Jwt:AccessTokenMinutes' debe ser mayor que cero.");
        }
    }
}
