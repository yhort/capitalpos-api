using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CapitalPos.Tests;

public class AuthEndpointTests
{
    private static readonly Guid UsuarioId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb");
    private const string SigningKey = "capitalpos-http-integration-tests-signing-key-2026";
    private const string ApiKeyFicticia = "capitalpos-auth-tests-cpe-api-key";
    private const string PasswordValido = "password-correcto";
    private const string PasswordIncorrecto = "password-incorrecto";

    [Fact]
    public async Task Login_exitoso_devuelve_access_token_y_usuario_basico()
    {
        await using var factory = new AuthEndpointFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new AuthLoginRequest(" USUARIO.AUTH@CAPITALPOS.TEST ", PasswordValido));
        var content = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<AuthLoginResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal("Bearer", body.TokenType);
        Assert.InRange(body.ExpiresIn, 1, 900);
        Assert.True(body.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(UsuarioId, body.Usuario.Id);
        Assert.Equal("usuario.auth@capitalpos.test", body.Usuario.Correo);
        AssertSeguro(content);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type is "EmpresaId" or "empresaId" or "Rol" or "rol");
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_no_requiere_authorization_ni_empresa_activa()
    {
        await using var factory = new AuthEndpointFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(EmpresaActivaHeaders.HeaderName, Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new AuthLoginRequest("usuario.auth@capitalpos.test", PasswordValido));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_con_usuario_no_encontrado_devuelve_error_generico()
    {
        await using var factory = new AuthEndpointFactory(usuarioExiste: false);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new AuthLoginRequest("nadie@capitalpos.test", PasswordValido));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("credenciales", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no encontrado", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nadie@capitalpos.test", content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Login_con_password_incorrecto_devuelve_el_mismo_error_generico()
    {
        await using var factory = new AuthEndpointFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new AuthLoginRequest("usuario.auth@capitalpos.test", PasswordIncorrecto));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("credenciales", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("incorrect", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("usuario.auth@capitalpos.test", content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Login_con_credencial_inactiva_devuelve_error_seguro()
    {
        await using var factory = new AuthEndpointFactory(credencialActiva: false);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new AuthLoginRequest("usuario.auth@capitalpos.test", PasswordValido));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("credenciales", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inactiva", content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Login_con_credencial_bloqueada_devuelve_error_seguro()
    {
        await using var factory = new AuthEndpointFactory(credencialBloqueada: true);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new AuthLoginRequest("usuario.auth@capitalpos.test", PasswordValido));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("credenciales", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bloqueada", content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Login_con_request_invalido_devuelve_bad_request_seguro()
    {
        await using var factory = new AuthEndpointFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new AuthLoginRequest("", ""));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("correo", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("contrasena", content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public void Auth_endpoint_permanece_publico_y_sin_empresa_activa()
    {
        var source = File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "src", "CapitalPos.Api", "Endpoints", "AuthEndpoints.cs"));

        Assert.Contains("MapPost(\"/login\"", source);
        Assert.DoesNotContain("RequireAuthorization", source);
        Assert.DoesNotContain("EmpresaActivaEndpointFilter", source);
        Assert.DoesNotContain("RequirePermisoEmpresa", source);
    }

    private static void AssertSeguro(string content)
    {
        Assert.DoesNotContain(SigningKey, content, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKeyFicticia, content, StringComparison.Ordinal);
        Assert.DoesNotContain(PasswordValido, content, StringComparison.Ordinal);
        Assert.DoesNotContain(PasswordIncorrecto, content, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EmpresaId", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Rol", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", content, StringComparison.OrdinalIgnoreCase);

    }

    private sealed class AuthEndpointFactory : WebApplicationFactory<Program>
    {
        private readonly bool _credencialActiva;
        private readonly bool _credencialBloqueada;
        private readonly bool _usuarioExiste;

        public AuthEndpointFactory(
            bool usuarioExiste = true,
            bool credencialActiva = true,
            bool credencialBloqueada = false)
        {
            _usuarioExiste = usuarioExiste;
            _credencialActiva = credencialActiva;
            _credencialBloqueada = credencialBloqueada;
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__CapitalPos",
                "Host=localhost;Database=capitalpos_auth_endpoint_tests");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "CapitalPos.Api");
            Environment.SetEnvironmentVariable("Jwt__Audience", "CapitalPos.Web");
            Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
            Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
            Environment.SetEnvironmentVariable("CpeApi__BaseUrl", "http://localhost/capitalpos-cpe-auth-tests/");
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
                    ["ConnectionStrings:CapitalPos"] = "Host=localhost;Database=capitalpos_auth_endpoint_tests",
                    ["Jwt:Issuer"] = "CapitalPos.Api",
                    ["Jwt:Audience"] = "CapitalPos.Web",
                    ["Jwt:SigningKey"] = SigningKey,
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["CpeApi:BaseUrl"] = "http://localhost/capitalpos-cpe-auth-tests/",
                    ["CpeApi:ApiKey"] = ApiKeyFicticia,
                    ["DemoSeed:Enabled"] = "false"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUsuarioRepository>();
                services.RemoveAll<IUsuarioCredencialRepository>();
                services.RemoveAll<IPasswordHasher>();
                services.RemoveAll<ICpeGateway>();

                var usuario = _usuarioExiste
                    ? new Usuario(UsuarioId, "Usuario", "Auth", "usuario.auth@capitalpos.test")
                    : null;
                var credencial = usuario is null
                    ? null
                    : new UsuarioCredencial(
                        usuario.Id,
                        "hash-de-prueba",
                        "TestHasher",
                        activo: _credencialActiva,
                        bloqueado: _credencialBloqueada);

                services.AddSingleton<IUsuarioRepository>(new UsuarioRepositoryFake(usuario));
                services.AddSingleton<IUsuarioCredencialRepository>(new UsuarioCredencialRepositoryFake(credencial));
                services.AddSingleton<IPasswordHasher, PasswordHasherFake>();
                services.AddSingleton<ICpeGateway, CpeGatewayFake>();
            });
        }
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        private readonly Usuario? _usuario;

        public UsuarioRepositoryFake(Usuario? usuario)
        {
            _usuario = usuario;
        }

        public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Usuario> usuarios = _usuario is null ? [] : [_usuario];

            return Task.FromResult(usuarios);
        }

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuario?.Id == id ? _usuario : null);
        }

        public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuario?.Correo == correo.Trim().ToLowerInvariant() ? _usuario : null);
        }

