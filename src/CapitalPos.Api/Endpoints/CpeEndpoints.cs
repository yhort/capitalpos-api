using System.Text.Json;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Seguridad;

namespace CapitalPos.Api.Endpoints;

public static class CpeEndpoints
{
    public static IEndpointRouteBuilder MapCpeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cpe")
            .WithTags("CPE")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapPost("/emitir", EmitirAsync)
            .WithName("EmitirCpe")
            .RequirePermisoEmpresa(PermisoEmpresa.EmitirCpe);

        return app;
    }

    private static async Task<IResult> EmitirAsync(
        JsonElement request,
        ICpeGateway gateway,
        CancellationToken cancellationToken)
    {
        var response = await gateway.EmitirAsync(request, cancellationToken);

        return Results.Content(
            response.Content,
            response.ContentType,
            statusCode: response.StatusCode);
    }
}
