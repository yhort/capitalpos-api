using System.Text.Json;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Series;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed class EmitirCpeDesdeVentaUseCase
{
    private static readonly TimeZoneInfo ZonaHorariaPeru = ObtenerZonaHorariaPeru();

    private readonly IClienteRepository _clienteRepository;
    private readonly IConfiguracionFiscalEmpresaRepository _configuracionFiscalRepository;
    private readonly ICpeGateway _cpeGateway;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoPresentacionRepository _productoPresentacionRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly ISerieComprobanteRepository _serieRepository;
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly IVentaRepository _ventaRepository;

    public EmitirCpeDesdeVentaUseCase(
        IVentaRepository ventaRepository,
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

    public async Task<EmitirCpeDesdeVentaResult?> EjecutarAsync(
        Guid ventaId,
        EmitirCpeDesdeVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var venta = await _ventaRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            ventaId,
            cancellationToken);
        if (venta is null)
        {
            return null;
        }

        var configuracionFiscal = await ObtenerConfiguracionFiscalAsync(request, cancellationToken);
        var serie = await ObtenerSerieComprobanteAsync(venta, request, cancellationToken);
        var correlativo = serie.ObtenerSiguienteCorrelativo();
        var cliente = await ObtenerClienteAsync(venta, cancellationToken);
        var items = await CrearItemsAsync(venta, cancellationToken);
        var payload = CrearPayload(venta, request, serie, correlativo, configuracionFiscal, cliente, items);
        var response = await _cpeGateway.EmitirAsync(payload, cancellationToken);

        if (EsEmisionExitosaParaCorrelativo(response))
        {
            serie.IncrementarCorrelativo();
            await _serieRepository.GuardarAsync(serie, cancellationToken);
        }

        return new EmitirCpeDesdeVentaResult(
            response,
            request.TipoComprobante.Trim().ToUpperInvariant(),
            serie.Serie,
            correlativo);
    }

    private async Task<ConfiguracionFiscalEmpresa> ObtenerConfiguracionFiscalAsync(
        EmitirCpeDesdeVentaRequest request,
        CancellationToken cancellationToken)
    {
        var configuracion = await _configuracionFiscalRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);
        if (configuracion is null)
        {
            throw new InvalidOperationException("La empresa activa no tiene configuracion fiscal para emitir CPE.");
        }

        if (!configuracion.Activa)
        {
            throw new InvalidOperationException("La configuracion fiscal de la empresa activa esta inactiva.");
        }

        if (!string.Equals(
            request.RucEmisor.Trim(),
            configuracion.Ruc,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException("El RUC emisor del request no coincide con la configuracion fiscal de la empresa activa.");
        }

        return configuracion;
    }

    private async Task<SerieComprobante> ObtenerSerieComprobanteAsync(
        Venta venta,
        EmitirCpeDesdeVentaRequest request,
        CancellationToken cancellationToken)
    {
        var tipoComprobante = request.TipoComprobante.Trim().ToUpperInvariant();
        var serie = await _serieRepository.ObtenerActivaPorSedeYTipoAsync(
            _empresaActiva.EmpresaId,
            venta.SedeId,
            tipoComprobante,
            cancellationToken);

        return serie
            ?? throw new InvalidOperationException("No existe una serie activa para la sede y tipo de comprobante de la venta.");
    }

    private async Task<Cliente> ObtenerClienteAsync(
        Venta venta,
        CancellationToken cancellationToken)
    {
        if (venta.ClienteId is null)
        {
            throw new InvalidOperationException("La venta debe tener cliente para emitir CPE.");
        }

        var cliente = await _clienteRepository.ObtenerPorEmpresaAsync(
            venta.EmpresaId,
            venta.ClienteId.Value,
            cancellationToken);

        return cliente
            ?? throw new InvalidOperationException("El cliente de la venta no pertenece a la empresa activa.");
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
                throw new InvalidOperationException("El producto de la venta no pertenece a la empresa activa.");
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
                    throw new InvalidOperationException("La variante de la venta no pertenece al producto y empresa activos.");
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
                    throw new InvalidOperationException("La presentacion de la venta no pertenece al producto y empresa activos.");
                }

                unidadMedida = await _unidadMedidaRepository.ObtenerPorIdAsync(
                    presentacion.UnidadMedidaId,
                    cancellationToken);
                if (unidadMedida is null)
                {
                    throw new InvalidOperationException("La unidad de medida de la presentacion de venta no existe.");
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
        EmitirCpeDesdeVentaRequest request,
        SerieComprobante serie,
        int correlativo,
        ConfiguracionFiscalEmpresa configuracionFiscal,
        Cliente cliente,
        IReadOnlyCollection<CpeItemPayload> items)
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
            tipoComprobante = request.TipoComprobante.Trim().ToUpperInvariant(),
            serie = serie.Serie,
            correlativo,
            fechaEmision = ConvertirFechaEmisionPeru(venta.Fecha),
            moneda = "PEN",
            tipoOperacion = "0101",
            formaPago = "CONTADO",
            montoPendientePago = 0m,
            cuotas = Array.Empty<object>(),
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

    private static string ObtenerCodigoProducto(
        Producto producto,
        ProductoVariante? variante)
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
            throw new InvalidOperationException("La empresa activa es obligatoria para emitir CPE desde una venta.");
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
