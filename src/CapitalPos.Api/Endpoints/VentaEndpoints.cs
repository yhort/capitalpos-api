using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class VentaEndpoints
{
    public static IEndpointRouteBuilder MapVentaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ventas")
            .WithTags("Ventas")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapPost("/", CrearVentaAsync)
            .WithName("CrearVenta")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        return app;
    }

    private static async Task<IResult> CrearVentaAsync(
        CrearVentaRequest request,
        CrearVentaUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!EndpointInputValidator.TryValidate(request, out var error))
            {
                await AuditarVentaAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var venta = await useCase.EjecutarAsync(request, cancellationToken);
            await AuditarVentaAsync(
                auditoria,
                empresaActiva,
                httpContext,
                AuditoriaResultados.Exitoso,
                $"VentaId={venta.Id}",
                cancellationToken);

            return Results.Created(
                $"/api/ventas/{venta.Id}",
                VentaResponse.From(venta));
        }
        catch (ArgumentException ex)
        {
            await AuditarVentaAsync(
                auditoria,
                empresaActiva,
                httpContext,
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await AuditarVentaAsync(
                auditoria,
                empresaActiva,
                httpContext,
                AuditoriaResultados.Rechazado,
                "ReferenciaFueraDeEmpresa",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch
        {
            await AuditarVentaAsync(
                auditoria,
                empresaActiva,
                httpContext,
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static Task AuditarVentaAsync(
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        string resultado,
        string? detalle,
        CancellationToken cancellationToken)
    {
        return AuditoriaEndpointHelper.AuditarAsync(
            auditoria,
            empresaActiva,
            httpContext,
            "CrearVenta",
            "Venta",
            "Crear",
            resultado,
            detalle,
            cancellationToken);
    }
}

public sealed record VentaResponse(
    Guid Id,
    Guid EmpresaId,
    Guid? ClienteId,
    DateTimeOffset Fecha,
    decimal Subtotal,
    decimal Igv,
    decimal Total,
    string Estado,
    DateTimeOffset FechaCreacion,
    IReadOnlyCollection<VentaDetalleResponse> Detalles)
{
    public static VentaResponse From(Venta venta)
    {
        return new VentaResponse(
            venta.Id,
            venta.EmpresaId,
            venta.ClienteId,
            venta.Fecha,
            venta.Subtotal,
            venta.Igv,
            venta.Total,
            venta.Estado.ToString(),
            venta.FechaCreacion,
            venta.Detalles.Select(VentaDetalleResponse.From).ToArray());
    }
}

public sealed record VentaDetalleResponse(
    Guid Id,
    Guid ProductoId,
    Guid? ProductoVarianteId,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Igv,
    decimal Total)
{
    public static VentaDetalleResponse From(VentaDetalle detalle)
    {
        return new VentaDetalleResponse(
            detalle.Id,
            detalle.ProductoId,
            detalle.ProductoVarianteId,
            detalle.Cantidad,
            detalle.PrecioUnitario,
            detalle.Igv,
            detalle.Total);
    }
}
