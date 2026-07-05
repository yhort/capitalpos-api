using CapitalPos.Application.Empresas;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class EmpresaEndpoints
{
    public static IEndpointRouteBuilder MapEmpresaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/empresas")
            .WithTags("Empresas");

        group.MapGet("/", ListarEmpresasAsync)
            .WithName("ListarEmpresas");

        group.MapGet("/{id:guid}", ObtenerEmpresaPorIdAsync)
            .WithName("ObtenerEmpresaPorId");

        group.MapPost("/", CrearEmpresaAsync)
            .WithName("CrearEmpresa");

        group.MapPatch("/{id:guid}/activar", ActivarEmpresaAsync)
            .WithName("ActivarEmpresa");

        group.MapPatch("/{id:guid}/desactivar", DesactivarEmpresaAsync)
            .WithName("DesactivarEmpresa");

        return app;
    }

    private static async Task<IResult> ListarEmpresasAsync(
        ListarEmpresasUseCase useCase,
        CancellationToken cancellationToken)
    {
        var empresas = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(empresas.Select(EmpresaResponse.From));
    }

    private static async Task<IResult> ObtenerEmpresaPorIdAsync(
        Guid id,
        ObtenerEmpresaPorIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var empresa = await useCase.EjecutarAsync(id, cancellationToken);

        return empresa is null
            ? Results.NotFound()
            : Results.Ok(EmpresaResponse.From(empresa));
    }

    private static async Task<IResult> CrearEmpresaAsync(
        CrearEmpresaRequest request,
        CrearEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var empresa = await useCase.EjecutarAsync(request, cancellationToken);

            return Results.Created(
                $"/api/empresas/{empresa.Id}",
                EmpresaResponse.From(empresa));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> ActivarEmpresaAsync(
        Guid id,
        ActivarEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var empresa = await useCase.EjecutarAsync(id, cancellationToken);

        return empresa is null
            ? Results.NotFound()
            : Results.Ok(EmpresaResponse.From(empresa));
    }

    private static async Task<IResult> DesactivarEmpresaAsync(
        Guid id,
        DesactivarEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var empresa = await useCase.EjecutarAsync(id, cancellationToken);

        return empresa is null
            ? Results.NotFound()
            : Results.Ok(EmpresaResponse.From(empresa));
    }
}

public sealed record EmpresaResponse(
    Guid Id,
    string Ruc,
    string RazonSocial,
    string NombreComercial,
    bool Activa,
    DateTimeOffset FechaCreacion)
{
    public static EmpresaResponse From(Empresa empresa)
    {
        return new EmpresaResponse(
            empresa.Id,
            empresa.Ruc,
            empresa.RazonSocial,
            empresa.NombreComercial,
            empresa.Activa,
            empresa.FechaCreacion);
    }
}
