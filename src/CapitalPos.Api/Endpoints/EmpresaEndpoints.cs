using CapitalPos.Application.Empresas;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class EmpresaEndpoints
{
    public static IEndpointRouteBuilder MapEmpresaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/empresas")
            .WithTags("Empresas")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/", ListarEmpresasAsync)
            .WithName("ListarEmpresas")
            .RequirePermisoEmpresa(PermisoEmpresa.ConsultarEmpresa);

        group.MapGet("/{id:guid}", ObtenerEmpresaPorIdAsync)
            .WithName("ObtenerEmpresaPorId")
            .RequirePermisoEmpresa(PermisoEmpresa.ConsultarEmpresa);

        group.MapPost("/", CrearEmpresaAsync)
            .WithName("CrearEmpresa")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarEmpresas);

        group.MapPatch("/{id:guid}/activar", ActivarEmpresaAsync)
            .WithName("ActivarEmpresa")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarEmpresas);

        group.MapPatch("/{id:guid}/desactivar", DesactivarEmpresaAsync)
            .WithName("DesactivarEmpresa")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarEmpresas);

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
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!EndpointInputValidator.TryValidate(request, out var error))
            {
                await AuditoriaEndpointHelper.AuditarAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "CrearEmpresa",
                    "Empresa",
                    "Crear",
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var empresa = await useCase.EjecutarAsync(request, cancellationToken);
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearEmpresa",
                "Empresa",
                "Crear",
                AuditoriaResultados.Exitoso,
                $"EmpresaId={empresa.Id}",
                cancellationToken);

            return Results.Created(
                $"/api/empresas/{empresa.Id}",
                EmpresaResponse.From(empresa));
        }
        catch (ArgumentException ex)
        {
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearEmpresa",
                "Empresa",
                "Crear",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearEmpresa",
                "Empresa",
                "Crear",
                AuditoriaResultados.Rechazado,
                "ConflictoDeDominio",
                cancellationToken);

            return Results.Conflict(ErrorResponse.From(ex.Message));
        }
        catch
        {
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearEmpresa",
                "Empresa",
                "Crear",
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static async Task<IResult> ActivarEmpresaAsync(
        Guid id,
        ActivarEmpresaUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var empresa = await useCase.EjecutarAsync(id, cancellationToken);
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ActivarEmpresa",
                "Empresa",
                "Activar",
                empresa is null ? AuditoriaResultados.Rechazado : AuditoriaResultados.Exitoso,
                $"EmpresaId={id}",
                cancellationToken);

            return empresa is null
                ? Results.NotFound()
                : Results.Ok(EmpresaResponse.From(empresa));
        }
        catch
        {
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ActivarEmpresa",
                "Empresa",
                "Activar",
                AuditoriaResultados.Error,
                $"EmpresaId={id}",
                cancellationToken);
            throw;
        }
    }

    private static async Task<IResult> DesactivarEmpresaAsync(
        Guid id,
        DesactivarEmpresaUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var empresa = await useCase.EjecutarAsync(id, cancellationToken);
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "DesactivarEmpresa",
                "Empresa",
                "Desactivar",
                empresa is null ? AuditoriaResultados.Rechazado : AuditoriaResultados.Exitoso,
                $"EmpresaId={id}",
                cancellationToken);

            return empresa is null
                ? Results.NotFound()
                : Results.Ok(EmpresaResponse.From(empresa));
        }
        catch
        {
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "DesactivarEmpresa",
                "Empresa",
                "Desactivar",
                AuditoriaResultados.Error,
                $"EmpresaId={id}",
                cancellationToken);
            throw;
        }
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
