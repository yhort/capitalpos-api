using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Caja;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class CajaEndpoints
{
    public static IEndpointRouteBuilder MapCajaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/caja/sesiones")
            .WithTags("Caja")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/abierta", ObtenerAbiertaAsync)
            .WithName("ObtenerSesionCajaAbierta")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapGet("/{sesionCajaId:guid}/resumen", ObtenerResumenAsync)
            .WithName("ObtenerResumenSesionCaja")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPost("/abrir", AbrirAsync)
            .WithName("AbrirSesionCaja")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPost("/{sesionCajaId:guid}/cerrar", CerrarAsync)
            .WithName("CerrarSesionCaja")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        return app;
    }

    private static async Task<IResult> ObtenerResumenAsync(
        Guid sesionCajaId,
        ObtenerResumenSesionCajaUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (sesionCajaId == Guid.Empty)
        {
            return Results.BadRequest(ErrorResponse.From(
                "El identificador de la sesion de caja es obligatorio."));
        }

        try
        {
            var resumen = await useCase.EjecutarAsync(sesionCajaId, cancellationToken);
            return resumen is null
                ? Results.NotFound()
                : Results.Ok(resumen);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> ObtenerAbiertaAsync(
        Guid puntoVentaId,
        ObtenerSesionCajaAbiertaUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (puntoVentaId == Guid.Empty)
        {
            return Results.BadRequest(ErrorResponse.From("El identificador del punto de venta es obligatorio."));
        }

        try
        {
            var sesionCaja = await useCase.EjecutarAsync(puntoVentaId, cancellationToken);

            return sesionCaja is null
                ? Results.NotFound()
                : Results.Ok(SesionCajaResponse.From(sesionCaja));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> AbrirAsync(
        AbrirSesionCajaRequest request,
        AbrirSesionCajaUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        try
        {
            var sesionCaja = await useCase.EjecutarAsync(request, cancellationToken);

            return Results.Created(
                $"/api/caja/sesiones/abierta?puntoVentaId={sesionCaja.PuntoVentaId}",
                SesionCajaResponse.From(sesionCaja));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> CerrarAsync(
        Guid sesionCajaId,
        CerrarSesionCajaRequest request,
        CerrarSesionCajaUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (sesionCajaId == Guid.Empty)
        {
            return Results.BadRequest(ErrorResponse.From("El identificador de la sesion de caja es obligatorio."));
        }

        if (!TryValidate(request, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        var requestNormalizado = request with
        {
            SesionCajaId = sesionCajaId
        };

        try
        {
            var sesionCaja = await useCase.EjecutarAsync(requestNormalizado, cancellationToken);

            return Results.Ok(SesionCajaResponse.From(sesionCaja));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static bool TryValidate(AbrirSesionCajaRequest request, out string error)
    {
        if (request.PuntoVentaId == Guid.Empty)
        {
            error = "El identificador del punto de venta es obligatorio.";
            return false;
        }

        if (request.MontoInicial < 0)
        {
            error = "El monto inicial no puede ser negativo.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidate(CerrarSesionCajaRequest request, out string error)
    {
        if (request.MontoDeclaradoCierre < 0)
        {
            error = "El monto declarado de cierre no puede ser negativo.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

public sealed record SesionCajaResponse(
    Guid Id,
    Guid EmpresaId,
    Guid SedeId,
    Guid PuntoVentaId,
    string Estado,
    decimal MontoInicial,
    decimal? MontoDeclaradoCierre,
    decimal? DiferenciaCierre,
    DateTimeOffset FechaApertura,
    DateTimeOffset? FechaCierre,
    string ObservacionApertura,
    string ObservacionCierre)
{
    public static SesionCajaResponse From(SesionCaja sesionCaja)
    {
        return new SesionCajaResponse(
            sesionCaja.Id,
            sesionCaja.EmpresaId,
            sesionCaja.SedeId,
            sesionCaja.PuntoVentaId,
            sesionCaja.Estado.ToString(),
            sesionCaja.MontoInicial,
            sesionCaja.MontoDeclaradoCierre,
            sesionCaja.DiferenciaCierre,
            sesionCaja.FechaApertura,
            sesionCaja.FechaCierre,
            sesionCaja.ObservacionApertura,
            sesionCaja.ObservacionCierre);
    }
}
