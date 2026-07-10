using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClienteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes")
            .WithTags("Clientes")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/", ListarClientesAsync)
            .WithName("ListarClientes")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapGet("/{id:guid}", ObtenerClientePorIdAsync)
            .WithName("ObtenerClientePorId")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPost("/", CrearClienteAsync)
            .WithName("CrearCliente")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        return app;
    }

    private static async Task<IResult> ListarClientesAsync(
        ListarClientesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var clientes = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(clientes.Select(ClienteResponse.From));
    }

    private static async Task<IResult> ObtenerClientePorIdAsync(
        Guid id,
        ObtenerClientePorIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var cliente = await useCase.EjecutarAsync(id, cancellationToken);

        return cliente is null
            ? Results.NotFound()
            : Results.Ok(ClienteResponse.From(cliente));
    }

    private static async Task<IResult> CrearClienteAsync(
        CrearClienteRequest request,
        CrearClienteUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!EndpointInputValidator.TryValidate(request, out var error))
            {
                await AuditarClienteAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "CrearCliente",
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var cliente = await useCase.EjecutarAsync(request, cancellationToken);
            await AuditarClienteAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearCliente",
                AuditoriaResultados.Exitoso,
                $"ClienteId={cliente.Id}",
                cancellationToken);

            return Results.Created(
                $"/api/clientes/{cliente.Id}",
                ClienteResponse.From(cliente));
        }
        catch (ArgumentException ex)
        {
            await AuditarClienteAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearCliente",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch
        {
            await AuditarClienteAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearCliente",
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static Task AuditarClienteAsync(
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        string operacion,
        string resultado,
        string? detalle,
        CancellationToken cancellationToken)
    {
        return AuditoriaEndpointHelper.AuditarAsync(
            auditoria,
            empresaActiva,
            httpContext,
            operacion,
            "Cliente",
            "Crear",
            resultado,
            detalle,
            cancellationToken);
    }
}

public sealed record ClienteResponse(
    Guid Id,
    Guid EmpresaId,
    string TipoDocumento,
    string NumeroDocumento,
    string NombreRazonSocial,
    string Direccion,
    bool Activo,
    DateTimeOffset FechaCreacion)
{
    public static ClienteResponse From(Cliente cliente)
    {
        return new ClienteResponse(
            cliente.Id,
            cliente.EmpresaId,
            cliente.TipoDocumento,
            cliente.NumeroDocumento,
            cliente.NombreRazonSocial,
            cliente.Direccion,
            cliente.Activo,
            cliente.FechaCreacion);
    }
}
