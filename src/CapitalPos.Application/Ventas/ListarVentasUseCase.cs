using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed class ListarVentasUseCase
{
    private static readonly TimeSpan OffsetLima = TimeSpan.FromHours(-5);
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IVentaRepository _ventaRepository;

    public ListarVentasUseCase(
        IVentaRepository ventaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<IReadOnlyCollection<VentaResumenResponse>> EjecutarAsync(
        DateOnly desde,
        DateOnly hasta,
        CanalVenta? canalVenta = null,
        Guid? sedeId = null,
        Guid? puntoVentaId = null,
        CancellationToken cancellationToken = default)
    {
        ValidarEntrada(desde, hasta, sedeId, puntoVentaId);
        ValidarEmpresaActiva();

        var ventas = await _ventaRepository.ListarPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);

        return ventas
            .Where(venta =>
            {
                var fechaLima = DateOnly.FromDateTime(venta.Fecha.ToOffset(OffsetLima).Date);
                return fechaLima >= desde
                    && fechaLima <= hasta
                    && (!canalVenta.HasValue || venta.CanalVenta == canalVenta.Value)
                    && (!sedeId.HasValue || venta.SedeId == sedeId.Value)
                    && (!puntoVentaId.HasValue || venta.PuntoVentaId == puntoVentaId.Value);
            })
            .OrderByDescending(venta => venta.Fecha)
            .Select(VentaResumenResponse.From)
            .ToArray();
    }

    private static void ValidarEntrada(
        DateOnly desde,
        DateOnly hasta,
        Guid? sedeId,
        Guid? puntoVentaId)
    {
        if (desde > hasta)
        {
            throw new ArgumentException("La fecha desde no puede ser mayor que la fecha hasta.");
        }

        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede no puede estar vacio.", nameof(sedeId));
        }

        if (puntoVentaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta no puede estar vacio.", nameof(puntoVentaId));
        }
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar ventas.");
        }
    }
}

public sealed record VentaResumenResponse(
    Guid Id,
    Guid EmpresaId,
    Guid SedeId,
    Guid PuntoVentaId,
    string CanalVenta,
    DateTimeOffset Fecha,
    string Estado,
    decimal Subtotal,
    decimal Igv,
    decimal Total,
    int CantidadItems,
    decimal UnidadesComerciales,
    IReadOnlyCollection<VentaPagoResumenResponse> Pagos)
{
    public static VentaResumenResponse From(Venta venta)
    {
        return new VentaResumenResponse(
            venta.Id,
            venta.EmpresaId,
            venta.SedeId,
            venta.PuntoVentaId,
            venta.CanalVenta.ToString(),
            venta.Fecha,
            venta.Estado.ToString(),
            venta.Subtotal,
            venta.Igv,
            venta.Total,
            venta.Detalles.Count,
            venta.Detalles.Sum(detalle => detalle.Cantidad),
            venta.Pagos.Select(VentaPagoResumenResponse.From).ToArray());
    }
}

public sealed record VentaPagoResumenResponse(
    string MetodoPago,
    decimal Monto)
{
    public static VentaPagoResumenResponse From(VentaPago pago)
    {
        return new VentaPagoResumenResponse(
            pago.MetodoPago.ToString(),
            pago.Monto);
    }
}
