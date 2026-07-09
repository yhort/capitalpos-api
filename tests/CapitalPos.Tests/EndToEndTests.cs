using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CapitalPos.Tests;

[Collection("CapitalPosEndToEnd")]
public class EndToEndTests
{
    private const string ConnectionStringEnvironmentVariable = "CAPITALPOS_TEST_CONNECTION_STRING";
    private const string SigningKey = "capitalpos-e2e-tests-signing-key-without-real-secret-2026";
    private const string Issuer = "CapitalPos.Api";
    private const string Audience = "CapitalPos.Web";
    private const string ApiKeyFicticia = "capitalpos-e2e-cpe-api-key-placeholder";
    private const string PasswordValido = "CapitalPos-E2E-Password-2026";
    private const string PasswordIncorrecto = "CapitalPos-E2E-Password-Incorrecto";
    private static readonly SemaphoreSlim DatabaseLock = new(1, 1);

    [Fact]
    public async Task Flujo_e2e_emite_cpe_con_usuario_empresa_activa_permiso_y_respuesta_normalizada()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.Exitoso);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador);

        try
        {
            var token = await ObtenerAccessTokenAsync(factory, escenario, PasswordValido);
            using var client = CrearClienteAutenticado(factory, token.Token, escenario.EmpresaId);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/cpe/emitir")
            {
                Content = JsonContent.Create(new
                {
                    tipoDocumento = "01",
                    serie = "F001",
                    numero = "1"
                })
            };
            request.Headers.Add("X-Correlation-Id", escenario.CorrelationId);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadFromJsonAsync<EmitirCpeResponse>();
            var logs = string.Join('\n', factory.Logs.Messages);
            var responseContent = JsonSerializer.Serialize(body);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
            Assert.True(body.Ok);
            Assert.Equal("ACEPTADO", body.Estado);
            Assert.Equal("F001-1", body.Comprobante?.Numero);
            Assert.Equal("hash-e2e", body.Hash);
            Assert.Contains("xml-e2e.xml", body.NombreXml);
            Assert.Contains("cdr-e2e.zip", body.NombreCdr);
            Assert.Contains("EmitirCpe", logs);
            Assert.Contains(escenario.UsuarioId.ToString(), logs);
            Assert.Contains(escenario.EmpresaId.ToString(), logs);
            Assert.Contains(escenario.CorrelationId, logs);
            AssertSeguro(responseContent, logs, token.Token);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Login_e2e_con_password_incorrecto_rechaza_credenciales()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.Exitoso);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador);

        try
        {
            using var scope = factory.Services.CreateScope();
            var login = scope.ServiceProvider.GetRequiredService<LoginUseCase>();

            var result = await login.EjecutarAsync(
                new LoginRequest(escenario.Correo, PasswordIncorrecto));

            Assert.Equal(LoginStatus.PasswordIncorrecto, result.Status);
            Assert.Null(result.UsuarioId);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.Exitoso);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador);

        try
        {
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add(EmpresaActivaHeaders.HeaderName, escenario.EmpresaId.ToString());

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_con_jwt_invalido_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.Exitoso);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador);

        try
        {
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");
            client.DefaultRequestHeaders.Add(EmpresaActivaHeaders.HeaderName, escenario.EmpresaId.ToString());

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.Exitoso);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador);

        try
        {
            var token = await ObtenerAccessTokenAsync(factory, escenario, PasswordValido);
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
            AssertSeguro(content, string.Empty, token.Token);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_usuario_no_asociado_devuelve_forbidden()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.Exitoso);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador, crearRelacion: false);

        try
        {
            var token = await ObtenerAccessTokenAsync(factory, escenario, PasswordValido);
            using var client = CrearClienteAutenticado(factory, token.Token, escenario.EmpresaId);

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Contains("no pertenece", content);
            AssertSeguro(content, string.Empty, token.Token);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_relacion_inactiva_devuelve_forbidden()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.Exitoso);
        var escenario = await factory.PrepararEscenarioAsync(
            RolEmpresa.Administrador,
            relacionActiva: false);

        try
        {
            var token = await ObtenerAccessTokenAsync(factory, escenario, PasswordValido);
            using var client = CrearClienteAutenticado(factory, token.Token, escenario.EmpresaId);

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Contains("relacion esta inactiva", content);
            AssertSeguro(content, string.Empty, token.Token);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_rol_sin_permiso_emitir_cpe_devuelve_forbidden()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.Exitoso);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Almacenero);

        try
        {
            var token = await ObtenerAccessTokenAsync(factory, escenario, PasswordValido);
            using var client = CrearClienteAutenticado(factory, token.Token, escenario.EmpresaId);

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Contains("permiso requerido", content);
            AssertSeguro(content, string.Empty, token.Token);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_stub_con_rechazo_funcional_devuelve_respuesta_normalizada()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.RechazoFuncional);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador);

        try
        {
            var token = await ObtenerAccessTokenAsync(factory, escenario, PasswordValido);
            using var client = CrearClienteAutenticado(factory, token.Token, escenario.EmpresaId);

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });
            var body = await response.Content.ReadFromJsonAsync<EmitirCpeResponse>();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(body);
            Assert.False(body.Ok);
            Assert.Equal("RECHAZADO", body.Estado);
            Assert.Contains(body.Errores, error => error.Codigo == "SUNAT_2335");
            AssertSeguro(JsonSerializer.Serialize(body), string.Join('\n', factory.Logs.Messages), token.Token);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_stub_con_error_http_devuelve_respuesta_normalizada()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.ErrorHttp);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador);

        try
        {
            var token = await ObtenerAccessTokenAsync(factory, escenario, PasswordValido);
            using var client = CrearClienteAutenticado(factory, token.Token, escenario.EmpresaId);

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });
            var body = await response.Content.ReadFromJsonAsync<EmitirCpeResponse>();

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.NotNull(body);
            Assert.False(body.Ok);
            Assert.Equal("ERROR_CPE", body.Estado);
            Assert.Contains(body.Errores, error => error.Mensaje.Contains("Servicio CPE no disponible", StringComparison.OrdinalIgnoreCase));
            AssertSeguro(JsonSerializer.Serialize(body), string.Join('\n', factory.Logs.Messages), token.Token);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    [Fact]
    public async Task Cpe_e2e_stub_con_respuesta_invalida_devuelve_error_seguro()
    {
        await using var factory = new CapitalPosE2EFactory(CpeStubMode.RespuestaInvalida);
        var escenario = await factory.PrepararEscenarioAsync(RolEmpresa.Administrador);

        try
        {
            var token = await ObtenerAccessTokenAsync(factory, escenario, PasswordValido);
            using var client = CrearClienteAutenticado(factory, token.Token, escenario.EmpresaId);

            var response = await client.PostAsJsonAsync("/api/cpe/emitir", new { serie = "F001" });
            var content = await response.Content.ReadAsStringAsync();
            var body = JsonSerializer.Deserialize<EmitirCpeResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.NotNull(body);
            Assert.False(body.Ok);
            Assert.Equal("RESPUESTA_CPE_INVALIDA", body.Estado);
            Assert.DoesNotContain("json-invalido", content);
            AssertSeguro(content, string.Join('\n', factory.Logs.Messages), token.Token);
        }
        finally
        {
            await factory.LimpiarEscenarioAsync(escenario);
        }
    }

    private static async Task<AccessTokenResult> ObtenerAccessTokenAsync(
        CapitalPosE2EFactory factory,
        E2EScenario escenario,
        string password)
    {
        using var scope = factory.Services.CreateScope();
        var login = scope.ServiceProvider.GetRequiredService<LoginUseCase>();
        var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

        var loginResult = await login.EjecutarAsync(new LoginRequest(escenario.Correo, password));

        Assert.Equal(LoginStatus.CredencialesValidas, loginResult.Status);
        Assert.Equal(escenario.UsuarioId, loginResult.UsuarioId);

        var token = issuer.Emitir(new AccessTokenRequest(
            loginResult.UsuarioId!.Value,
            loginResult.Correo!,
            "Usuario E2E"));

        await Task.Delay(TimeSpan.FromSeconds(1));

        return token;
    }

    private static HttpClient CrearClienteAutenticado(
        CapitalPosE2EFactory factory,
        string token,
        Guid empresaId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add(EmpresaActivaHeaders.HeaderName, empresaId.ToString());

        return client;
    }

    private static string ObtenerConnectionStringValidada()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Defina {ConnectionStringEnvironmentVariable} para ejecutar pruebas end-to-end locales contra PostgreSQL de pruebas.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Database) ||
            !builder.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} debe apuntar a una base PostgreSQL exclusiva de pruebas cuyo nombre contenga 'test'.");
        }

        return connectionString;
    }

    private static void AssertSeguro(string responseContent, string logs, string? token = null)
    {
        Assert.DoesNotContain(SigningKey, responseContent, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKeyFicticia, responseContent, StringComparison.Ordinal);
        Assert.DoesNotContain(PasswordValido, responseContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Host=", responseContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", responseContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", responseContent, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(SigningKey, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKeyFicticia, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(PasswordValido, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", logs, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(token))
        {
            Assert.DoesNotContain(token, responseContent, StringComparison.Ordinal);
            Assert.DoesNotContain(token, logs, StringComparison.Ordinal);
        }
    }

    private sealed class CapitalPosE2EFactory : WebApplicationFactory<Program>
    {
        private readonly CpeStubMode _cpeStubMode;
        private readonly string _connectionString = ObtenerConnectionStringValidada();

        public CapitalPosE2EFactory(CpeStubMode cpeStubMode)
        {
            _cpeStubMode = cpeStubMode;
            Logs = new TestLoggerProvider();
            Environment.SetEnvironmentVariable("ConnectionStrings__CapitalPos", _connectionString);
            Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
            Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
            Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
            Environment.SetEnvironmentVariable("CpeApi__BaseUrl", "http://localhost/capitalpos-cpe-e2e/");
            Environment.SetEnvironmentVariable("CpeApi__ApiKey", ApiKeyFicticia);
        }

        public TestLoggerProvider Logs { get; }

        public async Task<E2EScenario> PrepararEscenarioAsync(
            RolEmpresa rol,
            bool crearRelacion = true,
            bool relacionActiva = true)
        {
            var escenario = E2EScenario.Create();

            await DatabaseLock.WaitAsync();
            try
            {
                using var scope = Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<CapitalPosDbContext>();
                await dbContext.Database.MigrateAsync();
                await LimpiarEscenarioAsync(dbContext, escenario);

                var usuario = new Usuario(
                    escenario.UsuarioId,
                    "Usuario",
                    "E2E",
                    escenario.Correo);
                var empresa = new Empresa(
                    escenario.EmpresaId,
                    escenario.Ruc,
                    "CapitalPOS E2E SAC",
                    "CapitalPOS E2E");
                var credencial = new UsuarioCredencial(
                    escenario.UsuarioId,
                    "hash-temporal",
                    "ASP.NET Core Identity PasswordHasher");
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                credencial.CambiarPasswordHash(
                    hasher.GenerarHash(credencial, PasswordValido),
                    "ASP.NET Core Identity PasswordHasher");

                await dbContext.Empresas.AddAsync(empresa);
                await dbContext.Usuarios.AddAsync(usuario);
                await dbContext.UsuariosCredenciales.AddAsync(credencial);

                if (crearRelacion)
                {
                    var usuarioEmpresa = new UsuarioEmpresa(
                        escenario.UsuarioEmpresaId,
                        escenario.UsuarioId,
                        escenario.EmpresaId,
                        rol,
                        relacionActiva);
                    await dbContext.UsuariosEmpresa.AddAsync(usuarioEmpresa);
                }

                await dbContext.SaveChangesAsync();
            }
            finally
            {
                DatabaseLock.Release();
            }

            return escenario;
        }

        public async Task LimpiarEscenarioAsync(E2EScenario escenario)
        {
            await DatabaseLock.WaitAsync();
            try
            {
                using var scope = Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<CapitalPosDbContext>();
                await LimpiarEscenarioAsync(dbContext, escenario);
            }
            finally
            {
                DatabaseLock.Release();
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CapitalPos"] = _connectionString,
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:SigningKey"] = SigningKey,
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["CpeApi:BaseUrl"] = "http://localhost/capitalpos-cpe-e2e/",
                    ["CpeApi:ApiKey"] = ApiKeyFicticia,
                    ["DemoSeed:Enabled"] = "false"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICpeGateway>();
                services.AddSingleton<ICpeGateway>(new StubCpeGateway(_cpeStubMode));
                services.AddSingleton<ILoggerProvider>(Logs);
            });
        }

        private static async Task LimpiarEscenarioAsync(
            CapitalPosDbContext dbContext,
            E2EScenario escenario)
        {
            await dbContext.UsuariosEmpresa
                .Where(usuarioEmpresa =>
                    usuarioEmpresa.Id == escenario.UsuarioEmpresaId ||
                    usuarioEmpresa.UsuarioId == escenario.UsuarioId ||
                    usuarioEmpresa.EmpresaId == escenario.EmpresaId)
                .ExecuteDeleteAsync();
            await dbContext.UsuariosCredenciales
                .Where(credencial => credencial.UsuarioId == escenario.UsuarioId)
                .ExecuteDeleteAsync();
            await dbContext.Usuarios
                .Where(usuario => usuario.Id == escenario.UsuarioId)
                .ExecuteDeleteAsync();
            await dbContext.Empresas
                .Where(empresa => empresa.Id == escenario.EmpresaId || empresa.Ruc == escenario.Ruc)
                .ExecuteDeleteAsync();
        }
    }

    private sealed class StubCpeGateway : ICpeGateway
    {
        private readonly CpeStubMode _mode;

        public StubCpeGateway(CpeStubMode mode)
        {
            _mode = mode;
        }

        public Task<CpeGatewayResponse> ObtenerEstadoAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CpeGatewayResponse(
                StatusCodes.Status200OK,
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
                "application/json"));
        }

        public Task<CpeGatewayResponse> EmitirAsync(
            JsonElement request,
            CancellationToken cancellationToken = default)
        {
            var response = _mode switch
            {
                CpeStubMode.Exitoso => new CpeGatewayResponse(
                    StatusCodes.Status200OK,
                    true,
                    """
                    {
                      "ok": true,
                      "data": {
                        "ok": true,
                        "estado": "ACEPTADO",
                        "mensaje": "Comprobante aceptado",
                        "comprobante": "F001-1",
                        "hash": "hash-e2e",
                        "nombreXml": "xml-e2e.xml",
                        "nombreZip": "zip-e2e.zip",
                        "nombreCdr": "cdr-e2e.zip"
                      }
                    }
                    """,
                    "application/json"),
                CpeStubMode.RechazoFuncional => new CpeGatewayResponse(
                    StatusCodes.Status400BadRequest,
                    false,
                    """
                    {
                      "ok": false,
                      "mensaje": "SUNAT rechazo el comprobante",
                      "data": {
                        "ok": false,
                        "estado": "RECHAZADO",
                        "mensaje": "SUNAT rechazo el comprobante",
                        "errores": [
                          {
                            "codigo": "SUNAT_2335",
                            "campo": "numeroDocumento",
                            "mensaje": "El numero de documento no es valido"
                          }
                        ]
                      }
                    }
                    """,
                    "application/json"),
                CpeStubMode.ErrorHttp => new CpeGatewayResponse(
                    StatusCodes.Status502BadGateway,
                    false,
                    """
                    {
                      "ok": false,
                      "mensaje": "Servicio CPE no disponible"
                    }
                    """,
                    "application/json"),
                CpeStubMode.RespuestaInvalida => new CpeGatewayResponse(
                    StatusCodes.Status200OK,
                    true,
                    "{ json-invalido-con-detalle-interno }",
                    "application/json"),
                _ => throw new InvalidOperationException("Modo CPE no soportado.")
            };

            return Task.FromResult(response);
        }
    }

    private sealed class TestLoggerProvider : ILoggerProvider
    {
        private readonly object _sync = new();
        private readonly List<string> _messages = [];

        public IReadOnlyCollection<string> Messages
        {
            get
            {
                lock (_sync)
                {
                    return _messages.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(this, categoryName);
        }

        public void Dispose()
        {
        }

        private void Add(string categoryName, LogLevel logLevel, string message)
        {
            if (!categoryName.StartsWith("CapitalPos.", StringComparison.Ordinal))
            {
                return;
            }

            lock (_sync)
            {
                _messages.Add($"{logLevel}: {categoryName}: {message}");
            }
        }

        private sealed class TestLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly TestLoggerProvider _provider;

            public TestLogger(TestLoggerProvider provider, string categoryName)
            {
                _provider = provider;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _provider.Add(_categoryName, logLevel, formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed record E2EScenario(
        Guid UsuarioId,
        Guid EmpresaId,
        Guid UsuarioEmpresaId,
        string Correo,
        string Ruc,
        string CorrelationId)
    {
        public static E2EScenario Create()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var rucNumber = Math.Abs(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));

            return new E2EScenario(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                $"usuario-e2e-{suffix}@capitalpos.test",
                $"20{rucNumber:D9}"[..11],
                $"capitalpos-e2e-{suffix}");
        }
    }

    private enum CpeStubMode
    {
        Exitoso,
        RechazoFuncional,
        ErrorHttp,
        RespuestaInvalida
    }
}

[CollectionDefinition("CapitalPosEndToEnd", DisableParallelization = true)]
public sealed class CapitalPosEndToEndCollection;
