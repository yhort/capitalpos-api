using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Compras;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class CompraEndpoints
{
    public static IEndpointRouteBuilder MapCompraEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/compras")
            .WithTags("Compras")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/", ListarComprasAsync)
            .WithName("ListarCompras")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapGet("/{id:guid}", ObtenerCompraAsync)
            .WithName("ObtenerCompra")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPost("/", CrearCompraAsync)
            .WithName("CrearCompra")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        return app;
    }

    private static async Task<IResult> ListarComprasAsync(
        ListarComprasUseCase useCase,
        CancellationToken cancellationToken)
    {
        var compras = await useCase.EjecutarAsync(cancellationToken);
        return Results.Ok(compras.Select(CompraResponse.From).ToArray());
    }

    private static async Task<IResult> ObtenerCompraAsync(
        Guid id,
        ObtenerCompraUseCase useCase,
        CancellationToken cancellationToken)
    {
        var compra = await useCase.EjecutarAsync(id, cancellationToken);
        return compra is null
            ? Results.NotFound()
            : Results.Ok(CompraResponse.From(compra));
    }

    private static async Task<IResult> CrearCompraAsync(
        CrearCompraRequest request,
        CrearCompraUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!EndpointInputValidator.TryValidate(request, out var error))
            {
                await AuditarCompraAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var compra = await useCase.EjecutarAsync(request, cancellationToken);
            await AuditarCompraAsync(
                auditoria,
                empresaActiva,
                httpContext,
                AuditoriaResultados.Exitoso,
                $"CompraId={compra.Id}",
                cancellationToken);

            return Results.Created(
                $"/api/compras/{compra.Id}",
                CompraResponse.From(compra));
        }
        catch (ArgumentException ex)
        {
            await AuditarCompraAsync(
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
            await AuditarCompraAsync(
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
            await AuditarCompraAsync(
                auditoria,
                empresaActiva,
                httpContext,
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static Task AuditarCompraAsync(
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
            "CrearCompra",
            "Compra",
            "Crear",
            resultado,
            detalle,
            cancellationToken);
    }
}

public sealed record CompraResponse(
    Guid Id,
    Guid EmpresaId,
    Guid SedeId,
    string Proveedor,
    string TipoComprobante,
    string Serie,
    string Correlativo,
    DateTimeOffset FechaCompra,
    decimal Total,
    DateTimeOffset FechaCreacion,
    IReadOnlyCollection<CompraDetalleResponse> Detalles)
{
    public static CompraResponse From(Compra compra)
    {
        return new CompraResponse(
            compra.Id,
            compra.EmpresaId,
            compra.SedeId,
            compra.Proveedor,
            compra.TipoComprobante,
            compra.Serie,
            compra.Correlativo,
            compra.FechaCompra,
            compra.Total,
            compra.FechaCreacion,
            compra.Detalles.Select(CompraDetalleResponse.From).ToArray());
    }
}

public sealed record CompraDetalleResponse(
    Guid Id,
    Guid ProductoId,
    Guid? ProductoVarianteId,
    decimal Cantidad,
    decimal CostoUnitario,
    decimal Total)
{
    public static CompraDetalleResponse From(CompraDetalle detalle)
    {
        return new CompraDetalleResponse(
            detalle.Id,
            detalle.ProductoId,
            detalle.ProductoVarianteId,
            detalle.Cantidad,
            detalle.CostoUnitario,
            detalle.Total);
    }
}
