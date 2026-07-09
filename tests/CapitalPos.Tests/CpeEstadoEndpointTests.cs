using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Cpe;
using CapitalPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CapitalPos.Tests;

public class CpeEstadoEndpointTests
{
    private const string SigningKey = "capitalpos-http-integration-tests-signing-key-2026";
    private const string ApiKeyFicticia = "capitalpos-cpe-estado-tests-api-key";

    [Fact]
    public async Task Estado_requiere_autenticacion()
    {
        await using var factory = new CpeEstadoFactory(CpeEstadoMode.Exitoso);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cpe/estado");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Estado_devuelve_respuesta_normalizada_y_no_expone_api_key()
    {
        await using var factory = new CpeEstadoFactory(CpeEstadoMode.Exitoso);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorization();

        var response = await client.GetAsync("/api/cpe/estado");
        var content = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<CpeEstadoResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Ok);
        Assert.Equal("OK", body.Estado);
        Assert.Equal("CapitalPOS CPE API", body.Servicio);
        Assert.Equal("1.0.0", body.Version);
        Assert.Equal("BETA", body.Modo);
        Assert.False(body.SimularGeneracionXml);
        Assert.False(body.SimularFirma);
        Assert.True(body.SimularEnvioSunat);
        Assert.DoesNotContain(ApiKeyFicticia, content);
        Assert.DoesNotContain("X-API-KEY", content);
    }

    [Fact]
    public async Task Estado_maneja_cpe_no_disponible()
    {
        await using var factory = new CpeEstadoFactory(CpeEstadoMode.NoDisponible);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorization();

        using var httpResponse = await client.GetAsync("/api/cpe/estado");
        var response = await httpResponse.Content.ReadFromJsonAsync<CpeEstadoResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, httpResponse.StatusCode);
        Assert.NotNull(response);
        Assert.False(response.Ok);
        Assert.Equal("NO_DISPONIBLE", response.Estado);
    }

    [Fact]
    public async Task Estado_maneja_no_autorizado_de_cpe()
    {
        await using var factory = new CpeEstadoFactory(CpeEstadoMode.NoAutorizado);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorization();

        using var response = await client.GetAsync("/api/cpe/estado");
        var content = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<CpeEstadoResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body.Ok);
        Assert.Equal("NO_AUTORIZADO", body.Estado);
        Assert.Contains("configuracion interna", body.Mensaje);
        Assert.DoesNotContain(ApiKeyFicticia, content);
    }

    [Fact]
    public async Task Estado_maneja_respuesta_invalida()
    {
        await using var factory = new CpeEstadoFactory(CpeEstadoMode.RespuestaInvalida);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorization();

        using var response = await client.GetAsync("/api/cpe/estado");
        var body = await response.Content.ReadFromJsonAsync<CpeEstadoResponse>();

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body.Ok);
        Assert.Equal("RESPUESTA_INVALIDA", body.Estado);
    }

    private static AuthenticationHeaderValue CrearAuthorization()
    {
        return new AuthenticationHeaderValue("Bearer", HttpIntegrationTestTokenFactory.CrearJwt(SigningKey));
    }

    private sealed class CpeEstadoFactory : WebApplicationFactory<Program>
    {
        private readonly CpeEstadoMode _mode;

        public CpeEstadoFactory(CpeEstadoMode mode)
        {
            _mode = mode;
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__CapitalPos",
                "Host=localhost;Database=capitalpos_cpe_estado_tests");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "CapitalPos.Api");
            Environment.SetEnvironmentVariable("Jwt__Audience", "CapitalPos.Web");
            Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
            Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
            Environment.SetEnvironmentVariable("CpeApi__BaseUrl", "http://localhost/cpe-estado-tests/");
            Environment.SetEnvironmentVariable("CpeApi__ApiKey", ApiKeyFicticia);
            Environment.SetEnvironmentVariable("DemoSeed__Enabled", "false");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CapitalPos"] = "Host=localhost;Database=capitalpos_cpe_estado_tests",
                    ["Jwt:Issuer"] = "CapitalPos.Api",
                    ["Jwt:Audience"] = "CapitalPos.Web",
                    ["Jwt:SigningKey"] = SigningKey,
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["CpeApi:BaseUrl"] = "http://localhost/cpe-estado-tests/",
                    ["CpeApi:ApiKey"] = ApiKeyFicticia,
                    ["DemoSeed:Enabled"] = "false"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICpeGateway>();
                services.AddSingleton<ICpeGateway>(new CpeGatewayFake(_mode));
            });
        }
    }

    private sealed class CpeGatewayFake : ICpeGateway
    {
        private readonly CpeEstadoMode _mode;

        public CpeGatewayFake(CpeEstadoMode mode)
        {
            _mode = mode;
        }

        public Task<CpeGatewayResponse> ObtenerEstadoAsync(CancellationToken cancellationToken = default)
        {
            return _mode switch
            {
                CpeEstadoMode.Exitoso => Task.FromResult(new CpeGatewayResponse(
                    200,
                    true,
                    """
                    {
                      "ok": true,
                      "mensaje": "API CPE funcionando correctamente.",
                      "data": {
                        "status": "OK",
                        "service": "CapitalPOS CPE API",
                        "version": "1.0.0",
                        "modo": "BETA",
                        "simularGeneracionXml": false,
                        "simularFirma": false,
                        "simularEnvioSunat": true
                      },
                      "errores": []
                    }
                    """,
                    "application/json")),
                CpeEstadoMode.NoDisponible => throw new HttpRequestException("Servicio no disponible"),
                CpeEstadoMode.NoAutorizado => Task.FromResult(new CpeGatewayResponse(
                    401,
                    false,
                    """{"mensaje":"api-key-secreta"}""",
                    "application/json")),
                CpeEstadoMode.RespuestaInvalida => Task.FromResult(new CpeGatewayResponse(
                    200,
                    true,
                    "{ json-invalido }",
                    "application/json")),
                _ => throw new InvalidOperationException("Modo no soportado.")
            };
        }

        public Task<CpeGatewayResponse> EmitirAsync(
            JsonElement request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CpeGatewayResponse(
                200,
                true,
                """{"ok":true,"data":{"ok":true,"estado":"ACEPTADO"}}""",
                "application/json"));
        }
    }

    private enum CpeEstadoMode
    {
        Exitoso,
        NoDisponible,
        NoAutorizado,
        RespuestaInvalida
    }
}

internal static class HttpIntegrationTestTokenFactory
{
    public static string CrearJwt(string signingKey)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "usuario.cpe.estado@capitalpos.test"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key,
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            "CapitalPos.Api",
            "CapitalPos.Web",
            claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
