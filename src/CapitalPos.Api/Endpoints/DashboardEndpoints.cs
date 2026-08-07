using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Dashboard;
using CapitalPos.Application.Seguridad;

namespace CapitalPos.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/comercial", DashboardComercialAsync)
            .WithName("DashboardComercial")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapGet("/reporte-canales", DashboardReporteCanalesAsync)
            .WithName("DashboardReporteCanales")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        return app;
    }

    private static async Task<IResult> DashboardComercialAsync(
        DashboardComercialUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> DashboardReporteCanalesAsync(
        DashboardReporteCanalesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(response);
    }
}
