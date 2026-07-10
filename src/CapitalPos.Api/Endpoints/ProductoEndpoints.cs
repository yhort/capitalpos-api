using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class ProductoEndpoints
{
    public static IEndpointRouteBuilder MapProductoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/productos")
            .WithTags("Productos")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/", ListarProductosAsync)
            .WithName("ListarProductos")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapGet("/{id:guid}", ObtenerProductoPorIdAsync)
            .WithName("ObtenerProductoPorId")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPost("/", CrearProductoAsync)
            .WithName("CrearProducto")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPatch("/{id:guid}/activar", ActivarProductoAsync)
            .WithName("ActivarProducto")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPatch("/{id:guid}/desactivar", DesactivarProductoAsync)
            .WithName("DesactivarProducto")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        return app;
    }

    private static async Task<IResult> ListarProductosAsync(
        ListarProductosUseCase useCase,
        CancellationToken cancellationToken)
    {
        var productos = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(productos.Select(ProductoResponse.From));
    }

    private static async Task<IResult> ObtenerProductoPorIdAsync(
        Guid id,
        ObtenerProductoPorIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var producto = await useCase.EjecutarAsync(id, cancellationToken);

        return producto is null
            ? Results.NotFound()
            : Results.Ok(ProductoResponse.From(producto));
    }

    private static async Task<IResult> CrearProductoAsync(
        CrearProductoRequest request,
        CrearProductoUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!EndpointInputValidator.TryValidate(request, out var error))
            {
                await AuditarProductoAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "CrearProducto",
                    "Crear",
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var producto = await useCase.EjecutarAsync(request, cancellationToken);
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProducto",
                "Crear",
                AuditoriaResultados.Exitoso,
                $"ProductoId={producto.Id}",
                cancellationToken);

            return Results.Created(
                $"/api/productos/{producto.Id}",
                ProductoResponse.From(producto));
        }
        catch (ArgumentException ex)
        {
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProducto",
                "Crear",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch
        {
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProducto",
                "Crear",
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static async Task<IResult> ActivarProductoAsync(
        Guid id,
        ActivarProductoUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var producto = await useCase.EjecutarAsync(id, cancellationToken);
        await AuditarProductoAsync(
            auditoria,
            empresaActiva,
            httpContext,
            "ActivarProducto",
            "Activar",
            producto is null ? AuditoriaResultados.Rechazado : AuditoriaResultados.Exitoso,
            $"ProductoId={id}",
            cancellationToken);

        return producto is null
            ? Results.NotFound()
            : Results.Ok(ProductoResponse.From(producto));
    }

    private static async Task<IResult> DesactivarProductoAsync(
        Guid id,
        DesactivarProductoUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var producto = await useCase.EjecutarAsync(id, cancellationToken);
        await AuditarProductoAsync(
            auditoria,
            empresaActiva,
            httpContext,
            "DesactivarProducto",
            "Desactivar",
            producto is null ? AuditoriaResultados.Rechazado : AuditoriaResultados.Exitoso,
            $"ProductoId={id}",
            cancellationToken);

        return producto is null
            ? Results.NotFound()
            : Results.Ok(ProductoResponse.From(producto));
    }

    private static Task AuditarProductoAsync(
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        string operacion,
        string accion,
        string resultado,
        string? detalle,
        CancellationToken cancellationToken)
    {
        return AuditoriaEndpointHelper.AuditarAsync(
            auditoria,
            empresaActiva,
            httpContext,
            operacion,
            "Producto",
            accion,
            resultado,
            detalle,
            cancellationToken);
    }
}

public sealed record ProductoResponse(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    string CodigoSku,
    string CodigoBarras,
    decimal PrecioVenta,
    decimal? Costo,
    bool Activo,
    DateTimeOffset FechaCreacion)
{
    public static ProductoResponse From(Producto producto)
    {
        return new ProductoResponse(
            producto.Id,
            producto.EmpresaId,
            producto.Nombre,
            producto.CodigoSku,
            producto.CodigoBarras,
            producto.PrecioVenta,
            producto.Costo,
            producto.Activo,
            producto.FechaCreacion);
    }
}
