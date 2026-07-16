using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Application.Reportes;

public sealed class ReporteVentasPorCanalUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IVentaRepository _ventaRepository;

    public ReporteVentasPorCanalUseCase(
        IVentaRepository ventaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ReporteVentasPorCanalResponse> EjecutarAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        if (desde > hasta)
        {
            throw new ArgumentException("La fecha desde no puede ser mayor que la fecha hasta.", nameof(desde));
        }

        var ventas = await _ventaRepository.ListarPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);
        var ventasEnRango = ventas
            .Where(venta =>
                DateOnly.FromDateTime(venta.Fecha.Date) >= desde &&
                DateOnly.FromDateTime(venta.Fecha.Date) <= hasta)
            .ToArray();

        var items = Enum.GetValues<CanalVenta>()
            .Select(canal => CrearItem(canal, ventasEnRango.Where(venta => venta.CanalVenta == canal)))
            .ToArray();
        var totalGeneral = CrearResumen(items);

        return new ReporteVentasPorCanalResponse(desde, hasta, items, totalGeneral);
    }

    private static ReporteVentasPorCanalItem CrearItem(
        CanalVenta canalVenta,
        IEnumerable<Venta> ventas)
    {
        var ventasArray = ventas.ToArray();
        var unidades = ventasArray.Sum(venta => venta.Detalles.Sum(detalle => detalle.Cantidad));
        var soles = ventasArray.Sum(venta => venta.Total);

        return new ReporteVentasPorCanalItem(
            canalVenta.ToString(),
            ventasArray.Length,
            unidades,
            soles,
            CalcularPrecioPromedio(soles, unidades));
    }

    private static ReporteVentasPorCanalTotal CrearResumen(
        IReadOnlyCollection<ReporteVentasPorCanalItem> items)
    {
        var unidades = items.Sum(item => item.Unidades);
        var soles = items.Sum(item => item.Soles);
        var cantidadVentas = items.Sum(item => item.CantidadVentas);

        return new ReporteVentasPorCanalTotal(
            cantidadVentas,
            unidades,
            soles,
            CalcularPrecioPromedio(soles, unidades));
    }

    private static decimal CalcularPrecioPromedio(decimal soles, decimal unidades)
    {
        return unidades > 0
            ? Math.Round(soles / unidades, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar reportes.");
        }
    }
}

public sealed record ReporteVentasPorCanalResponse(
    DateOnly Desde,
    DateOnly Hasta,
    IReadOnlyCollection<ReporteVentasPorCanalItem> Items,
    ReporteVentasPorCanalTotal TotalGeneral);

public sealed record ReporteVentasPorCanalItem(
    string CanalVenta,
    int CantidadVentas,
    decimal Unidades,
    decimal Soles,
    decimal PrecioPromedio);

public sealed record ReporteVentasPorCanalTotal(
    int CantidadVentas,
    decimal Unidades,
    decimal Soles,
    decimal PrecioPromedio);
