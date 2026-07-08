using CapitalPos.Api.Endpoints;

namespace CapitalPos.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private const string PublicMessage = "Ocurrio un error inesperado al procesar la solicitud.";

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error no controlado al procesar la solicitud {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(
                ErrorResponse.From(PublicMessage),
                context.RequestAborted);
        }
    }
}
