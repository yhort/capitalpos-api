using System.Text.Json;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Series;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed record EmitirNotaCreditoDesdeVentaRequest(
    Guid ComprobanteAfectadoId,
    string? CodigoMotivo = null,
    string? DescripcionMotivo = null);

public sealed record EmitirNotaCreditoDesdeVentaResult(
    CpeGatewayResponse GatewayResponse,
    string TipoComprobante,
    string Serie,
    int Correlativo,
    Guid ComprobanteAfectadoId,
    string TipoComprobanteAfectado,
    string SerieAfectada,
    int CorrelativoAfectado,
    string CodigoMotivo,
    string DescripcionMotivo);

public class EmitirNotaCreditoDesdeVentaUseCase
{
    public const string TipoNotaCredito = "07";
    public const string MotivoAnulacionOperacion = "01";

    private static readonly TimeZoneInfo ZonaHorariaPeru = ObtenerZonaHorariaPeru();

    private readonly IClienteRepository _clienteRepository;
    private readonly IComprobanteRepository _comprobanteRepository;
    private readonly IConfiguracionFiscalEmpresaRepository _configuracionFiscalRepository;
    private readonly ICpeGateway _cpeGateway;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoPresentacionRepository _productoPresentacionRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly ISerieComprobanteRepository _serieRepository;
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly IVentaRepository _ventaRepository;

    public EmitirNotaCreditoDesdeVentaUseCase(
        IVentaRepository ventaRepository,
        IComprobanteRepository comprobanteRepository,
        IConfiguracionFiscalEmpresaRepository configuracionFiscalRepository,
        IClienteRepository clienteRepository,
        IProductoRepository productoRepository,
        IProductoPresentacionRepository productoPresentacionRepository,
        IUnidadMedidaRepository unidadMedidaRepository,
        IProductoVarianteRepository productoVarianteRepository,
        ISerieComprobanteRepository serieRepository,
        ICpeGateway cpeGateway,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
        _comprobanteRepository = comprobanteRepository;
        _configuracionFiscalRepository = configuracionFiscalRepository;
        _clienteRepository = clienteRepository;
        _productoRepository = productoRepository;
        _productoPresentacionRepository = productoPresentacionRepository;
        _unidadMedidaRepository = unidadMedidaRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _serieRepository = serieRepository;
        _cpeGateway = cpeGateway;
        _empresaActiva = empresaActiva;
    }

    public virtual async Task<EmitirNotaCreditoDesdeVentaResult?> EjecutarAsync(
        Guid ventaId,
        EmitirNotaCreditoDesdeVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var empresaId = _empresaActiva.EmpresaId;
        var venta = await _ventaRepository.ObtenerPorEmpresaAsync(empresaId, ventaId, cancellationToken);
        if (venta is null)
        {
            return null;
        }

        var comprobanteAfectado = await ObtenerComprobanteAfectadoAsync(
            empresaId,
            ventaId,
            request.ComprobanteAfectadoId,
            cancellationToken);

        var codigoMotivo = string.IsNullOrWhiteSpace(request.CodigoMotivo)
            ? MotivoAnulacionOperacion
            : request.CodigoMotivo.Trim();
        var descripcionMotivo = string.IsNullOrWhiteSpace(request.DescripcionMotivo)
            ? "Anulacion de la operacion"
            : request.DescripcionMotivo.Trim();

        var configuracionFiscal = await ObtenerConfiguracionFiscalAsync(cancellationToken);
        var serie = await ObtenerSerieNotaCreditoAsync(venta, comprobanteAfectado, cancellationToken);
        var correlativo = serie.ObtenerSiguienteCorrelativo();
        var cliente = await ObtenerClienteAsync(venta, cancellationToken);
        var items = await CrearItemsAsync(venta, cancellationToken);
        var payload = CrearPayload(
            venta,
            serie,
            correlativo,
            configuracionFiscal,
            cliente,
            items,
            comprobanteAfectado,
            codigoMotivo,
            descripcionMotivo);

        var response = await _cpeGateway.EmitirAsync(payload, cancellationToken);

        if (EsEmisionExitosaParaCorrelativo(response))
        {
            serie.IncrementarCorrelativo();
            await _serieRepository.GuardarAsync(serie, cancellationToken);
        }

        return new EmitirNotaCreditoDesdeVentaResult(
            response,
            TipoNotaCredito,
            serie.Serie,
            correlativo,
            comprobanteAfectado.Id,
            comprobanteAfectado.TipoComprobante,
            comprobanteAfectado.Serie,
            comprobanteAfectado.Correlativo,
            codigoMotivo,
            descripcionMotivo);
    }

