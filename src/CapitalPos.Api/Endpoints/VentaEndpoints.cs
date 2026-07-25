using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Cpe;
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

        group.MapGet("/", ListarVentasAsync)
            .WithName("ListarVentas")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapGet("/{id:guid}", ObtenerVentaDetalleAsync)
            .WithName("ObtenerVentaDetalle")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPost("/", CrearVentaAsync)
            .WithName("CrearVenta")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarVentas);

        group.MapPost("/{id:guid}/emitir-cpe", EmitirCpeDesdeVentaAsync)
            .WithName("EmitirCpeDesdeVenta")
            .RequirePermisoEmpresa(PermisoEmpresa.EmitirCpe);

        return app;
    }

    private static async Task<IResult> ListarVentasAsync(
        string desde,
        string hasta,
        string? canalVenta,
        Guid? sedeId,
        Guid? puntoVentaId,
        ListarVentasUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(desde, "yyyy-MM-dd", out var fechaDesde)
            || !DateOnly.TryParseExact(hasta, "yyyy-MM-dd", out var fechaHasta))
        {
            return Results.BadRequest(ErrorResponse.From(
                "Los filtros desde y hasta son obligatorios y deben usar el formato yyyy-MM-dd."));
        }

        CanalVenta? canal = null;
        if (!string.IsNullOrWhiteSpace(canalVenta))
        {
            if (!Enum.TryParse<CanalVenta>(canalVenta, true, out var canalParseado)
                || !Enum.IsDefined(canalParseado))
            {
                return Results.BadRequest(ErrorResponse.From("El canal de venta no es valido."));
            }

            canal = canalParseado;
        }

        try
        {
            var ventas = await useCase.EjecutarAsync(
                fechaDesde,
                fechaHasta,
                canal,
                sedeId,
                puntoVentaId,
                cancellationToken);
            return Results.Ok(ventas);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> ObtenerVentaDetalleAsync(
        Guid id,
        ObtenerVentaDetalleUseCase useCase,
        CancellationToken cancellationToken)
    {
        var venta = await useCase.EjecutarAsync(id, cancellationToken);
        return venta is null
            ? Results.NotFound()
            : Results.Ok(venta);
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

    private static async Task<IResult> EmitirCpeDesdeVentaAsync(
        Guid id,
        EmitirCpeDesdeVentaRequest request,
        EmitirCpeDesdeVentaUseCase useCase,
        RegistrarComprobanteCpeUseCase registrarComprobanteUseCase,
        IAuditoriaOperaciones auditoria,
        IEmpresaActivaContext empresaActiva,
        ILoggerFactory loggerFactory,
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
                    "EmitirCpeDesdeVenta",
                    "Venta",
                    "EmitirCpe",
                    AuditoriaResultados.Rechazado,
                    "ValidacionDeEntrada",
                    cancellationToken);

                return Results.BadRequest(ErrorResponse.From(error));
            }

            var result = await useCase.EjecutarAsync(id, request, cancellationToken);
            if (result is null)
            {
                await AuditoriaEndpointHelper.AuditarAsync(
                    auditoria,
                    empresaActiva,
                    httpContext,
                    "EmitirCpeDesdeVenta",
                    "Venta",
                    "EmitirCpe",
                    AuditoriaResultados.Rechazado,
                    $"VentaId={id}",
                    cancellationToken);

                return Results.NotFound();
            }

            var response = result.GatewayResponse;
            LogDiagnosticoEmisionCpe(loggerFactory, response);
            var normalizedResponse = EmitirCpeResponseNormalizer.Normalizar(response);
            var publicResponse = EmitirCpeApiResponse.From(normalizedResponse.Body);
            await registrarComprobanteUseCase.EjecutarAsync(
                new RegistrarComprobanteCpeRequest(
                    id,
                    result.TipoComprobante,
                    result.Serie,
                    result.Correlativo,
                    normalizedResponse.Body.Estado,
                    normalizedResponse.Body.Mensaje,
                    normalizedResponse.Body.Hash,
                    normalizedResponse.Body.NombreXml,
                    normalizedResponse.Body.NombreZip,
                    normalizedResponse.Body.NombreCdr),
                cancellationToken);
            var resultado = normalizedResponse.Body.Ok
                ? AuditoriaResultados.Exitoso
                : normalizedResponse.StatusCode >= StatusCodes.Status500InternalServerError
                    ? AuditoriaResultados.Error
                    : AuditoriaResultados.Rechazado;

            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "EmitirCpeDesdeVenta",
                "Venta",
                "EmitirCpe",
                resultado,
                $"VentaId={id};Estado={normalizedResponse.Body.Estado};Codigo={normalizedResponse.Body.Codigo}",
                cancellationToken);

            return Results.Json(
                publicResponse,
                statusCode: normalizedResponse.StatusCode);
        }
        catch
        {
            await AuditoriaEndpointHelper.AuditarAsync(
                auditoria,
                empresaActiva,
                httpContext,
                "EmitirCpeDesdeVenta",
                "Venta",
                "EmitirCpe",
                AuditoriaResultados.Error,
                $"VentaId={id}",
                cancellationToken);
            throw;
        }
    }

    private static void LogDiagnosticoEmisionCpe(
        ILoggerFactory loggerFactory,
        CpeGatewayResponse response)
    {
        var diagnostics = EmitirCpeResponseNormalizer.ObtenerDiagnosticoSeguro(response);
        var logger = loggerFactory.CreateLogger("CapitalPos.Api.Cpe.EmisionVenta");
        logger.LogInformation(
            "Respuesta CPE de venta recibida. StatusCode={StatusCode}; TieneOk={TieneOk}; TieneDataEstado={TieneDataEstado}; TieneMensaje={TieneMensaje}",
            diagnostics.StatusCode,
            diagnostics.TieneOk,
            diagnostics.TieneDataEstado,
            diagnostics.TieneMensaje);
    }
}

