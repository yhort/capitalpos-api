using System.Text.Json;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed class EmitirCpeDesdeVentaUseCase
{
    private const string UbigeoTemporalEmisor = "150101";
    private const string DireccionTemporalEmisor = "AV. DEMO 123";
    private const string DepartamentoTemporalEmisor = "LIMA";
    private const string ProvinciaTemporalEmisor = "LIMA";
    private const string DistritoTemporalEmisor = "LIMA";

    private readonly IClienteRepository _clienteRepository;
    private readonly ICpeGateway _cpeGateway;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IVentaRepository _ventaRepository;

    public EmitirCpeDesdeVentaUseCase(
        IVentaRepository ventaRepository,
        IEmpresaRepository empresaRepository,
        IClienteRepository clienteRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        ICpeGateway cpeGateway,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
        _empresaRepository = empresaRepository;
        _clienteRepository = clienteRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _cpeGateway = cpeGateway;
        _empresaActiva = empresaActiva;
    }

    public async Task<CpeGatewayResponse?> EjecutarAsync(
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

        var empresa = await _empresaRepository.ObtenerPorIdAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);
        var cliente = await ObtenerClienteAsync(venta, cancellationToken);
        var items = await CrearItemsAsync(venta, cancellationToken);
        var payload = CrearPayload(venta, request, empresa, cliente, items);

        return await _cpeGateway.EmitirAsync(payload, cancellationToken);
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

            var subtotal = detalle.Total - detalle.Igv;
            items.Add(new CpeItemPayload(
                ObtenerCodigoProducto(producto, variante),
                ObtenerDescripcionProducto(producto, variante),
                "NIU",
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
        Empresa? empresa,
        Cliente cliente,
        IReadOnlyCollection<CpeItemPayload> items)
    {
        var razonSocialEmisor = string.IsNullOrWhiteSpace(empresa?.RazonSocial)
            ? $"EMISOR {request.RucEmisor}"
            : empresa.RazonSocial;
        var nombreComercialEmisor = string.IsNullOrWhiteSpace(empresa?.NombreComercial)
            ? razonSocialEmisor
            : empresa.NombreComercial;

        return JsonSerializer.SerializeToElement(new
        {
            rucEmisor = request.RucEmisor,
            emisor = new
            {
                ruc = request.RucEmisor,
                razonSocial = razonSocialEmisor,
                nombreComercial = nombreComercialEmisor,
                // Temporal hasta API-013/configuracion fiscal por empresa.
                ubigeo = UbigeoTemporalEmisor,
                direccion = DireccionTemporalEmisor,
                departamento = DepartamentoTemporalEmisor,
                provincia = ProvinciaTemporalEmisor,
                distrito = DistritoTemporalEmisor
            },
            tipoComprobante = request.TipoComprobante,
            serie = request.Serie,
            correlativo = request.Correlativo,
            fechaEmision = venta.Fecha.UtcDateTime,
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
        ProductoVariante? variante)
    {
        var atributos = new[]
            {
                variante?.Talla,
                variante?.Color
            }
            .Where(valor => !string.IsNullOrWhiteSpace(valor));
        var descripcionVariante = string.Join(" ", atributos);

        return string.IsNullOrWhiteSpace(descripcionVariante)
            ? producto.Nombre
            : $"{producto.Nombre} {descripcionVariante}";
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
