using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed class ObtenerVentaDetalleUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IVentaRepository _ventaRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _varianteRepository;

    public ObtenerVentaDetalleUseCase(
        IVentaRepository ventaRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository varianteRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
        _productoRepository = productoRepository;
        _varianteRepository = varianteRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<VentaDetalleCompletoResponse?> EjecutarAsync(
        Guid ventaId,
        CancellationToken cancellationToken = default)
    {
        if (ventaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la venta es obligatorio.", nameof(ventaId));
        }

        ValidarEmpresaActiva();
        var empresaId = _empresaActiva.EmpresaId;
        var venta = await _ventaRepository.ObtenerPorEmpresaAsync(
            empresaId,
            ventaId,
            cancellationToken);
        if (venta is null)
        {
            return null;
        }

        var detalles = new List<VentaDetalleLineaResponse>(venta.Detalles.Count);
        foreach (var detalle in venta.Detalles)
        {
            var producto = await _productoRepository.ObtenerPorEmpresaAsync(
                empresaId,
                detalle.ProductoId,
                cancellationToken);
            var variante = detalle.ProductoVarianteId.HasValue
                ? await _varianteRepository.ObtenerPorEmpresaAsync(
                    empresaId,
                    detalle.ProductoVarianteId.Value,
                    cancellationToken)
                : null;

            detalles.Add(VentaDetalleLineaResponse.From(
                detalle,
                CrearDescripcion(producto, variante, detalle)));
        }

        return VentaDetalleCompletoResponse.From(venta, detalles);
    }

    private static string CrearDescripcion(
        Producto? producto,
        ProductoVariante? variante,
        VentaDetalle detalle)
    {
        var nombreProducto = producto?.Nombre ?? $"Producto {detalle.ProductoId}";
        var atributos = new[] { variante?.Talla, variante?.Color }
            .Where(valor => !string.IsNullOrWhiteSpace(valor))
            .ToArray();
        var descripcion = atributos.Length == 0
            ? nombreProducto
            : $"{nombreProducto} - {string.Join(" / ", atributos)}";

        return detalle.ProductoPresentacionId.HasValue
            ? $"{descripcion} - Presentacion x{detalle.FactorConversionAplicado:0.##}"
            : descripcion;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar ventas.");
        }
    }
}

public sealed record VentaDetalleCompletoResponse(
    Guid Id,
    Guid EmpresaId,
    Guid SedeId,
    Guid PuntoVentaId,
    Guid? ClienteId,
    string CanalVenta,
    DateTimeOffset Fecha,
    string Estado,
    decimal Subtotal,
    decimal Igv,
    decimal Total,
    int CantidadItems,
    decimal UnidadesComerciales,
    IReadOnlyCollection<VentaDetalleLineaResponse> Detalles,
    IReadOnlyCollection<VentaPagoDetalleResponse> Pagos)
{
    public static VentaDetalleCompletoResponse From(
        Venta venta,
        IReadOnlyCollection<VentaDetalleLineaResponse> detalles)
    {
        return new VentaDetalleCompletoResponse(
            venta.Id,
            venta.EmpresaId,
            venta.SedeId,
            venta.PuntoVentaId,
            venta.ClienteId,
            venta.CanalVenta.ToString(),
            venta.Fecha,
            venta.Estado.ToString(),
            venta.Subtotal,
            venta.Igv,
            venta.Total,
            detalles.Count,
            detalles.Sum(detalle => detalle.Cantidad),
            detalles,
            venta.Pagos.Select(VentaPagoDetalleResponse.From).ToArray());
    }
}

public sealed record VentaPagoDetalleResponse(
    Guid Id,
    string MetodoPago,
    decimal Monto,
    string? CodigoOperacion,
    string? Observacion,
    DateTimeOffset FechaCreacion)
{
    public static VentaPagoDetalleResponse From(VentaPago pago)
    {
        return new VentaPagoDetalleResponse(
            pago.Id,
            pago.MetodoPago.ToString(),
            pago.Monto,
            pago.CodigoOperacion,
            pago.Observacion,
            pago.FechaCreacion);
    }
}

public sealed record VentaDetalleLineaResponse(
    Guid Id,
    Guid ProductoId,
    Guid? ProductoVarianteId,
    Guid? ProductoPresentacionId,
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Total,
    decimal FactorConversionAplicado,
    decimal CantidadBaseDescontada,
    bool PrecioMayoristaAplicado)
{
    public static VentaDetalleLineaResponse From(VentaDetalle detalle, string descripcion)
    {
        return new VentaDetalleLineaResponse(
            detalle.Id,
            detalle.ProductoId,
            detalle.ProductoVarianteId,
            detalle.ProductoPresentacionId,
            descripcion,
            detalle.Cantidad,
            detalle.PrecioUnitario,
            detalle.Total,
            detalle.FactorConversionAplicado,
            detalle.CantidadBaseDescontada,
            detalle.PrecioMayoristaAplicado);
    }
}
