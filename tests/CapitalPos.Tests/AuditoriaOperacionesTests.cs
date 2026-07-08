using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Endpoints;
using CapitalPos.Api.Middleware;
using CapitalPos.Application.Auditoria;
using CapitalPos.Infrastructure.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CapitalPos.Tests;

public class AuditoriaOperacionesTests
{
    [Fact]
    public async Task Logger_registra_evento_exitoso_con_propiedades_estructuradas()
    {
        var logger = new CapturingLogger<LoggerAuditoriaOperaciones>();
        var auditoria = new LoggerAuditoriaOperaciones(logger);
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var fechaUtc = DateTimeOffset.UtcNow;

        await auditoria.RegistrarAsync(new AuditoriaOperacion(
            "CrearEmpresa",
            usuarioId,
            empresaId,
            "Empresa",
            "Crear",
            AuditoriaResultados.Exitoso,
            fechaUtc,
            "correlation-test",
            "EmpresaId=empresa-test"));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("CrearEmpresa", entry.Properties["Operacion"]);
        Assert.Equal(usuarioId, entry.Properties["UsuarioId"]);
        Assert.Equal(empresaId, entry.Properties["EmpresaId"]);
        Assert.Equal("Empresa", entry.Properties["Recurso"]);
        Assert.Equal("Crear", entry.Properties["Accion"]);
        Assert.Equal(AuditoriaResultados.Exitoso, entry.Properties["Resultado"]);
        Assert.Equal("correlation-test", entry.Properties["CorrelationId"]);
        Assert.Equal(fechaUtc.ToUniversalTime(), entry.Properties["FechaUtc"]);
    }

    [Fact]
    public async Task Logger_registra_evento_rechazado_con_usuario_empresa_y_correlation_id()
    {
        var logger = new CapturingLogger<LoggerAuditoriaOperaciones>();
        var auditoria = new LoggerAuditoriaOperaciones(logger);
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();

        await auditoria.RegistrarAsync(new AuditoriaOperacion(
            "AsignarUsuarioEmpresa",
            usuarioId,
            empresaId,
            "UsuarioEmpresa",
            "Asignar",
            AuditoriaResultados.Rechazado,
            DateTimeOffset.UtcNow,
            "correlation-rechazado",
            "ValidacionDeDominio"));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(AuditoriaResultados.Rechazado, entry.Properties["Resultado"]);
        Assert.Equal(usuarioId, entry.Properties["UsuarioId"]);
        Assert.Equal(empresaId, entry.Properties["EmpresaId"]);
        Assert.Equal("correlation-rechazado", entry.Properties["CorrelationId"]);
    }

    [Fact]
    public async Task Logger_no_registra_secretos_en_evento_seguro()
    {
        var logger = new CapturingLogger<LoggerAuditoriaOperaciones>();
        var auditoria = new LoggerAuditoriaOperaciones(logger);

        await auditoria.RegistrarAsync(new AuditoriaOperacion(
            "EmitirCpe",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CPE",
            "Emitir",
            AuditoriaResultados.Error,
            DateTimeOffset.UtcNow,
            "correlation-error",
            "Estado=ERROR_VALIDACION;Codigo=ERROR_VALIDACION"));

        var entry = Assert.Single(logger.Entries);
        var loggedText = string.Join(
            " ",
            entry.Message,
            string.Join(" ", entry.Properties.Values.Select(value => value?.ToString())));

        Assert.DoesNotContain("password", loggedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", loggedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer", loggedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-API-KEY", loggedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection string", loggedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN CERTIFICATE", loggedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", loggedText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Falla_de_auditoria_no_rompe_operacion_principal()
    {
        var empresaActiva = new EmpresaActivaContext();
        empresaActiva.Establecer(Guid.NewGuid(), Guid.NewGuid(), Domain.RolEmpresa.Administrador);
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-auditoria";

        var exception = await Record.ExceptionAsync(() =>
            AuditoriaEndpointHelper.AuditarAsync(
                new ThrowingAuditoriaOperaciones(),
                empresaActiva,
                httpContext,
                "CrearUsuario",
                "Usuario",
                "Crear",
                AuditoriaResultados.Exitoso,
                "UsuarioId=usuario-test",
                CancellationToken.None));

        Assert.Null(exception);
    }

    private sealed class ThrowingAuditoriaOperaciones : IAuditoriaOperaciones
    {
        public Task RegistrarAsync(
            AuditoriaOperacion operacion,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Fallo de auditoria simulado.");
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return new EmptyDisposable();
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
            var properties = state is IEnumerable<KeyValuePair<string, object>> values
                ? values
                    .Where(property => property.Key != "{OriginalFormat}")
                    .ToDictionary(property => property.Key, property => property.Value)
                : [];

            Entries.Add(new LogEntry(logLevel, formatter(state, exception), properties));
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object> Properties);
}
