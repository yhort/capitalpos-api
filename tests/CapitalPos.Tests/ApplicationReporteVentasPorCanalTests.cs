using CapitalPos.Application.Reportes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationReporteVentasPorCanalTests
{
    private static readonly Guid SedeIdPrueba = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PuntoVentaIdPrueba = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Reporte_ventas_por_canal_agrupa_canales_y_total_general()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var repository = new VentaRepositoryFake();
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            [(2m, 100m), (1m, 50m)]));
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            CanalVenta.PROVINCIA,
            new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero),
            [(3m, 90m)]));
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
            [(1m, 20m)]));
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 4, 30, 10, 0, 0, TimeSpan.Zero),
            [(9m, 900m)]));
        await repository.AgregarAsync(CrearVenta(
            otraEmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            [(99m, 999m)]));
        var useCase = new ReporteVentasPorCanalUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var reporte = await useCase.EjecutarAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31));

        Assert.Equal(new DateOnly(2026, 5, 1), reporte.Desde);
        Assert.Equal(new DateOnly(2026, 5, 31), reporte.Hasta);
        Assert.Equal(Enum.GetValues<CanalVenta>().Length, reporte.Items.Count);
        var tienda = reporte.Items.Single(item => item.CanalVenta == "TIENDA");
        Assert.Equal(1, tienda.CantidadVentas);
        Assert.Equal(3m, tienda.Unidades);
        Assert.Equal(150m, tienda.Soles);
        Assert.Equal(50m, tienda.PrecioPromedio);
        var provincia = reporte.Items.Single(item => item.CanalVenta == "PROVINCIA");
        Assert.Equal(1, provincia.CantidadVentas);
        Assert.Equal(3m, provincia.Unidades);
        Assert.Equal(90m, provincia.Soles);
        Assert.Equal(30m, provincia.PrecioPromedio);
        var marketing = reporte.Items.Single(item => item.CanalVenta == "MARKETING");
        Assert.Equal(1, marketing.CantidadVentas);
        Assert.Equal(1m, marketing.Unidades);
        Assert.Equal(20m, marketing.Soles);
        Assert.Equal(20m, marketing.PrecioPromedio);
        var mayorista = reporte.Items.Single(item => item.CanalVenta == "MAYORISTA");
        Assert.Equal(0, mayorista.CantidadVentas);
        Assert.Equal(0m, mayorista.Unidades);
        Assert.Equal(0m, mayorista.Soles);
        Assert.Equal(0m, mayorista.PrecioPromedio);
        Assert.Equal(3, reporte.TotalGeneral.CantidadVentas);
        Assert.Equal(7m, reporte.TotalGeneral.Unidades);
        Assert.Equal(260m, reporte.TotalGeneral.Soles);
        Assert.Equal(37.14m, reporte.TotalGeneral.PrecioPromedio);
    }

    [Fact]
    public async Task Reporte_ventas_por_canal_sin_ventas_devuelve_canales_en_cero()
    {
        var useCase = new ReporteVentasPorCanalUseCase(
            new VentaRepositoryFake(),
            new EmpresaActivaContextFake(Guid.NewGuid()));

        var reporte = await useCase.EjecutarAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31));

        Assert.All(reporte.Items, item =>
        {
            Assert.Equal(0, item.CantidadVentas);
            Assert.Equal(0m, item.Unidades);
            Assert.Equal(0m, item.Soles);
            Assert.Equal(0m, item.PrecioPromedio);
        });
        Assert.Equal(0, reporte.TotalGeneral.CantidadVentas);
        Assert.Equal(0m, reporte.TotalGeneral.Unidades);
        Assert.Equal(0m, reporte.TotalGeneral.Soles);
        Assert.Equal(0m, reporte.TotalGeneral.PrecioPromedio);
    }

    [Fact]
    public async Task Reporte_ventas_por_canal_rechaza_rango_invalido()
    {
        var useCase = new ReporteVentasPorCanalUseCase(
            new VentaRepositoryFake(),
            new EmpresaActivaContextFake(Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.EjecutarAsync(
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 5, 31)));
    }

    private static Venta CrearVenta(
        Guid empresaId,
        CanalVenta canalVenta,
        DateTimeOffset fecha,
        IReadOnlyCollection<(decimal Cantidad, decimal Total)> detalles)
    {
        var ventaId = Guid.NewGuid();
        var ventaDetalles = detalles
            .Select(detalle => new VentaDetalle(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                Guid.NewGuid(),
                detalle.Cantidad,
                detalle.Total / detalle.Cantidad,
                0m,
                detalle.Total))
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
            canalVenta: canalVenta);
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
                venta.EmpresaId == empresaId &&
                venta.Id == id));
        }
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
}
