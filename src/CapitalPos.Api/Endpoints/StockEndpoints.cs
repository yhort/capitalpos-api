using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock")
            .WithTags("Stock")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/productos/{productoId:guid}", ObtenerStockProductoAsync)
            .WithName("ObtenerStockProducto")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapGet("/productos/{productoId:guid}/variantes/{productoVarianteId:guid}", ObtenerStockProductoVarianteAsync)
            .WithName("ObtenerStockProductoVariante")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        group.MapPut("/ajustar", AjustarStockProductoAsync)
            .WithName("AjustarStockProducto")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        return app;
    }

    private static Task<IResult> ObtenerStockProductoAsync(
        Guid productoId,
        Guid sedeId,
        ObtenerStockProductoUseCase useCase,
        CancellationToken cancellationToken)
    {
        return ObtenerStockAsync(sedeId, productoId, null, useCase, cancellationToken);
    }

    private static Task<IResult> ObtenerStockProductoVarianteAsync(
        Guid productoId,
        Guid productoVarianteId,
        Guid sedeId,
        ObtenerStockProductoUseCase useCase,
        CancellationToken cancellationToken)
    {
        return ObtenerStockAsync(sedeId, productoId, productoVarianteId, useCase, cancellationToken);
    }

    private static async Task<IResult> ObtenerStockAsync(
        Guid sedeId,
        Guid productoId,
        Guid? productoVarianteId,
        ObtenerStockProductoUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (sedeId == Guid.Empty)
        {
            return Results.BadRequest(ErrorResponse.From("El identificador de la sede es obligatorio."));
        }

        if (productoId == Guid.Empty)
        {
            return Results.BadRequest(ErrorResponse.From("El identificador del producto es obligatorio."));
        }

        if (productoVarianteId == Guid.Empty)
        {
            return Results.BadRequest(ErrorResponse.From("El identificador de la variante no puede estar vacio."));
        }

        try
        {
            var stock = await useCase.EjecutarAsync(
                sedeId,
                productoId,
                productoVarianteId,
                cancellationToken);

            return stock is null
                ? Results.NotFound()
                : Results.Ok(StockProductoResponse.From(stock));
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

    private static async Task<IResult> AjustarStockProductoAsync(
        AjustarStockProductoRequest request,
        AjustarStockProductoUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!EndpointInputValidator.TryValidate(request, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        try
        {
            var stock = await useCase.EjecutarAsync(request, cancellationToken);

            return Results.Ok(StockProductoResponse.From(stock));
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
}

public sealed record StockProductoResponse(
    Guid EmpresaId,
    Guid SedeId,
    Guid ProductoId,
    Guid? ProductoVarianteId,
    decimal CantidadDisponible,
    decimal CantidadReservada,
    decimal StockLibre,
    DateTimeOffset FechaActualizacion)
{
    public static StockProductoResponse From(StockProducto stock)
    {
        return new StockProductoResponse(
            stock.EmpresaId,
            stock.SedeId,
            stock.ProductoId,
            stock.ProductoVarianteId,
            stock.CantidadDisponible,
            stock.CantidadReservada,
            stock.CantidadLibre,
            stock.FechaActualizacion);
    }
}
