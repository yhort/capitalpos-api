using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationHistorialVentasTests
{
    [Fact]
    public async Task Listar_aplica_fechas_lima_canal_sede_y_punto()
    {
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var puntoVentaId = Guid.NewGuid();
        var repository = new VentaRepositoryFake();
        repository.Ventas.Add(CrearVenta(
            empresaId,
            sedeId,
            puntoVentaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 25, 23, 30, 0, TimeSpan.FromHours(-5)),
            2m));
        repository.Ventas.Add(CrearVenta(
            empresaId,
            sedeId,
            puntoVentaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.FromHours(-5)),
            3m));
        var useCase = new ListarVentasUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var result = await useCase.EjecutarAsync(
            new DateOnly(2026, 7, 25),
            new DateOnly(2026, 7, 25),
            CanalVenta.TIENDA,
            sedeId,
            puntoVentaId);

        var venta = Assert.Single(result);
        Assert.Equal("TIENDA", venta.CanalVenta);
        Assert.Equal(2m, venta.UnidadesComerciales);
        Assert.Equal(1, venta.CantidadItems);
    }

    [Fact]
    public async Task Listar_rechaza_rango_invalido()
    {
        var useCase = new ListarVentasUseCase(
            new VentaRepositoryFake(),
            new EmpresaActivaContextFake(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.EjecutarAsync(
                new DateOnly(2026, 7, 26),
                new DateOnly(2026, 7, 25)));

        Assert.Contains("fecha desde", exception.Message);
    }

    private static Venta CrearVenta(
        Guid empresaId,
        Guid sedeId,
        Guid puntoVentaId,
        CanalVenta canal,
        DateTimeOffset fecha,
        decimal cantidad)
    {
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            Guid.NewGuid(),
            cantidad,
            10m,
            0m,
            cantidad * 10m);
        return new Venta(
            ventaId,
            empresaId,
            fecha,
            cantidad * 10m,
            0m,
            cantidad * 10m,
            [detalle],
            sedeId,
            puntoVentaId,
            canalVenta: canal);
    }

    private sealed class EmpresaActivaContextFake(Guid empresaId) : IEmpresaActivaContext
    {
        public bool TieneEmpresaActiva => true;
        public Guid UsuarioId => Guid.NewGuid();
        public Guid EmpresaId => empresaId;
        public RolEmpresa Rol => RolEmpresa.Administrador;
    }

    private sealed class VentaRepositoryFake : IVentaRepository
    {
        public List<Venta> Ventas { get; } = [];

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
                    venta.EmpresaId == empresaId
                    && venta.Fecha >= desde
                    && venta.Fecha < hastaExclusivo).ToArray());
        }

        public Task<Venta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Ventas.SingleOrDefault(venta => venta.EmpresaId == empresaId && venta.Id == id));
        }
    }
}
