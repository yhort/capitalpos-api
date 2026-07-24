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

        group.MapGet("/{productoId:guid}/presentaciones", ListarPresentacionesAsync)
            .WithName("ListarProductoPresentaciones")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPost("/{productoId:guid}/presentaciones", CrearPresentacionAsync)
            .WithName("CrearProductoPresentacion")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapGet("/{productoId:guid}/variantes", ListarVariantesAsync)
            .WithName("ListarProductoVariantes")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPost("/{productoId:guid}/variantes", CrearVarianteAsync)
            .WithName("CrearProductoVariante")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPatch("/{productoId:guid}/variantes/{varianteId:guid}/activar", ActivarVarianteAsync)
            .WithName("ActivarProductoVariante")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPatch("/{productoId:guid}/variantes/{varianteId:guid}/desactivar", DesactivarVarianteAsync)
            .WithName("DesactivarProductoVariante")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapGet("/{productoId:guid}/precios-mayoristas", ListarReglasPrecioMayoristaAsync)
            .WithName("ListarReglasPrecioMayorista")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPost("/{productoId:guid}/precios-mayoristas", CrearReglaPrecioMayoristaAsync)
            .WithName("CrearReglaPrecioMayorista")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPatch("/{productoId:guid}/precios-mayoristas/{reglaId:guid}/activar", ActivarReglaPrecioMayoristaAsync)
            .WithName("ActivarReglaPrecioMayorista")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPatch("/{productoId:guid}/precios-mayoristas/{reglaId:guid}/desactivar", DesactivarReglaPrecioMayoristaAsync)
            .WithName("DesactivarReglaPrecioMayorista")
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

    private static async Task<IResult> ListarPresentacionesAsync(
        Guid productoId,
        ListarProductoPresentacionesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var presentaciones = await useCase.EjecutarAsync(productoId, cancellationToken);

        return presentaciones is null
            ? Results.NotFound()
            : Results.Ok(presentaciones.Select(ProductoPresentacionResponse.From));
    }

    private static async Task<IResult> CrearPresentacionAsync(
        Guid productoId,
        CrearProductoPresentacionRequest request,
        CrearProductoPresentacionUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestSeguro = request with
            {
                ProductoId = productoId
            };
            if (!EndpointInputValidator.TryValidate(requestSeguro, out var error))
            {
                await AuditarProductoAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "CrearProductoPresentacion",
                    "CrearPresentacion",
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var presentacion = await useCase.EjecutarAsync(requestSeguro, cancellationToken);
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProductoPresentacion",
                "CrearPresentacion",
                AuditoriaResultados.Exitoso,
                $"ProductoId={productoId};PresentacionId={presentacion.Presentacion.Id}",
                cancellationToken);

            return Results.Created(
                $"/api/productos/{productoId}/presentaciones/{presentacion.Presentacion.Id}",
                ProductoPresentacionResponse.From(presentacion));
        }
        catch (ArgumentException ex)
        {
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProductoPresentacion",
                "CrearPresentacion",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProductoPresentacion",
                "CrearPresentacion",
                AuditoriaResultados.Rechazado,
                "ReferenciaFueraDeEmpresa",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> ListarVariantesAsync(
        Guid productoId,
        ListarProductoVariantesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var variantes = await useCase.EjecutarAsync(productoId, cancellationToken);

        return variantes is null
            ? Results.NotFound()
            : Results.Ok(variantes.Select(ProductoVarianteResponse.From));
    }

    private static async Task<IResult> CrearVarianteAsync(
        Guid productoId,
        CrearProductoVarianteRequest request,
        CrearProductoVarianteUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestSeguro = request with
            {
                ProductoId = productoId
            };
            if (!EndpointInputValidator.TryValidate(requestSeguro, out var error))
            {
                await AuditarProductoAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "CrearProductoVariante",
                    "CrearVariante",
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var variante = await useCase.EjecutarAsync(requestSeguro, cancellationToken);
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProductoVariante",
                "CrearVariante",
                AuditoriaResultados.Exitoso,
                $"ProductoId={productoId};VarianteId={variante.Id}",
                cancellationToken);

            return Results.Created(
                $"/api/productos/{productoId}/variantes/{variante.Id}",
                ProductoVarianteResponse.From(variante));
        }
        catch (ArgumentException ex)
        {
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProductoVariante",
                "CrearVariante",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await AuditarProductoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearProductoVariante",
                "CrearVariante",
                AuditoriaResultados.Rechazado,
                "ReferenciaFueraDeEmpresa",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> ActivarVarianteAsync(
        Guid productoId,
        Guid varianteId,
        ActivarProductoVarianteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var variante = await useCase.EjecutarAsync(productoId, varianteId, cancellationToken);

        return variante is null
            ? Results.NotFound()
            : Results.Ok(ProductoVarianteResponse.From(variante));
    }

    private static async Task<IResult> DesactivarVarianteAsync(
        Guid productoId,
        Guid varianteId,
        DesactivarProductoVarianteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var variante = await useCase.EjecutarAsync(productoId, varianteId, cancellationToken);

        return variante is null
            ? Results.NotFound()
            : Results.Ok(ProductoVarianteResponse.From(variante));
    }

    private static async Task<IResult> ListarReglasPrecioMayoristaAsync(
        Guid productoId,
        ListarReglasPrecioMayoristaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var reglas = await useCase.EjecutarAsync(productoId, cancellationToken);

        return reglas is null
            ? Results.NotFound()
            : Results.Ok(reglas.Select(ReglaPrecioMayoristaResponse.From));
    }

    private static async Task<IResult> CrearReglaPrecioMayoristaAsync(
        Guid productoId,
        CrearReglaPrecioMayoristaRequest request,
        CrearReglaPrecioMayoristaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var requestSeguro = request with
        {
            ProductoId = productoId
        };
        if (!EndpointInputValidator.TryValidate(requestSeguro, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        try
        {
            var regla = await useCase.EjecutarAsync(requestSeguro, cancellationToken);

            return Results.Created(
                $"/api/productos/{productoId}/precios-mayoristas/{regla.Id}",
                ReglaPrecioMayoristaResponse.From(regla));
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

    private static async Task<IResult> ActivarReglaPrecioMayoristaAsync(
        Guid productoId,
        Guid reglaId,
        ActivarReglaPrecioMayoristaUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var regla = await useCase.EjecutarAsync(productoId, reglaId, cancellationToken);

            return regla is null
                ? Results.NotFound()
                : Results.Ok(ReglaPrecioMayoristaResponse.From(regla));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> DesactivarReglaPrecioMayoristaAsync(
        Guid productoId,
        Guid reglaId,
        DesactivarReglaPrecioMayoristaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var regla = await useCase.EjecutarAsync(productoId, reglaId, cancellationToken);

        return regla is null
            ? Results.NotFound()
            : Results.Ok(ReglaPrecioMayoristaResponse.From(regla));
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
    Guid? CategoriaId,
    Guid? MarcaId,
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
            producto.CategoriaId,
            producto.MarcaId,
            producto.Activo,
            producto.FechaCreacion);
    }
}

public sealed record ProductoPresentacionResponse(
    Guid Id,
    Guid EmpresaId,
    Guid ProductoId,
    Guid? ProductoVarianteId,
    Guid UnidadMedidaId,
    string UnidadCodigo,
    string UnidadNombre,
    decimal FactorConversion,
    bool EsUnidadBase,
    decimal PrecioVenta,
    string CodigoBarras,
    bool Activo,
    DateTimeOffset FechaCreacion)
{
    public static ProductoPresentacionResponse From(ProductoPresentacionDetalle detalle)
    {
        return new ProductoPresentacionResponse(
            detalle.Presentacion.Id,
            detalle.Presentacion.EmpresaId,
            detalle.Presentacion.ProductoId,
            null,
            detalle.Presentacion.UnidadMedidaId,
            detalle.UnidadMedida.Codigo,
            detalle.UnidadMedida.Nombre,
            detalle.Presentacion.FactorConversion,
            detalle.Presentacion.EsUnidadBase,
            detalle.Presentacion.PrecioVenta,
            detalle.Presentacion.CodigoBarras,
            detalle.Presentacion.Activa,
            detalle.Presentacion.FechaCreacion);
    }
}

public sealed record ProductoVarianteResponse(
    Guid Id,
    Guid EmpresaId,
    Guid ProductoId,
    string Talla,
    string Color,
    string CodigoSku,
    string CodigoBarras,
    bool Activo,
    DateTimeOffset FechaCreacion)
{
    public static ProductoVarianteResponse From(ProductoVariante variante)
    {
        return new ProductoVarianteResponse(
            variante.Id,
            variante.EmpresaId,
            variante.ProductoId,
            variante.Talla,
            variante.Color,
            variante.CodigoSku,
            variante.CodigoBarras,
            variante.Activo,
            variante.FechaCreacion);
    }
}

public sealed record ReglaPrecioMayoristaResponse(
    Guid Id,
    Guid EmpresaId,
    Guid ProductoId,
    int CantidadMinima,
    decimal PrecioUnitarioMayorista,
    bool Activa,
    DateTimeOffset FechaCreacion)
{
    public static ReglaPrecioMayoristaResponse From(ReglaPrecioMayorista regla)
    {
        return new ReglaPrecioMayoristaResponse(
            regla.Id,
            regla.EmpresaId,
            regla.ProductoId,
            regla.CantidadMinima,
            regla.PrecioUnitarioMayorista,
            regla.Activa,
            regla.FechaCreacion);
    }
}
