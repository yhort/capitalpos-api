using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Pedidos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class PedidoDigitalEndpoints
{
    public static IEndpointRouteBuilder MapPedidoDigitalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pedidos-digitales")
            .WithTags("PedidosDigitales")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        group.MapGet("/", ListarPedidosDigitalesAsync)
            .WithName("ListarPedidosDigitales")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapGet("/{id:guid}", ObtenerPedidoDigitalAsync)
            .WithName("ObtenerPedidoDigitalPorId")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPost("/", CrearPedidoDigitalAsync)
            .WithName("CrearPedidoDigital")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPost("/{id:guid}/cancelar", CancelarPedidoDigitalAsync)
            .WithName("CancelarPedidoDigital")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPut("/{id:guid}/estado", ActualizarEstadoPedidoDigitalAsync)
            .WithName("ActualizarEstadoPedidoDigital")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPost("/{id:guid}/convertir-venta", ConvertirPedidoDigitalAVentaAsync)
            .WithName("ConvertirPedidoDigitalAVenta")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        return app;
    }

    private static async Task<IResult> ListarPedidosDigitalesAsync(
        string? estado,
        string? canalPedido,
        Guid? sedeId,
        ListarPedidosDigitalesUseCase useCase,
        CancellationToken cancellationToken)
    {
        EstadoPedidoDigital? estadoFiltro = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<EstadoPedidoDigital>(estado, true, out var estadoParseado)
                || !Enum.IsDefined(estadoParseado))
            {
                return Results.BadRequest(ErrorResponse.From("El estado del pedido digital no es valido."));
            }

            estadoFiltro = estadoParseado;
        }

        CanalPedidoDigital? canalFiltro = null;
        if (!string.IsNullOrWhiteSpace(canalPedido))
        {
            if (!Enum.TryParse<CanalPedidoDigital>(canalPedido, true, out var canalParseado)
                || !Enum.IsDefined(canalParseado))
            {
                return Results.BadRequest(ErrorResponse.From("El canal del pedido digital no es valido."));
            }

            canalFiltro = canalParseado;
        }

        try
        {
            var pedidos = await useCase.EjecutarAsync(
                estadoFiltro,
                canalFiltro,
                sedeId,
                cancellationToken);
            return Results.Ok(pedidos.Select(PedidoDigitalResponse.From).ToArray());
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

    private static async Task<IResult> ObtenerPedidoDigitalAsync(
        Guid id,
        ObtenerPedidoDigitalUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await useCase.EjecutarAsync(id, cancellationToken);
            return pedido is null
                ? Results.NotFound()
                : Results.Ok(PedidoDigitalResponse.From(pedido));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> CrearPedidoDigitalAsync(
        CrearPedidoDigitalRequest request,
        CrearPedidoDigitalUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!EndpointInputValidator.TryValidate(request, out var error))
            {
                await AuditarPedidoAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "CrearPedidoDigital",
                    "Crear",
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var pedido = await useCase.EjecutarAsync(request, cancellationToken);
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearPedidoDigital",
                "Crear",
                AuditoriaResultados.Exitoso,
                $"PedidoDigitalId={pedido.Id};Canal={pedido.CanalPedido};Estado={pedido.Estado}",
                cancellationToken);

            return Results.Created(
                $"/api/pedidos-digitales/{pedido.Id}",
                PedidoDigitalResponse.From(pedido));
        }
        catch (ArgumentException ex)
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearPedidoDigital",
                "Crear",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearPedidoDigital",
                "Crear",
                AuditoriaResultados.Rechazado,
                "ReglaDeNegocio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CrearPedidoDigital",
                "Crear",
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static async Task<IResult> CancelarPedidoDigitalAsync(
        Guid id,
        CancelarPedidoDigitalRequest? request,
        CancelarPedidoDigitalUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await useCase.EjecutarAsync(
                id,
                request ?? new CancelarPedidoDigitalRequest(),
                cancellationToken);
            if (pedido is null)
            {
                await AuditarPedidoAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "CancelarPedidoDigital",
                    "Cancelar",
                    AuditoriaResultados.Rechazado,
                    "NoEncontrado",
                    cancellationToken);
                return Results.NotFound();
            }

            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CancelarPedidoDigital",
                "Cancelar",
                AuditoriaResultados.Exitoso,
                $"PedidoDigitalId={pedido.Id};Estado={pedido.Estado}",
                cancellationToken);

            return Results.Ok(PedidoDigitalResponse.From(pedido));
        }
        catch (ArgumentException ex)
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CancelarPedidoDigital",
                "Cancelar",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CancelarPedidoDigital",
                "Cancelar",
                AuditoriaResultados.Rechazado,
                "ReglaDeNegocio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "CancelarPedidoDigital",
                "Cancelar",
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static async Task<IResult> ActualizarEstadoPedidoDigitalAsync(
        Guid id,
        ActualizarEstadoPedidoDigitalRequest? request,
        ActualizarEstadoPedidoDigitalUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request is null)
            {
                await AuditarPedidoAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "ActualizarEstadoPedidoDigital",
                    "ActualizarEstado",
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(
                    "La solicitud de actualizacion de estado es obligatoria."));
            }

            var pedido = await useCase.EjecutarAsync(id, request, cancellationToken);
            if (pedido is null)
            {
                await AuditarPedidoAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "ActualizarEstadoPedidoDigital",
                    "ActualizarEstado",
                    AuditoriaResultados.Rechazado,
                    "NoEncontrado",
                    cancellationToken);
                return Results.NotFound();
            }

            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ActualizarEstadoPedidoDigital",
                "ActualizarEstado",
                AuditoriaResultados.Exitoso,
                $"PedidoDigitalId={pedido.Id};Estado={pedido.Estado}",
                cancellationToken);

            return Results.Ok(PedidoDigitalResponse.From(pedido));
        }
        catch (ArgumentException ex)
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ActualizarEstadoPedidoDigital",
                "ActualizarEstado",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ActualizarEstadoPedidoDigital",
                "ActualizarEstado",
                AuditoriaResultados.Rechazado,
                "ReglaDeNegocio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ActualizarEstadoPedidoDigital",
                "ActualizarEstado",
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static async Task<IResult> ConvertirPedidoDigitalAVentaAsync(
        Guid id,
        ConvertirPedidoDigitalAVentaRequest? request,
        ConvertirPedidoDigitalAVentaUseCase useCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await useCase.EjecutarAsync(
                id,
                request ?? new ConvertirPedidoDigitalAVentaRequest(),
                cancellationToken);
            if (resultado is null)
            {
                await AuditarPedidoAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "ConvertirPedidoDigitalAVenta",
                    "ConvertirVenta",
                    AuditoriaResultados.Rechazado,
                    "NoEncontrado",
                    cancellationToken);
                return Results.NotFound();
            }

            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ConvertirPedidoDigitalAVenta",
                "ConvertirVenta",
                AuditoriaResultados.Exitoso,
                $"PedidoDigitalId={resultado.Pedido.Id};VentaId={resultado.Venta.Id};Estado={resultado.Pedido.Estado}",
                cancellationToken);

            return Results.Ok(ConvertirPedidoDigitalAVentaResponse.From(resultado));
        }
        catch (ArgumentException ex)
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ConvertirPedidoDigitalAVenta",
                "ConvertirVenta",
                AuditoriaResultados.Rechazado,
                "ValidacionDeDominio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ConvertirPedidoDigitalAVenta",
                "ConvertirVenta",
                AuditoriaResultados.Rechazado,
                "ReglaDeNegocio",
                cancellationToken);

            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch
        {
            await AuditarPedidoAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "ConvertirPedidoDigitalAVenta",
                "ConvertirVenta",
                AuditoriaResultados.Error,
                null,
                cancellationToken);
            throw;
        }
    }

    private static Task AuditarPedidoAsync(
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
            "PedidoDigital",
            accion,
            resultado,
            detalle,
            cancellationToken);
    }
}

