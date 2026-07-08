using System.Diagnostics;

namespace CapitalPos.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ObtenerCorrelationId(context);
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.Value ?? string.Empty
        });

        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        _logger.LogInformation(
            "Solicitud HTTP completada {Method} {Path} con estado {StatusCode} en {ElapsedMilliseconds} ms.",
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }

    private static string ObtenerCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();

        return string.IsNullOrWhiteSpace(correlationId)
            ? context.TraceIdentifier
            : correlationId.Trim();
    }
}
