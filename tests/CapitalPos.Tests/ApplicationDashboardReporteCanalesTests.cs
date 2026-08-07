using CapitalPos.Application.Dashboard;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationDashboardReporteCanalesTests
{
    private static readonly Guid SedeIdPrueba = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PuntoVentaIdPrueba = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Reporte_canales_agrupa_monto_y_transacciones_del_dia_incluyendo_ceros()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventas = new VentaRepositoryFake();
        ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 2m, 100m)]));
        ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 11, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 1m, 50m)]));
        ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 3m, 80m)]));
        ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 14, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 9m, 900m)],
            EstadoVenta.Anulada));
        ventas.Ventas.Add(CrearVenta(
            otraEmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 99m, 990m)]));
        ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.PROVINCIA,
            new DateTimeOffset(2026, 7, 18, 0, 1, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 1m, 10m)]));
        var useCase = CrearUseCase(ventas, empresaId);

        var reporte = await useCase.EjecutarAsync();

        Assert.Equal(new DateOnly(2026, 7, 17), reporte.Fecha);
        Assert.Equal(Enum.GetValues<CanalVenta>().Length, reporte.Canales.Count);
        Assert.Equal(230m, reporte.Total.MontoFacturado);
        Assert.Equal(3, reporte.Total.CantidadTransacciones);

        var tienda = reporte.Canales.Single(c => c.CanalVenta == "TIENDA");
        Assert.Equal(150m, tienda.MontoFacturado);
        Assert.Equal(2, tienda.CantidadTransacciones);

        var marketing = reporte.Canales.Single(c => c.CanalVenta == "MARKETING");
        Assert.Equal(80m, marketing.MontoFacturado);
        Assert.Equal(1, marketing.CantidadTransacciones);

        var provincia = reporte.Canales.Single(c => c.CanalVenta == "PROVINCIA");
        Assert.Equal(0m, provincia.MontoFacturado);
        Assert.Equal(0, provincia.CantidadTransacciones);

        Assert.Contains(reporte.Canales, c => c.CanalVenta == "MAYORISTA" && c.CantidadTransacciones == 0);
        Assert.Contains(reporte.Canales, c => c.CanalVenta == "MAQUILA" && c.CantidadTransacciones == 0);
        Assert.Contains(reporte.Canales, c => c.CanalVenta == "OFERTAS" && c.CantidadTransacciones == 0);
    }

    [Fact]
    public async Task Reporte_canales_sin_ventas_devuelve_todos_los_canales_en_cero()
    {
        var empresaId = Guid.NewGuid();
        var useCase = CrearUseCase(new VentaRepositoryFake(), empresaId);

        var reporte = await useCase.EjecutarAsync();

        Assert.Equal(0m, reporte.Total.MontoFacturado);
        Assert.Equal(0, reporte.Total.CantidadTransacciones);
        Assert.Equal(Enum.GetValues<CanalVenta>().Length, reporte.Canales.Count);
        Assert.All(reporte.Canales, canal =>
        {
            Assert.Equal(0m, canal.MontoFacturado);
            Assert.Equal(0, canal.CantidadTransacciones);
        });
    }

    private static DashboardReporteCanalesUseCase CrearUseCase(VentaRepositoryFake ventas, Guid empresaId)
    {
        return new DashboardReporteCanalesUseCase(
            ventas,
            new EmpresaActivaContextFake(empresaId),
            new ClockFake(new DateTimeOffset(2026, 7, 17, 15, 42, 10, TimeSpan.FromHours(-5))));
    }

    private static Venta CrearVenta(
        Guid empresaId,
        CanalVenta canalVenta,
        DateTimeOffset fecha,
        IReadOnlyCollection<(Guid ProductoId, Guid? ProductoVarianteId, decimal Cantidad, decimal Total)> detalles,
        EstadoVenta estado = EstadoVenta.Registrada)
    {
        var ventaId = Guid.NewGuid();
        var ventaDetalles = detalles
            .Select(detalle => new VentaDetalle(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                detalle.ProductoId,
                detalle.Cantidad,
                detalle.Total / detalle.Cantidad,
                0m,
                detalle.Total,
                detalle.ProductoVarianteId))
            .ToArray();
        var total = ventaDetalles.Sum(detalle => detalle.Total);

        return new Venta(
            ventaId,
            empresaId,
            fecha,
            total,
            0m,
            total,
            ventaDetalles,
            SedeIdPrueba,
            PuntoVentaIdPrueba,
            canalVenta: canalVenta,
            estado: estado);
    }

    private sealed class ClockFake : IDashboardComercialClock
    {
        private readonly DateTimeOffset _ahoraLima;

        public ClockFake(DateTimeOffset ahoraLima)
        {
            _ahoraLima = ahoraLima;
        }

        public DateTimeOffset AhoraLima() => _ahoraLima;
    }

    private sealed class EmpresaActivaContextFake : IEmpresaActivaContext
    {
        public EmpresaActivaContextFake(Guid empresaId)
        {
            UsuarioId = Guid.NewGuid();
            EmpresaId = empresaId;
            Rol = RolEmpresa.Administrador;
            TieneEmpresaActiva = true;
        }

        public bool TieneEmpresaActiva { get; }

        public Guid UsuarioId { get; }

        public Guid EmpresaId { get; }

        public RolEmpresa Rol { get; }
    }

    private sealed class VentaRepositoryFake : IVentaRepository
    {
        public List<Venta> Ventas { get; } = new();

        public Task AgregarAsync(Venta venta, CancellationToken cancellationToken = default)
        {
            Ventas.Add(venta);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Venta>>(
                Ventas.Where(venta => venta.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<Venta>> ListarRegistradasPorEmpresaYFechaAsync(
            Guid empresaId,
            DateTimeOffset desde,
            DateTimeOffset hastaExclusivo,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Venta>>(
                Ventas.Where(venta =>
                    venta.EmpresaId == empresaId &&
                    venta.Estado == EstadoVenta.Registrada &&
                    venta.Fecha >= desde &&
                    venta.Fecha < hastaExclusivo).ToArray());
        }

        public Task<Venta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Ventas.FirstOrDefault(venta =>
                venta.EmpresaId == empresaId && venta.Id == id));
        }
    }
}
