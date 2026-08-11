using CapitalPos.Application.Reportes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationReporteVentasPorSedeVendedorTests
{
    private static readonly Guid SedeA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SedeB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PuntoVentaIdPrueba = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid VendedorA = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid VendedorB = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    [Fact]
    public async Task Reporte_ventas_por_sede_vendedor_agrupa_y_total_general()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var repository = new VentaRepositoryFake();
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            SedeA,
            VendedorA,
            new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            [(2m, 100m), (1m, 50m)]));
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            SedeA,
            VendedorA,
            new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            [(1m, 40m)]));
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            SedeA,
            VendedorB,
            new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero),
            [(3m, 90m)]));
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            SedeB,
            null,
            new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
            [(1m, 20m)]));
        await repository.AgregarAsync(CrearVenta(
            empresaId,
            SedeA,
            VendedorA,
            new DateTimeOffset(2026, 4, 30, 10, 0, 0, TimeSpan.Zero),
            [(9m, 900m)]));
        await repository.AgregarAsync(CrearVenta(
            otraEmpresaId,
            SedeA,
            VendedorA,
            new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            [(99m, 999m)]));
        var useCase = new ReporteVentasPorSedeVendedorUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var reporte = await useCase.EjecutarAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31));

        Assert.Equal(new DateOnly(2026, 5, 1), reporte.Desde);
        Assert.Equal(new DateOnly(2026, 5, 31), reporte.Hasta);
        Assert.Equal(3, reporte.Items.Count);

        var sedeAVendedorA = reporte.Items.Single(item =>
            item.SedeId == SedeA && item.VendedorId == VendedorA);
        Assert.Equal(2, sedeAVendedorA.CantidadVentas);
        Assert.Equal(4m, sedeAVendedorA.Unidades);
        Assert.Equal(190m, sedeAVendedorA.Soles);
        Assert.Equal(47.5m, sedeAVendedorA.PrecioPromedio);

        var sedeAVendedorB = reporte.Items.Single(item =>
            item.SedeId == SedeA && item.VendedorId == VendedorB);
        Assert.Equal(1, sedeAVendedorB.CantidadVentas);
        Assert.Equal(3m, sedeAVendedorB.Unidades);
        Assert.Equal(90m, sedeAVendedorB.Soles);
        Assert.Equal(30m, sedeAVendedorB.PrecioPromedio);

        var sedeBSinVendedor = reporte.Items.Single(item =>
            item.SedeId == SedeB && item.VendedorId is null);
        Assert.Equal(1, sedeBSinVendedor.CantidadVentas);
        Assert.Equal(1m, sedeBSinVendedor.Unidades);
        Assert.Equal(20m, sedeBSinVendedor.Soles);
        Assert.Equal(20m, sedeBSinVendedor.PrecioPromedio);

        Assert.Equal(4, reporte.TotalGeneral.CantidadVentas);
        Assert.Equal(8m, reporte.TotalGeneral.Unidades);
        Assert.Equal(300m, reporte.TotalGeneral.Soles);
        Assert.Equal(37.5m, reporte.TotalGeneral.PrecioPromedio);
    }

    [Fact]
    public async Task Reporte_ventas_por_sede_vendedor_sin_ventas_devuelve_vacio()
    {
        var useCase = new ReporteVentasPorSedeVendedorUseCase(
            new VentaRepositoryFake(),
            new EmpresaActivaContextFake(Guid.NewGuid()));

        var reporte = await useCase.EjecutarAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31));

        Assert.Empty(reporte.Items);
        Assert.Equal(0, reporte.TotalGeneral.CantidadVentas);
        Assert.Equal(0m, reporte.TotalGeneral.Unidades);
        Assert.Equal(0m, reporte.TotalGeneral.Soles);
        Assert.Equal(0m, reporte.TotalGeneral.PrecioPromedio);
    }

    [Fact]
    public async Task Reporte_ventas_por_sede_vendedor_rechaza_rango_invalido()
    {
        var useCase = new ReporteVentasPorSedeVendedorUseCase(
            new VentaRepositoryFake(),
            new EmpresaActivaContextFake(Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.EjecutarAsync(
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 5, 31)));
    }

    private static Venta CrearVenta(
        Guid empresaId,
        Guid sedeId,
        Guid? vendedorId,
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
            sedeId,
            PuntoVentaIdPrueba,
            vendedorId: vendedorId);
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
