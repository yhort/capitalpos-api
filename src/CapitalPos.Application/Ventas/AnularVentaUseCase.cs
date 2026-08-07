using System.Text.Json;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed record AnularVentaRequest(
    string? Observacion = null,
    string? CodigoMotivo = null,
    string? DescripcionMotivo = null);

public sealed class AnularVentaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IComprobanteRepository _comprobanteRepository;
    private readonly EmitirNotaCreditoDesdeVentaUseCase _emitirNotaCredito;
    private readonly RegistrarComprobanteCpeUseCase _registrarComprobante;
    private readonly IStockProductoRepository _stockRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly IMovimientoInventarioRepository? _movimientos;

    public AnularVentaUseCase(
        IVentaRepository ventaRepository,
        IStockProductoRepository stockRepository,
        IComprobanteRepository comprobanteRepository,
        EmitirNotaCreditoDesdeVentaUseCase emitirNotaCredito,
        RegistrarComprobanteCpeUseCase registrarComprobante,
        IEmpresaActivaContext empresaActiva,
        IMovimientoInventarioRepository? movimientos = null)
    {
        _ventaRepository = ventaRepository;
        _stockRepository = stockRepository;
        _comprobanteRepository = comprobanteRepository;
        _emitirNotaCredito = emitirNotaCredito;
        _registrarComprobante = registrarComprobante;
        _empresaActiva = empresaActiva;
        _movimientos = movimientos;
    }

    public async Task<Venta?> EjecutarAsync(
        Guid ventaId,
        AnularVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (ventaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la venta es obligatorio.", nameof(ventaId));
        }

        ValidarEmpresaActiva();
        var empresaId = _empresaActiva.EmpresaId;
        var venta = await _ventaRepository.ObtenerPorEmpresaAsync(empresaId, ventaId, cancellationToken);
        if (venta is null)
        {
            return null;
        }

        if (venta.Estado == EstadoVenta.Anulada)
        {
            throw new InvalidOperationException("La venta ya se encuentra anulada.");
        }

        var emision = await _comprobanteRepository.ObtenerEmisionAceptadaPorVentaAsync(
            empresaId,
            venta.Id,
            cancellationToken);

        if (emision is not null)
        {
            await EmitirNotaCreditoSiCorrespondeAsync(
                venta,
                emision,
                request,
                cancellationToken);
        }

        await RevertirStockYAnularAsync(venta, request.Observacion, cancellationToken);
        return venta;
    }

    private async Task EmitirNotaCreditoSiCorrespondeAsync(
        Venta venta,
        Comprobante emision,
        AnularVentaRequest request,
        CancellationToken cancellationToken)
    {
        var notaExistente = await _comprobanteRepository
            .ObtenerNotaCreditoAceptadaPorComprobanteAfectadoAsync(
                venta.EmpresaId,
                emision.Id,
                cancellationToken);

        if (notaExistente is not null)
        {
            return;
        }

        var descripcionMotivo = !string.IsNullOrWhiteSpace(request.DescripcionMotivo)
            ? request.DescripcionMotivo
            : !string.IsNullOrWhiteSpace(request.Observacion)
                ? request.Observacion
                : "Anulacion de la operacion";

        var resultado = await _emitirNotaCredito.EjecutarAsync(
            venta.Id,
            new EmitirNotaCreditoDesdeVentaRequest(
                emision.Id,
                request.CodigoMotivo ?? EmitirNotaCreditoDesdeVentaUseCase.MotivoAnulacionOperacion,
                descripcionMotivo),
            cancellationToken);

        if (resultado is null)
        {
            throw new InvalidOperationException(
                "No se pudo emitir la nota de credito para anular la venta.");
        }

        if (!TryObtenerEmisionExitosa(resultado.GatewayResponse, out var datosCpe))
        {
            throw new InvalidOperationException(
                datosCpe.Mensaje is null
                    ? "La nota de credito fue rechazada o no pudo procesarse; la venta no fue anulada."
                    : $"La nota de credito no fue aceptada: {datosCpe.Mensaje}");
        }

        await _registrarComprobante.EjecutarAsync(
            new RegistrarComprobanteCpeRequest(
                venta.Id,
                resultado.TipoComprobante,
                resultado.Serie,
                resultado.Correlativo,
                datosCpe.Estado!,
                datosCpe.Mensaje,
                datosCpe.Hash,
                datosCpe.NombreXml,
                datosCpe.NombreZip,
                datosCpe.NombreCdr,
                resultado.ComprobanteAfectadoId,
                resultado.TipoComprobanteAfectado,
                resultado.SerieAfectada,
                resultado.CorrelativoAfectado,
                resultado.CodigoMotivo,
                resultado.DescripcionMotivo),
            cancellationToken);
    }

    private async Task RevertirStockYAnularAsync(
        Venta venta,
        string? observacion,
        CancellationToken cancellationToken)
    {
        var empresaId = _empresaActiva.EmpresaId;
        var stocks = new List<(StockProducto Stock, decimal Cantidad)>();
        foreach (var detalle in venta.Detalles)
        {
            var stock = await _stockRepository.ObtenerPorProductoAsync(
                empresaId,
                venta.SedeId,
                detalle.ProductoId,
                detalle.ProductoVarianteId,
                cancellationToken);
            if (stock is null)
            {
                throw new InvalidOperationException("No se encontro el stock para revertir la venta.");
            }

            stocks.Add((stock, detalle.CantidadBaseDescontada));
        }

        venta.Anular(observacion);
        foreach (var (stock, cantidad) in stocks)
        {
            var anterior = stock.CantidadDisponible;
            stock.Incrementar(cantidad);
            await _stockRepository.GuardarAsync(stock, cancellationToken);
            if (_movimientos is not null)
            {
                await _movimientos.AgregarAsync(
                    new MovimientoInventario(
                        Guid.NewGuid(),
                        empresaId,
                        venta.SedeId,
                        stock.ProductoId,
                        stock.ProductoVarianteId,
                        TipoMovimientoInventario.ANULACION_VENTA,
                        cantidad,
                        anterior,
                        stock.CantidadDisponible,
                        "VENTA",
                        venta.Id,
                        observacion,
                        usuarioId: _empresaActiva.UsuarioId),
                    cancellationToken);
            }
        }

        await _ventaRepository.GuardarCambiosAsync(cancellationToken);
    }

    private static bool TryObtenerEmisionExitosa(
        Application.Cpe.CpeGatewayResponse response,
        out DatosCpeEmitido datos)
    {
        datos = default;
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            datos = new DatosCpeEmitido(
                null,
                "El servicio CPE no respondio correctamente al emitir la nota de credito.",
                null,
                null,
                null,
                null);
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(response.Content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                datos = new DatosCpeEmitido(null, "Respuesta CPE invalida al emitir la nota de credito.", null, null, null, null);
                return false;
            }

            var rootOk = TryGetBoolean(root, "ok");
            var data = TryGetProperty(root, "data", out var dataElement) &&
                dataElement.ValueKind == JsonValueKind.Object
                    ? dataElement
                    : default;
            var dataOk = data.ValueKind == JsonValueKind.Object
                ? TryGetBoolean(data, "ok")
                : null;
            var fuente = data.ValueKind == JsonValueKind.Object ? data : root;
            var estado = TryGetString(fuente, "estado");
            var mensaje = TryGetString(fuente, "mensaje") ?? TryGetString(root, "mensaje");
            var ok = (dataOk ?? rootOk) == true;
            var aceptado = response.IsSuccessStatusCode &&
                ok &&
                (string.Equals(estado, "ACEPTADO", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(estado, "SIMULADO", StringComparison.OrdinalIgnoreCase));

            datos = new DatosCpeEmitido(
                estado,
                mensaje ?? "La nota de credito fue rechazada o no pudo procesarse; la venta no fue anulada.",
                TryGetString(fuente, "hash"),
                TryGetString(fuente, "nombreXml"),
                TryGetString(fuente, "nombreZip"),
                TryGetString(fuente, "nombreCdr"));

            return aceptado;
        }
        catch (JsonException)
        {
            datos = new DatosCpeEmitido(
                null,
                "No se pudo interpretar la respuesta del servicio CPE al emitir la nota de credito.",
                null,
                null,
                null,
                null);
            return false;
        }
    }

    private static string? TryGetString(JsonElement source, string propertyName)
    {
        if (!TryGetProperty(source, propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    private static bool? TryGetBoolean(JsonElement source, string propertyName)
    {
        if (!TryGetProperty(source, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static bool TryGetProperty(
        JsonElement source,
        string propertyName,
        out JsonElement value)
    {
        foreach (var property in source.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para anular ventas.");
        }
    }

    private readonly record struct DatosCpeEmitido(
        string? Estado,
        string? Mensaje,
        string? Hash,
        string? NombreXml,
        string? NombreZip,
        string? NombreCdr);
}
