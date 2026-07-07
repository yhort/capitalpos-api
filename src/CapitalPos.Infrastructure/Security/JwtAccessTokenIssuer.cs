using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CapitalPos.Application.Seguridad;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CapitalPos.Infrastructure.Security;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly JwtTokenOptions _options;

    public JwtAccessTokenIssuer(IOptions<JwtTokenOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        JwtTokenOptionsValidator.Validar(_options);
    }

    public AccessTokenResult Emitir(AccessTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UsuarioId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(request));
        }

        var correo = NormalizarCorreo(request.Correo);
        if (string.IsNullOrWhiteSpace(correo))
        {
            throw new ArgumentException("El correo del usuario es obligatorio.", nameof(request));
        }

        var ahora = DateTimeOffset.UtcNow;
        var expiraEn = ahora.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UsuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, correo),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var nombre = request.Nombre?.Trim();
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            claims.Add(new Claim("name", nombre));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: expiraEn.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(_tokenHandler.WriteToken(token), expiraEn);
    }

    private static string NormalizarCorreo(string? correo)
    {
        return correo?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
