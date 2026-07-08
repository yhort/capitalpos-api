using CapitalPos.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CapitalPos.Tests;

public class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task Middleware_agrega_correlation_id_y_registra_log_estructurado()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var context = CrearHttpContext();
        context.Request.Headers[RequestLoggingMiddleware.CorrelationIdHeaderName] = "capitalpos-correlation-test";
        var middleware = new RequestLoggingMiddleware(
            httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        Assert.Equal(
            "capitalpos-correlation-test",
            context.Response.Headers[RequestLoggingMiddleware.CorrelationIdHeaderName]);
        var scope = Assert.Single(logger.Scopes);
        Assert.Equal("capitalpos-correlation-test", scope["CorrelationId"]);
        Assert.Equal("POST", scope["RequestMethod"]);
        Assert.Equal("/api/cpe/emitir", scope["RequestPath"]);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("Solicitud HTTP completada", entry.Message);
        Assert.Contains(entry.Properties, property =>
            property.Key == "StatusCode" && Equals(property.Value, StatusCodes.Status202Accepted));
        Assert.Contains(entry.Properties, property =>
            property.Key == "Method" && Equals(property.Value, "POST"));
        Assert.Contains(entry.Properties, property =>
            property.Key == "Path" && Equals(property.Value, "/api/cpe/emitir"));
        Assert.Contains(entry.Properties, property =>
            property.Key == "ElapsedMilliseconds");
    }

    [Fact]
    public async Task Middleware_usa_trace_identifier_si_no_llega_correlation_id()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var context = CrearHttpContext();
        context.TraceIdentifier = "trace-test";
        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            logger);

        await middleware.InvokeAsync(context);

        Assert.Equal("trace-test", context.Response.Headers[RequestLoggingMiddleware.CorrelationIdHeaderName]);
        var scope = Assert.Single(logger.Scopes);
        Assert.Equal("trace-test", scope["CorrelationId"]);
    }

    [Fact]
    public async Task Middleware_no_registra_query_string_ni_headers_sensibles()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var context = CrearHttpContext();
        context.Request.QueryString = new QueryString("?token=valor-sensible");
        context.Request.Headers["Authorization"] = "Bearer valor-sensible";
        context.Request.Headers["X-API-KEY"] = "valor-sensible";
        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            logger);

        await middleware.InvokeAsync(context);

        var loggedText = string.Join(
            " ",
            logger.Entries.Select(entry => entry.Message)
                .Concat(logger.Scopes.SelectMany(scope => scope.Values.Select(value => value.ToString()))));
        Assert.DoesNotContain("valor-sensible", loggedText);
        Assert.DoesNotContain("Authorization", loggedText);
        Assert.DoesNotContain("X-API-KEY", loggedText);
        Assert.DoesNotContain("?token=", loggedText);
    }

    [Fact]
    public void Program_registra_request_logging_antes_del_manejo_global_de_excepciones()
    {
        var source = File.ReadAllText(ResolverRutaRepo("src/CapitalPos.Api/Program.cs"));
        var requestLoggingIndex = source.IndexOf(
            "app.UseMiddleware<RequestLoggingMiddleware>();",
            StringComparison.Ordinal);
        var exceptionHandlingIndex = source.IndexOf(
            "app.UseMiddleware<GlobalExceptionHandlingMiddleware>();",
            StringComparison.Ordinal);

        Assert.True(requestLoggingIndex >= 0);
        Assert.True(exceptionHandlingIndex > requestLoggingIndex);
    }

    private static DefaultHttpContext CrearHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/cpe/emitir";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static string ResolverRutaRepo(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "CapitalPos.Api.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(directory.FullName, relativePath);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo resolver la raiz del repositorio.");
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public List<IReadOnlyDictionary<string, object>> Scopes { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object>> properties)
            {
                Scopes.Add(properties.ToDictionary(
                    property => property.Key,
                    property => property.Value));
            }

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
                ? values.ToDictionary(property => property.Key, property => property.Value)
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