public sealed record ConvertirPedidoDigitalAVentaResponse(
    PedidoDigitalResponse Pedido,
    VentaResponse Venta)
{
    public static ConvertirPedidoDigitalAVentaResponse From(ConvertirPedidoDigitalAVentaResult resultado)
    {
        return new ConvertirPedidoDigitalAVentaResponse(
            PedidoDigitalResponse.From(resultado.Pedido),
            VentaResponse.From(resultado.Venta));
    }
}

public sealed record PedidoDigitalResponse(
    Guid Id,
    Guid EmpresaId,
    Guid? ClienteId,
    Guid SedeId,
    Guid? PuntoVentaId,
    string CanalPedido,
    string Estado,
    DateTimeOffset FechaPedido,
    decimal Subtotal,
    decimal Igv,
    decimal Total,
    string ReferenciaExterna,
    string Observacion,
    DateTimeOffset FechaCreacion,
    DateTimeOffset FechaActualizacion,
    IReadOnlyCollection<PedidoDigitalDetalleResponse> Detalles,
    IReadOnlyCollection<PedidoDigitalHistorialEstadoResponse> HistorialEstados)
{
    public static PedidoDigitalResponse From(PedidoDigital pedido)
    {
        return new PedidoDigitalResponse(
            pedido.Id,
            pedido.EmpresaId,
            pedido.ClienteId,
            pedido.SedeId,
            pedido.PuntoVentaId,
            pedido.CanalPedido.ToString(),
            pedido.Estado.ToString(),
            pedido.FechaPedido,
            pedido.Subtotal,
            pedido.Igv,
            pedido.Total,
            pedido.ReferenciaExterna,
            pedido.Observacion,
            pedido.FechaCreacion,
            pedido.FechaActualizacion,
            pedido.Detalles.Select(PedidoDigitalDetalleResponse.From).ToArray(),
            pedido.HistorialEstados.Select(PedidoDigitalHistorialEstadoResponse.From).ToArray());
    }
}

public sealed record PedidoDigitalDetalleResponse(
    Guid Id,
    Guid ProductoId,
    Guid? ProductoVarianteId,
    Guid? ProductoPresentacionId,
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal FactorConversionAplicado,
    decimal CantidadBase,
    decimal Total)
{
    public static PedidoDigitalDetalleResponse From(PedidoDigitalDetalle detalle)
    {
        return new PedidoDigitalDetalleResponse(
            detalle.Id,
            detalle.ProductoId,
            detalle.ProductoVarianteId,
            detalle.ProductoPresentacionId,
            detalle.Descripcion,
            detalle.Cantidad,
            detalle.PrecioUnitario,
            detalle.FactorConversionAplicado,
            detalle.CantidadBase,
            detalle.Total);
    }
}

public sealed record PedidoDigitalHistorialEstadoResponse(
    Guid Id,
    string? EstadoAnterior,
    string EstadoNuevo,
    Guid? UsuarioId,
    DateTimeOffset Fecha,
    string Observacion)
{
    public static PedidoDigitalHistorialEstadoResponse From(PedidoDigitalHistorialEstado historial)
    {
        return new PedidoDigitalHistorialEstadoResponse(
            historial.Id,
            historial.EstadoAnterior?.ToString(),
            historial.EstadoNuevo.ToString(),
            historial.UsuarioId,
            historial.Fecha,
            historial.Observacion);
    }
}
