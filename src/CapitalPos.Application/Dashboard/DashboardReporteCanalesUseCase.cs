using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Application.Dashboard;

public sealed class DashboardReporteCanalesUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IDashboardComercialClock _clock;
    private readonly IVentaRepository _ventaRepository;

    public DashboardReporteCanalesUseCase(
        IVentaRepository ventaRepository,
        IEmpresaActivaContext empresaActiva,
        IDashboardComercialClock clock)
    {
        _ventaRepository = ventaRepository;
        _empresaActiva = empresaActiva;
        _clock = clock;
    }

    public async Task<DashboardReporteCanalesResponse> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        var ultimaActualizacion = _clock.AhoraLima();
        var fecha = DateOnly.FromDateTime(ultimaActualizacion.Date);
        var inicioLima = new DateTimeOffset(fecha.ToDateTime(TimeOnly.MinValue), ultimaActualizacion.Offset);
        var desde = inicioLima.ToUniversalTime();
        var hastaExclusivo = inicioLima.AddDays(1).ToUniversalTime();

        var ventas = await _ventaRepository.ListarRegistradasPorEmpresaYFechaAsync(
            _empresaActiva.EmpresaId,
            desde,
            hastaExclusivo,
            cancellationToken);

        var canales = Enum.GetValues<CanalVenta>()
            .OrderBy(canal => (int)canal)
            .Select(canal => CrearCanal(canal, ventas.Where(venta => venta.CanalVenta == canal)))
            .ToArray();
        var total = new DashboardReporteCanalesTotal(
            canales.Sum(canal => canal.MontoFacturado),
            canales.Sum(canal => canal.CantidadTransacciones));

        return new DashboardReporteCanalesResponse(
            fecha,
            ultimaActualizacion,
            canales,
            total);
    }

    private static DashboardReporteCanalesItem CrearCanal(
        CanalVenta canalVenta,
        IEnumerable<Venta> ventas)
    {
        var ventasArray = ventas.ToArray();

        return new DashboardReporteCanalesItem(
            canalVenta.ToString(),
            ventasArray.Sum(venta => venta.Total),
            ventasArray.Length);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "La empresa activa es obligatoria para consultar el reporte de canales.");
        }
    }
}

public sealed record DashboardReporteCanalesResponse(
    DateOnly Fecha,
    DateTimeOffset UltimaActualizacion,
    IReadOnlyCollection<DashboardReporteCanalesItem> Canales,
    DashboardReporteCanalesTotal Total);

public sealed record DashboardReporteCanalesItem(
    string CanalVenta,
    decimal MontoFacturado,
    int CantidadTransacciones);

public sealed record DashboardReporteCanalesTotal(
    decimal MontoFacturado,
    int CantidadTransacciones);
