using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class SedeEndpoints
{
    public static IEndpointRouteBuilder MapSedeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sedes")
            .WithTags("Sedes")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/", ListarSedesAsync)
            .WithName("ListarSedes")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapGet("/{sedeId:guid}/puntos-venta", ListarPuntosVentaAsync)
            .WithName("ListarPuntosVentaPorSede")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        return app;
    }

    private static async Task<IResult> ListarSedesAsync(
        ListarSedesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var sedes = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(sedes.Select(SedeResponse.From));
    }

    private static async Task<IResult> ListarPuntosVentaAsync(
        Guid sedeId,
        ListarPuntosVentaUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var puntosVenta = await useCase.EjecutarAsync(sedeId, cancellationToken);

            return puntosVenta is null
                ? Results.NotFound()
                : Results.Ok(puntosVenta.Select(PuntoVentaResponse.From));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }
}

public sealed record SedeResponse(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    string Tipo,
    string CodigoEstablecimiento,
    string Direccion,
    string Distrito,
    string Provincia,
    string Departamento,
    bool Activa,
    DateTimeOffset FechaCreacion)
{
    public static SedeResponse From(Sede sede)
    {
        return new SedeResponse(
            sede.Id,
            sede.EmpresaId,
            sede.Nombre,
            sede.Tipo.ToString(),
            sede.CodigoEstablecimiento,
            sede.Direccion,
            sede.Distrito,
            sede.Provincia,
            sede.Departamento,
            sede.Activa,
            sede.FechaCreacion);
    }
}

public sealed record PuntoVentaResponse(
    Guid Id,
    Guid EmpresaId,
    Guid SedeId,
    string Nombre,
    bool Activo)
{
    public static PuntoVentaResponse From(PuntoVenta puntoVenta)
    {
        return new PuntoVentaResponse(
            puntoVenta.Id,
            puntoVenta.EmpresaId,
            puntoVenta.SedeId,
            puntoVenta.Nombre,
            puntoVenta.Activo);
    }
}