    private async Task<Comprobante> ObtenerComprobanteAfectadoAsync(
        Guid empresaId,
        Guid ventaId,
        Guid comprobanteAfectadoId,
        CancellationToken cancellationToken)
    {
        if (comprobanteAfectadoId == Guid.Empty)
        {
            throw new ArgumentException("El comprobante afectado es obligatorio.", nameof(comprobanteAfectadoId));
        }

        var emision = await _comprobanteRepository.ObtenerEmisionAceptadaPorVentaAsync(
            empresaId,
            ventaId,
            cancellationToken);

        if (emision is null || emision.Id != comprobanteAfectadoId)
        {
            throw new InvalidOperationException(
                "No se encontro un comprobante de emision aceptado para la venta activa.");
        }

        return emision;
    }

    private async Task<ConfiguracionFiscalEmpresa> ObtenerConfiguracionFiscalAsync(
        CancellationToken cancellationToken)
    {
        var configuracion = await _configuracionFiscalRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);
        if (configuracion is null)
        {
            throw new InvalidOperationException(
                "La empresa activa no tiene configuracion fiscal para emitir nota de credito.");
        }

        if (!configuracion.Activa)
        {
            throw new InvalidOperationException(
                "La configuracion fiscal de la empresa activa esta inactiva.");
        }

