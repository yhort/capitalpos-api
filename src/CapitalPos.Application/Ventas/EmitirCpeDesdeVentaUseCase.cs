using System.Text.Json;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed class EmitirCpeDesdeVentaUseCase
{
    private readonly ICpeGateway _cpeGateway;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IVentaRepository _ventaRepository;

    public EmitirCpeDesdeVentaUseCase(
        IVentaRepository ventaRepository,
        ICpeGateway cpeGateway,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
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

        var payload = CrearPayload(venta, request);

        return await _cpeGateway.EmitirAsync(payload, cancellationToken);
    }

    private static JsonElement CrearPayload(
        Venta venta,
        EmitirCpeDesdeVentaRequest request)
    {
        return JsonSerializer.SerializeToElement(new
        {
            tipoDocumento = request.TipoComprobante,
            tipoComprobante = request.TipoComprobante,
            serie = request.Serie,
            correlativo = request.Correlativo,
            numero = request.Correlativo.ToString(),
            rucEmisor = request.RucEmisor,
            ventaId = venta.Id,
            empresaId = venta.EmpresaId,
            clienteId = venta.ClienteId,
            fecha = venta.Fecha,
            subtotal = venta.Subtotal,
            igv = venta.Igv,
            total = venta.Total,
            detalles = venta.Detalles.Select(detalle => new
            {
                productoId = detalle.ProductoId,
                productoVarianteId = detalle.ProductoVarianteId,
                cantidad = detalle.Cantidad,
                precioUnitario = detalle.PrecioUnitario,
                igv = detalle.Igv,
                total = detalle.Total
            }).ToArray()
        });
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para emitir CPE desde una venta.");
        }
    }
}
