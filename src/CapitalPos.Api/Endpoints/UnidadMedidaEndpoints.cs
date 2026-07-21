using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class UnidadMedidaEndpoints
{
    public static IEndpointRouteBuilder MapUnidadMedidaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/unidades-medida")
            .WithTags("Unidades de medida")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/", ListarUnidadesMedidaAsync)
            .WithName("ListarUnidadesMedida")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        return app;
    }

    private static async Task<IResult> ListarUnidadesMedidaAsync(
        ListarUnidadesMedidaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var unidades = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(unidades.Select(UnidadMedidaResponse.From));
    }
}

public sealed record UnidadMedidaResponse(
    Guid Id,
    string Codigo,
    string Nombre,
    bool Activa,
    DateTimeOffset FechaCreacion)
{
    public static UnidadMedidaResponse From(UnidadMedida unidadMedida)
    {
        return new UnidadMedidaResponse(
            unidadMedida.Id,
            unidadMedida.Codigo,
            unidadMedida.Nombre,
            unidadMedida.Activa,
            unidadMedida.FechaCreacion);
    }
}
