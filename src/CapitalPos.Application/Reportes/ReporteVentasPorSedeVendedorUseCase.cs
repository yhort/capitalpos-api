using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Application.Reportes;

public sealed class ReporteVentasPorSedeVendedorUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IVentaRepository _ventaRepository;

    public ReporteVentasPorSedeVendedorUseCase(
        IVentaRepository ventaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ReporteVentasPorSedeVendedorResponse> EjecutarAsync(
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

        var items = ventasEnRango
            .GroupBy(venta => (venta.SedeId, venta.VendedorId))
            .OrderBy(grupo => grupo.Key.SedeId)
            .ThenBy(grupo => grupo.Key.VendedorId)
            .Select(grupo => CrearItem(grupo.Key.SedeId, grupo.Key.VendedorId, grupo))
            .ToArray();
        var totalGeneral = CrearResumen(items);

        return new ReporteVentasPorSedeVendedorResponse(desde, hasta, items, totalGeneral);
    }

    private static ReporteVentasPorSedeVendedorItem CrearItem(
        Guid sedeId,
        Guid? vendedorId,
        IEnumerable<Venta> ventas)
    {
        var ventasArray = ventas.ToArray();
        var unidades = ventasArray.Sum(venta => venta.Detalles.Sum(detalle => detalle.Cantidad));
        var soles = ventasArray.Sum(venta => venta.Total);

        return new ReporteVentasPorSedeVendedorItem(
            sedeId,
            vendedorId,
            ventasArray.Length,
            unidades,
            soles,
            CalcularPrecioPromedio(soles, unidades));
    }

    private static ReporteVentasPorSedeVendedorTotal CrearResumen(
        IReadOnlyCollection<ReporteVentasPorSedeVendedorItem> items)
    {
        var unidades = items.Sum(item => item.Unidades);
        var soles = items.Sum(item => item.Soles);
        var cantidadVentas = items.Sum(item => item.CantidadVentas);

        return new ReporteVentasPorSedeVendedorTotal(
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

public sealed record ReporteVentasPorSedeVendedorResponse(
    DateOnly Desde,
    DateOnly Hasta,
    IReadOnlyCollection<ReporteVentasPorSedeVendedorItem> Items,
    ReporteVentasPorSedeVendedorTotal TotalGeneral);

public sealed record ReporteVentasPorSedeVendedorItem(
    Guid SedeId,
    Guid? VendedorId,
    int CantidadVentas,
    decimal Unidades,
    decimal Soles,
    decimal PrecioPromedio);

public sealed record ReporteVentasPorSedeVendedorTotal(
    int CantidadVentas,
    decimal Unidades,
    decimal Soles,
    decimal PrecioPromedio);
