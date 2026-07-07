using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CapitalPos.Application.Seguridad;
using CapitalPos.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CapitalPos.Tests;

public class JwtAccessTokenIssuerTests
{
    private const string SigningKey = "capitalpos-test-signing-key-32-chars-minimum";
    private const string OtherSigningKey = "capitalpos-other-signing-key-32-chars-min";

    [Fact]
    public void Emitir_crea_access_token_con_claims_minimos_y_sin_datos_sensibles()
    {
        var usuarioId = Guid.NewGuid();
        var issuer = CrearIssuer();

        var resultado = issuer.Emitir(new AccessTokenRequest(
            usuarioId,
            " USUARIO@CAPITALPOS.COM ",
            "Ada Lovelace"));

        var principal = ValidarToken(resultado.Token);
        Assert.Equal(usuarioId.ToString(), principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal("usuario@capitalpos.com", principal.FindFirstValue(JwtRegisteredClaimNames.Email));
        Assert.Equal("Ada Lovelace", principal.FindFirstValue("name"));
        Assert.False(string.IsNullOrWhiteSpace(principal.FindFirstValue(JwtRegisteredClaimNames.Jti)));
        Assert.DoesNotContain("PasswordHash", resultado.Token, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credencial", resultado.Token, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(
            resultado.ExpiraEn,
            DateTimeOffset.UtcNow.AddMinutes(14),
            DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void Emitir_omite_name_si_no_hay_nombre()
    {
        var issuer = CrearIssuer();

        var resultado = issuer.Emitir(new AccessTokenRequest(
            Guid.NewGuid(),
            "usuario@capitalpos.com"));

        var principal = ValidarToken(resultado.Token);
        Assert.Null(principal.FindFirstValue("name"));
    }

    [Fact]
    public void Validacion_acepta_token_valido()
    {
        var token = CrearIssuer().Emitir(new AccessTokenRequest(
            Guid.NewGuid(),
            "usuario@capitalpos.com")).Token;

        var principal = ValidarToken(token);

        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
    }

    [Fact]
    public void Validacion_rechaza_token_expirado()
    {
        var token = CrearTokenExpirado();

        Assert.Throws<SecurityTokenExpiredException>(() => ValidarToken(token));
    }

    [Fact]
    public void Validacion_rechaza_firma_incorrecta()
    {
        var token = CrearIssuer().Emitir(new AccessTokenRequest(
            Guid.NewGuid(),
            "usuario@capitalpos.com")).Token;

        Assert.ThrowsAny<SecurityTokenException>(() =>
            ValidarToken(token, signingKey: OtherSigningKey));
    }

    [Fact]
    public void Validacion_rechaza_issuer_incorrecto()
    {
        var token = CrearIssuer(issuer: "OtroIssuer").Emitir(new AccessTokenRequest(
            Guid.NewGuid(),
            "usuario@capitalpos.com")).Token;

        Assert.Throws<SecurityTokenInvalidIssuerException>(() => ValidarToken(token));
    }

    [Fact]
    public void Validacion_rechaza_audience_incorrecta()
    {
        var token = CrearIssuer(audience: "OtraAudience").Emitir(new AccessTokenRequest(
            Guid.NewGuid(),
            "usuario@capitalpos.com")).Token;

        Assert.Throws<SecurityTokenInvalidAudienceException>(() => ValidarToken(token));
    }

    [Fact]
    public void Crear_issuer_rechaza_signing_key_vacia()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CrearIssuer(signingKey: string.Empty));

        Assert.Contains("Jwt:SigningKey", exception.Message);
        Assert.DoesNotContain(SigningKey, exception.Message);
    }

    private static JwtAccessTokenIssuer CrearIssuer(
        string issuer = "CapitalPos.Api",
        string audience = "CapitalPos.Web",
        string signingKey = SigningKey,
        int accessTokenMinutes = 15)
    {
        return new JwtAccessTokenIssuer(Options.Create(new JwtTokenOptions
        {
            Issuer = issuer,
            Audience = audience,
            SigningKey = signingKey,
            AccessTokenMinutes = accessTokenMinutes
        }));
    }

    private static ClaimsPrincipal ValidarToken(
        string token,
        string issuer = "CapitalPos.Api",
        string audience = "CapitalPos.Web",
        string signingKey = SigningKey)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };

        return new JwtSecurityTokenHandler { MapInboundClaims = false }
            .ValidateToken(token, validationParameters, out _);
    }

    private static string CrearTokenExpirado()
    {
        var ahora = DateTimeOffset.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "CapitalPos.Api",
            audience: "CapitalPos.Web",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, "usuario@capitalpos.com"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: ahora.AddMinutes(-30).UtcDateTime,
            expires: ahora.AddMinutes(-15).UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