        return configuracion;
    }

    private async Task<SerieComprobante> ObtenerSerieNotaCreditoAsync(
        Venta venta,
        Comprobante comprobanteAfectado,
        CancellationToken cancellationToken)
    {
        var prefijo = ResolverPrefijoSerieNotaCredito(comprobanteAfectado);
        var seriesSede = await _serieRepository.ListarPorSedeAsync(
            _empresaActiva.EmpresaId,
            venta.SedeId,
            cancellationToken);

        var serie = seriesSede
            .Where(s =>
                string.Equals(s.TipoComprobante, TipoNotaCredito, StringComparison.OrdinalIgnoreCase) &&
                s.Activa &&
                s.Serie.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Serie)
            .FirstOrDefault();

        if (serie is not null)
        {
            return serie;
        }

        var seriePreferida = prefijo == "F" ? "FC01" : "BC01";
        serie = await _serieRepository.ObtenerActivaAsync(
            _empresaActiva.EmpresaId,
            venta.SedeId,
            TipoNotaCredito,
            seriePreferida,
            cancellationToken);

        return serie
            ?? throw new InvalidOperationException(
                $"No existe una serie activa de nota de credito (07) con prefijo {prefijo} para la sede de la venta.");
    }

    private static string ResolverPrefijoSerieNotaCredito(Comprobante afectado)
    {
        if (afectado.Serie.StartsWith("F", StringComparison.OrdinalIgnoreCase) ||
            afectado.TipoComprobante == "01")
        {
            return "F";
        }

        if (afectado.Serie.StartsWith("B", StringComparison.OrdinalIgnoreCase) ||
            afectado.TipoComprobante == "03")
        {
            return "B";
        }

        throw new InvalidOperationException(
            "No se pudo determinar la serie de nota de credito a partir del comprobante afectado.");
    }

    private async Task<Cliente> ObtenerClienteAsync(
        Venta venta,
        CancellationToken cancellationToken)
    {
        if (venta.ClienteId is null)
        {
            throw new InvalidOperationException("La venta debe tener cliente para emitir nota de credito.");
        }

        var cliente = await _clienteRepository.ObtenerPorEmpresaAsync(
            venta.EmpresaId,
            venta.ClienteId.Value,
            cancellationToken);

        return cliente
            ?? throw new InvalidOperationException(
                "El cliente de la venta no pertenece a la empresa activa.");
    }

    private async Task<IReadOnlyCollection<CpeItemPayload>> CrearItemsAsync(
        Venta venta,
        CancellationToken cancellationToken)
    {
        var items = new List<CpeItemPayload>();
        foreach (var detalle in venta.Detalles)
        {
            var producto = await _productoRepository.ObtenerPorEmpresaAsync(
                venta.EmpresaId,
                detalle.ProductoId,
                cancellationToken);
            if (producto is null)
            {
                throw new InvalidOperationException(
                    "El producto de la venta no pertenece a la empresa activa.");
            }

            ProductoVariante? variante = null;
            if (detalle.ProductoVarianteId is not null)
            {
                variante = await _productoVarianteRepository.ObtenerPorEmpresaAsync(
                    venta.EmpresaId,
                    detalle.ProductoVarianteId.Value,
                    cancellationToken);
                if (variante is null || variante.ProductoId != producto.Id)
                {
                    throw new InvalidOperationException(
                        "La variante de la venta no pertenece al producto y empresa activos.");
                }
            }

            ProductoPresentacion? presentacion = null;
            UnidadMedida? unidadMedida = null;
            if (detalle.ProductoPresentacionId is not null)
            {
                presentacion = await _productoPresentacionRepository.ObtenerPorEmpresaAsync(
                    venta.EmpresaId,
                    detalle.ProductoPresentacionId.Value,
                    cancellationToken);
                if (presentacion is null || presentacion.ProductoId != producto.Id)
                {
                    throw new InvalidOperationException(
                        "La presentacion de la venta no pertenece al producto y empresa activos.");
                }

                unidadMedida = await _unidadMedidaRepository.ObtenerPorIdAsync(
                    presentacion.UnidadMedidaId,
                    cancellationToken);
                if (unidadMedida is null)
                {
                    throw new InvalidOperationException(
                        "La unidad de medida de la presentacion de venta no existe.");
                }
            }

            var subtotal = detalle.Total - detalle.Igv;
            items.Add(new CpeItemPayload(
                ObtenerCodigoProducto(producto, variante),
                ObtenerDescripcionProducto(producto, variante, unidadMedida),
                ObtenerUnidadMedida(unidadMedida),
                detalle.Cantidad,
                Redondear(subtotal / detalle.Cantidad),
                Redondear(detalle.Total / detalle.Cantidad),
                Redondear(subtotal),
                Redondear(detalle.Igv),
                Redondear(detalle.Total),
                "10"));
        }

        return items;
    }

    private static JsonElement CrearPayload(
        Venta venta,
        SerieComprobante serie,
        int correlativo,
        ConfiguracionFiscalEmpresa configuracionFiscal,
        Cliente cliente,
        IReadOnlyCollection<CpeItemPayload> items,
        Comprobante comprobanteAfectado,
        string codigoMotivo,
        string descripcionMotivo)
    {
        var nombreComercialEmisor = string.IsNullOrWhiteSpace(configuracionFiscal.NombreComercial)
            ? configuracionFiscal.RazonSocial
            : configuracionFiscal.NombreComercial;

        return JsonSerializer.SerializeToElement(new
        {
            rucEmisor = configuracionFiscal.Ruc,
            emisor = new
            {
                ruc = configuracionFiscal.Ruc,
                razonSocial = configuracionFiscal.RazonSocial,
                nombreComercial = nombreComercialEmisor,
                ubigeo = configuracionFiscal.Ubigeo,
                direccion = configuracionFiscal.Direccion,
                departamento = configuracionFiscal.Departamento,
                provincia = configuracionFiscal.Provincia,
                distrito = configuracionFiscal.Distrito
            },
            tipoComprobante = TipoNotaCredito,
            serie = serie.Serie,
            correlativo,
            fechaEmision = ConvertirFechaEmisionPeru(DateTimeOffset.UtcNow),
            moneda = "PEN",
            tipoOperacion = "0101",
            formaPago = "CONTADO",
            montoPendientePago = 0m,
            cuotas = Array.Empty<object>(),
            codigoMotivo,
            descripcionMotivo,
            documentoReferencia = new
            {
                tipoComprobante = comprobanteAfectado.TipoComprobante,
                serieCorrelativo = $"{comprobanteAfectado.Serie}-{comprobanteAfectado.Correlativo}"
            },
            cliente = new
            {
                tipoDocumento = MapearTipoDocumentoCliente(cliente.TipoDocumento),
                numeroDocumento = cliente.NumeroDocumento,
                razonSocial = cliente.NombreRazonSocial
            },
            items = items.Select(item => new
            {
                codigo = item.Codigo,
                descripcion = item.Descripcion,
                unidadMedida = item.UnidadMedida,
                cantidad = item.Cantidad,
                valorUnitario = item.ValorUnitario,
                precioUnitario = item.PrecioUnitario,
                subtotal = item.Subtotal,
                igv = item.Igv,
                total = item.Total,
                codigoAfectacionIgv = item.CodigoAfectacionIgv
            }).ToArray(),
            totalGravada = Redondear(venta.Subtotal),
            totalExonerada = 0m,
            totalInafecta = 0m,
            totalIgv = Redondear(venta.Igv),
            total = Redondear(venta.Total),
            montoEnLetras = string.Empty,
            ventaId = venta.Id,
            empresaId = venta.EmpresaId,
            clienteId = venta.ClienteId
        });
    }

    private static bool EsEmisionExitosaParaCorrelativo(CpeGatewayResponse response)
    {
        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(response.Content))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(response.Content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
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
            var estado = data.ValueKind == JsonValueKind.Object
                ? TryGetString(data, "estado")
                : TryGetString(root, "estado");

            return (dataOk ?? rootOk) == true &&
                EsEstadoQueConsumeCorrelativo(estado);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool EsEstadoQueConsumeCorrelativo(string? estado)
    {
        return string.Equals(estado, "SIMULADO", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(estado, "ACEPTADO", StringComparison.OrdinalIgnoreCase);
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

    private static DateTime ConvertirFechaEmisionPeru(DateTimeOffset fecha)
    {
        var fechaPeru = TimeZoneInfo.ConvertTime(fecha, ZonaHorariaPeru);
        return DateTime.SpecifyKind(fechaPeru.DateTime, DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo ObtenerZonaHorariaPeru()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
    }

    private static string MapearTipoDocumentoCliente(string tipoDocumento)
    {
        return tipoDocumento.Trim().ToUpperInvariant() switch
        {
            "DNI" or "1" => "1",
            "RUC" or "6" => "6",
            "CE" or "CARNET_EXTRANJERIA" or "4" => "4",
            "PASAPORTE" or "PASSPORT" or "7" => "7",
            _ => tipoDocumento.Trim()
        };
    }

    private static string ObtenerCodigoProducto(Producto producto, ProductoVariante? variante)
    {
        if (!string.IsNullOrWhiteSpace(variante?.CodigoSku))
        {
            return variante.CodigoSku;
        }

        if (!string.IsNullOrWhiteSpace(producto.CodigoSku))
        {
            return producto.CodigoSku;
        }

        if (!string.IsNullOrWhiteSpace(variante?.CodigoBarras))
        {
            return variante.CodigoBarras;
        }

        if (!string.IsNullOrWhiteSpace(producto.CodigoBarras))
        {
            return producto.CodigoBarras;
        }

        return producto.Id.ToString("N");
    }

    private static string ObtenerDescripcionProducto(
        Producto producto,
        ProductoVariante? variante,
        UnidadMedida? unidadMedida = null)
    {
        var atributos = new[]
            {
                variante?.Talla,
                variante?.Color,
                unidadMedida?.Codigo
            }
            .Where(valor => !string.IsNullOrWhiteSpace(valor));
        var descripcionVariante = string.Join(" ", atributos);

        return string.IsNullOrWhiteSpace(descripcionVariante)
            ? producto.Nombre
            : $"{producto.Nombre} {descripcionVariante}";
    }

    private static string ObtenerUnidadMedida(UnidadMedida? unidadMedida)
    {
        return string.IsNullOrWhiteSpace(unidadMedida?.Codigo)
            ? "NIU"
            : unidadMedida.Codigo;
    }

    private static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "La empresa activa es obligatoria para emitir nota de credito desde una venta.");
        }
    }

    private sealed record CpeItemPayload(
        string Codigo,
        string Descripcion,
        string UnidadMedida,
        decimal Cantidad,
        decimal ValorUnitario,
        decimal PrecioUnitario,
        decimal Subtotal,
        decimal Igv,
        decimal Total,
        string CodigoAfectacionIgv);
}
