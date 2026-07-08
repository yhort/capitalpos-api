using CapitalPos.Api.Middleware;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Seguridad;

namespace CapitalPos.Api.Endpoints;

public static class AuditoriaEndpointHelper
{
    public static async Task AuditarAsync(
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        string operacion,
        string recurso,
        string accion,
        string resultado,
        string? detalleSeguro,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditoria.RegistrarAsync(
                new AuditoriaOperacion(
                    operacion,
                    empresaActiva.UsuarioId,
                    empresaActiva.EmpresaId,
                    recurso,
                    accion,
                    resultado,
                    DateTimeOffset.UtcNow,
                    ObtenerCorrelationId(httpContext),
                    detalleSeguro),
                cancellationToken);
        }
        catch
        {
            // La auditoria no debe cambiar el resultado de la operacion principal.
        }
    }

    private static string ObtenerCorrelationId(HttpContext httpContext)
    {
        var correlationId = httpContext.Response.Headers[RequestLoggingMiddleware.CorrelationIdHeaderName]
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = httpContext.Request.Headers[RequestLoggingMiddleware.CorrelationIdHeaderName]
                .FirstOrDefault();
        }

        return string.IsNullOrWhiteSpace(correlationId)
            ? httpContext.TraceIdentifier
            : correlationId.Trim();
    }
}
