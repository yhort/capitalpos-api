using System.Globalization;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Reportes;
using CapitalPos.Application.Seguridad;

namespace CapitalPos.Api.Endpoints;

public static class ReporteEndpoints
{
    private const string FormatoFecha = "yyyy-MM-dd";

    public static IEndpointRouteBuilder MapReporteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reportes")
            .WithTags("Reportes")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/ventas-por-canal", VentasPorCanalAsync)
            .WithName("ReporteVentasPorCanal")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        return app;
    }

    private static async Task<IResult> VentasPorCanalAsync(
        string? desde,
        string? hasta,
        ReporteVentasPorCanalUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!TryParseFechaRequerida(desde, "desde", out var desdeNormalizado, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        if (!TryParseFechaRequerida(hasta, "hasta", out var hastaNormalizado, out error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        try
        {
            var response = await useCase.EjecutarAsync(
                desdeNormalizado,
                hastaNormalizado,
                cancellationToken);

            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static bool TryParseFechaRequerida(
        string? valor,
        string nombre,
        out DateOnly fecha,
        out string error)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            fecha = default;
            error = $"La fecha {nombre} es obligatoria.";
            return false;
        }

        if (!DateOnly.TryParseExact(
            valor,
            FormatoFecha,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out fecha))
        {
            error = $"La fecha {nombre} debe tener formato {FormatoFecha}.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
