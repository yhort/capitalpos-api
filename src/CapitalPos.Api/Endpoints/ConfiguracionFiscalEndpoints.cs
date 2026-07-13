using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class ConfiguracionFiscalEndpoints
{
    public static IEndpointRouteBuilder MapConfiguracionFiscalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/configuracion-fiscal")
            .WithTags("Configuracion fiscal")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/", ObtenerConfiguracionFiscalAsync)
            .WithName("ObtenerConfiguracionFiscal")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarEmpresas);

        group.MapPut("/", GuardarConfiguracionFiscalAsync)
            .WithName("GuardarConfiguracionFiscal")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarEmpresas);

        return app;
    }

    private static async Task<IResult> ObtenerConfiguracionFiscalAsync(
        ObtenerConfiguracionFiscalEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var configuracion = await useCase.EjecutarAsync(cancellationToken);

        return configuracion is null
            ? Results.NotFound(ErrorResponse.From("La empresa activa no tiene configuracion fiscal."))
            : Results.Ok(ConfiguracionFiscalEmpresaResponse.From(configuracion));
    }

    private static async Task<IResult> GuardarConfiguracionFiscalAsync(
        GuardarConfiguracionFiscalEmpresaRequest request,
        GuardarConfiguracionFiscalEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!EndpointInputValidator.TryValidate(request, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        try
        {
            var configuracion = await useCase.EjecutarAsync(request, cancellationToken);

            return Results.Ok(ConfiguracionFiscalEmpresaResponse.From(configuracion));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }
}

public sealed record ConfiguracionFiscalEmpresaResponse(
    Guid EmpresaId,
    string Ruc,
    string RazonSocial,
    string NombreComercial,
    string Ubigeo,
    string Direccion,
    string Departamento,
    string Provincia,
    string Distrito,
    bool Activa,
    DateTimeOffset FechaCreacion)
{
    public static ConfiguracionFiscalEmpresaResponse From(ConfiguracionFiscalEmpresa configuracion)
    {
        return new ConfiguracionFiscalEmpresaResponse(
            configuracion.EmpresaId,
            configuracion.Ruc,
            configuracion.RazonSocial,
            configuracion.NombreComercial,
            configuracion.Ubigeo,
            configuracion.Direccion,
            configuracion.Departamento,
            configuracion.Provincia,
            configuracion.Distrito,
            configuracion.Activa,
            configuracion.FechaCreacion);
    }
}