        public Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuario?.Correo == correo.Trim().ToLowerInvariant());
        }
    }

    private sealed class UsuarioCredencialRepositoryFake : IUsuarioCredencialRepository
    {
        private readonly UsuarioCredencial? _credencial;

        public UsuarioCredencialRepositoryFake(UsuarioCredencial? credencial)
        {
            _credencial = credencial;
        }

        public Task<UsuarioCredencial?> ObtenerPorUsuarioIdAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_credencial?.UsuarioId == usuarioId ? _credencial : null);
        }
    }

    private sealed class PasswordHasherFake : IPasswordHasher
    {
        public string GenerarHash(UsuarioCredencial credencial, string password)
        {
            return "hash-de-prueba";
        }

        public PasswordVerificationResult Verificar(UsuarioCredencial credencial, string password)
        {
            return new PasswordVerificationResult(password == PasswordValido, RequiereRehash: false);
        }
    }

    private sealed class CpeGatewayFake : ICpeGateway
    {
        public Task<CpeGatewayResponse> ObtenerEstadoAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CpeGatewayResponse(
                StatusCodes.Status200OK,
                true,
                """{"ok":true,"data":{"status":"OK"}}""",
                "application/json"));
        }

        public Task<CpeGatewayResponse> EmitirAsync(
            JsonElement request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CpeGatewayResponse(
                StatusCodes.Status200OK,
                true,
                """{"ok":true,"data":{"ok":true,"estado":"ACEPTADO"}}""",
                "application/json"));
        }
    }

    private static string EncontrarRaizRepo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CapitalPos.Api.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo encontrar la raiz del repositorio.");
    }
}
