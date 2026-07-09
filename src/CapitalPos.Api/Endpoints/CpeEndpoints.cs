using System.Text.Json;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Seguridad;

namespace CapitalPos.Api.Endpoints;

public static class CpeEndpoints
{
    public static IEndpointRouteBuilder MapCpeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cpe")
            .WithTags("CPE")
            .RequireAuthorization();

        group.MapGet("/estado", ObtenerEstadoAsync)
            .WithName("ObtenerEstadoCpe");

        group.MapPost("/emitir", EmitirAsync)
            .AddEndpointFilter<EmpresaActivaEndpointFilter>()
            .WithName("EmitirCpe")
            .RequirePermisoEmpresa(PermisoEmpresa.EmitirCpe);

        return app;
    }

    private static async Task<IResult> ObtenerEstadoAsync(
        ICpeGateway gateway,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await gateway.ObtenerEstadoAsync(cancellationToken);
            var normalizedResponse = CpeEstadoResponseNormalizer.Normalizar(response);

            return Results.Json(
                normalizedResponse.Body,
                statusCode: normalizedResponse.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            var normalizedResponse = CpeEstadoResponseNormalizer.CrearNoDisponible(ex);

            return Results.Json(
                normalizedResponse.Body,
                statusCode: normalizedResponse.StatusCode);
        }
        catch (TaskCanceledException ex)
        {
            var normalizedResponse = CpeEstadoResponseNormalizer.CrearNoDisponible(ex);

            return Results.Json(
                normalizedResponse.Body,
                statusCode: normalizedResponse.StatusCode);
        }
    }

    private static async Task<IResult> EmitirAsync(
        JsonElement request,
        ICpeGateway gateway,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await gateway.EmitirAsync(request, cancellationToken);
            var normalizedResponse = EmitirCpeResponseNormalizer.Normalizar(response);
            var resultado = normalizedResponse.Body.Ok
                ? AuditoriaResultados.Exitoso
                : normalizedResponse.StatusCode >= StatusCodes.Status500InternalServerError
                    ? AuditoriaResultados.Error
                    : AuditoriaResultados.Rechazado;

            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "EmitirCpe",
                "CPE",
                "Emitir",
                resultado,
                $"Estado={normalizedResponse.Body.Estado};Codigo={normalizedResponse.Body.Codigo}",
                cancellationToken);

            return Results.Json(
                normalizedResponse.Body,
                statusCode: normalizedResponse.StatusCode);
        }
        catch
        {
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "EmitirCpe",
                "CPE",
                "Emitir",
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }
}