public sealed record VentaResponse(
    Guid Id,
    Guid EmpresaId,
    Guid SedeId,
    Guid? ClienteId,
    string CanalVenta,
    Guid PuntoVentaId,
    Guid? VendedorId,
    DateTimeOffset Fecha,
    decimal Subtotal,
    decimal Igv,
    decimal Total,
    string Estado,
    DateTimeOffset FechaCreacion,
    IReadOnlyCollection<VentaDetalleResponse> Detalles,
    IReadOnlyCollection<VentaPagoResponse> Pagos)
{
    public static VentaResponse From(Venta venta)
    {
        return new VentaResponse(
            venta.Id,
            venta.EmpresaId,
            venta.SedeId,
            venta.ClienteId,
            venta.CanalVenta.ToString(),
            venta.PuntoVentaId,
            venta.VendedorId,
            venta.Fecha,
            venta.Subtotal,
            venta.Igv,
            venta.Total,
            venta.Estado.ToString(),
            venta.FechaCreacion,
            venta.Detalles.Select(VentaDetalleResponse.From).ToArray(),
            venta.Pagos.Select(VentaPagoResponse.From).ToArray());
    }
}

public sealed record VentaPagoResponse(
    Guid Id,
    string MetodoPago,
    decimal Monto,
    string? CodigoOperacion,
    string? Observacion,
    DateTimeOffset FechaCreacion)
{
    public static VentaPagoResponse From(VentaPago pago)
    {
        return new VentaPagoResponse(
            pago.Id,
            pago.MetodoPago.ToString(),
            pago.Monto,
            pago.CodigoOperacion,
            pago.Observacion,
            pago.FechaCreacion);
    }
}

public sealed record VentaDetalleResponse(
    Guid Id,
    Guid ProductoId,
    Guid? ProductoVarianteId,
    Guid? ProductoPresentacionId,
    decimal Cantidad,
    decimal FactorConversionAplicado,
    decimal CantidadBaseDescontada,
    bool PrecioMayoristaAplicado,
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
            detalle.ProductoPresentacionId,
            detalle.Cantidad,
            detalle.FactorConversionAplicado,
            detalle.CantidadBaseDescontada,
            detalle.PrecioMayoristaAplicado,
            detalle.PrecioUnitario,
            detalle.Igv,
            detalle.Total);
    }
}
