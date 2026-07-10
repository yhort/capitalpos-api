using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CapitalPos.Tests;

public class HttpIntegrationTests
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EmpresaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string SigningKey = "capitalpos-http-integration-tests-signing-key-2026";
    private const string Issuer = "CapitalPos.Api";
    private const string Audience = "CapitalPos.Web";
    private const string ApiKeyFicticia = "capitalpos-cpe-http-tests-api-key";

    [Fact]
    public async Task Health_responde_sin_autenticacion()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ok\"", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task OpenApi_permanece_accesible_sin_autenticacion()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/health", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Endpoint_empresarial_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/empresas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_empresarial_con_jwt_sin_header_empresa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Header_empresa_con_formato_invalido_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);
        client.DefaultRequestHeaders.Add(EmpresaActivaHeaders.HeaderName, "empresa-invalida");

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identificador de empresa valido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Usuario_no_asociado_a_empresa_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = null
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("no pertenece", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Usuario_asociado_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/usuarios");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Usuario_autenticado_con_empresa_y_permiso_puede_acceder()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Administrador)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("20601234567", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Post_con_entrada_invalida_devuelve_error_de_validacion()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Administrador)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearEmpresaRequest("123", "CapitalPOS SAC");

        var response = await client.PostAsJsonAsync("/api/empresas", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("El RUC debe tener 11 digitos.", content);
        Assert.Empty(factory.EmpresaRepository.EmpresasAgregadas);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Excepcion_no_controlada_devuelve_error_seguro()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Administrador),
            LanzarExcepcionAlListarEmpresas = true
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("error inesperado", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Correlation_id_se_propaga_en_la_respuesta()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        const string correlationId = "capitalpos-http-correlation-test";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Equal(correlationId, Assert.Single(values));
    }

    [Fact]
    public async Task X_forwarded_proto_https_permanece_compatible_con_health()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ok\"", content);
        AssertSeguro(content);
    }

    private static HttpClient CrearClienteAutenticado(
        CapitalPosHttpFactory factory,
        Guid usuarioId,
        Guid empresaId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(usuarioId);
        client.DefaultRequestHeaders.Add(EmpresaActivaHeaders.HeaderName, empresaId.ToString());

        return client;
    }

    private static AuthenticationHeaderValue CrearAuthorizationHeader(Guid usuarioId)
    {
        return new AuthenticationHeaderValue("Bearer", CrearJwt(usuarioId));
    }

    private static string CrearJwt(Guid usuarioId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "usuario.http@capitalpos.test"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UsuarioEmpresa CrearUsuarioEmpresa(RolEmpresa rol)
    {
        return new UsuarioEmpresa(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UsuarioId,
            EmpresaId,
            rol);
    }

    private static void AssertSeguro(string content)
    {
        Assert.DoesNotContain(SigningKey, content, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKeyFicticia, content, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=localhost", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", content, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapitalPosHttpFactory : WebApplicationFactory<Program>
    {
        public CapitalPosHttpFactory()
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__CapitalPos",
                "Host=localhost;Database=capitalpos_http_tests");
            Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
            Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
            Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
            Environment.SetEnvironmentVariable("CpeApi__BaseUrl", "http://localhost/capitalpos-cpe-tests/");
            Environment.SetEnvironmentVariable("CpeApi__ApiKey", ApiKeyFicticia);
            Environment.SetEnvironmentVariable("DemoSeed__Enabled", "false");
        }

        public FakeEmpresaRepository EmpresaRepository { get; } = new();

        public FakeUsuarioRepository UsuarioRepository { get; } = new();

        public FakeUsuarioEmpresaRepository UsuarioEmpresaRepository { get; } = new();

        public UsuarioEmpresa? UsuarioEmpresa
        {
            get => UsuarioEmpresaRepository.UsuarioEmpresa;
            set => UsuarioEmpresaRepository.UsuarioEmpresa = value;
        }

        public bool LanzarExcepcionAlListarEmpresas
        {
            get => EmpresaRepository.LanzarExcepcionAlListar;
            set => EmpresaRepository.LanzarExcepcionAlListar = value;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CapitalPos"] = "Host=localhost;Database=capitalpos_http_tests",
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:SigningKey"] = SigningKey,
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["CpeApi:BaseUrl"] = "http://localhost/capitalpos-cpe-tests/",
                    ["CpeApi:ApiKey"] = ApiKeyFicticia,
                    ["DemoSeed:Enabled"] = "false"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<CapitalPosDbContext>>();
                services.RemoveAll<CapitalPosDbContext>();
                services.RemoveAll<IEmpresaRepository>();
                services.RemoveAll<IUsuarioRepository>();
                services.RemoveAll<IUsuarioEmpresaRepository>();
                services.RemoveAll<IUsuarioCredencialRepository>();
                services.RemoveAll<IProductoRepository>();
                services.RemoveAll<IProductoVarianteRepository>();

                services.AddSingleton<IEmpresaRepository>(EmpresaRepository);
                services.AddSingleton<IUsuarioRepository>(UsuarioRepository);
                services.AddSingleton<IUsuarioEmpresaRepository>(UsuarioEmpresaRepository);
                services.AddSingleton<IUsuarioCredencialRepository, FakeUsuarioCredencialRepository>();
                services.AddSingleton<IProductoRepository, FakeProductoRepository>();
                services.AddSingleton<IProductoVarianteRepository, FakeProductoVarianteRepository>();
            });
        }
    }

    private sealed class FakeEmpresaRepository : IEmpresaRepository
    {
        private readonly List<Empresa> _empresas =
        [
            new Empresa(EmpresaId, "20601234567", "CapitalPOS SAC", "CapitalPOS")
        ];

        public List<Empresa> EmpresasAgregadas { get; } = [];

        public bool LanzarExcepcionAlListar { get; set; }

        public Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            EmpresasAgregadas.Add(empresa);
            _empresas.Add(empresa);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Empresa>> ListarAsync(CancellationToken cancellationToken = default)
        {
            if (LanzarExcepcionAlListar)
            {
                throw new InvalidOperationException("Fallo interno de prueba con secreto simulado");
            }

            return Task.FromResult<IReadOnlyCollection<Empresa>>(_empresas);
        }

        public Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_empresas.FirstOrDefault(empresa => empresa.Id == id));
        }

        public Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteRucAsync(string ruc, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_empresas.Any(empresa => empresa.Ruc == ruc));
        }
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios =
        [
            new Usuario(UsuarioId, "Usuario", "HTTP", "usuario.http@capitalpos.test")
        ];

        public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            _usuarios.Add(usuario);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Usuario>>(_usuarios);
        }

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.Id == id));
        }

        public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuarios.FirstOrDefault(
                usuario => usuario.Correo == correo.Trim().ToLowerInvariant()));
        }

        public Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuarios.Any(usuario => usuario.Correo == correo.Trim().ToLowerInvariant()));
        }
    }

    private sealed class FakeUsuarioEmpresaRepository : IUsuarioEmpresaRepository
    {
        public UsuarioEmpresa? UsuarioEmpresa { get; set; } = CrearUsuarioEmpresa(RolEmpresa.Administrador);

        public Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            UsuarioEmpresa = usuarioEmpresa;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<UsuarioEmpresa>> ListarAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<UsuarioEmpresa> result = UsuarioEmpresa is null
                ? []
                : [UsuarioEmpresa];

            return Task.FromResult(result);
        }

        public Task<UsuarioEmpresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UsuarioEmpresa?.Id == id ? UsuarioEmpresa : null);
        }

        public Task<UsuarioEmpresa?> ObtenerPorUsuarioYEmpresaAsync(
            Guid usuarioId,
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            var result = UsuarioEmpresa is not null &&
                UsuarioEmpresa.UsuarioId == usuarioId &&
                UsuarioEmpresa.EmpresaId == empresaId
                    ? UsuarioEmpresa
                    : null;

            return Task.FromResult(result);
        }

        public Task ActualizarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            UsuarioEmpresa = usuarioEmpresa;

            return Task.CompletedTask;
        }

        public Task<bool> ExisteAsignacionAsync(
            Guid usuarioId,
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UsuarioEmpresa is not null &&
                UsuarioEmpresa.UsuarioId == usuarioId &&
                UsuarioEmpresa.EmpresaId == empresaId);
        }
    }

    private sealed class FakeUsuarioCredencialRepository : IUsuarioCredencialRepository
    {
        public Task<UsuarioCredencial?> ObtenerPorUsuarioIdAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UsuarioCredencial?>(null);
        }
    }

    private sealed class FakeProductoRepository : IProductoRepository
    {
        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Producto>>([]);
        }

        public Task<Producto?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Producto?>(null);
        }

        public Task ActualizarAsync(
            Producto producto,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductoVarianteRepository : IProductoVarianteRepository
    {
        public Task AgregarAsync(
            ProductoVariante variante,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>([]);
        }
    }
}
