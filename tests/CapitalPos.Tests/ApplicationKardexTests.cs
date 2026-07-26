using CapitalPos.Application.Inventario;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationKardexTests
{
    private static readonly Guid EmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OtraEmpresaId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SedeId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid OtraSedeId = Guid.Parse("10000000-0000-0000-0000-000000000014");
    private static readonly Guid ProductoId = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly Guid OtroProductoId = Guid.Parse("10000000-0000-0000-0000-000000000016");
    private static readonly Guid VarianteId = Guid.Parse("10000000-0000-0000-0000-000000000007");

    [Fact]
    public async Task Listar_kardex_filtra_por_empresa_sede_producto_variante_y_fecha_lima()
    {
        var repository = new MovimientoInventarioRepositoryFake();
        var esperado = CrearMovimiento(
            EmpresaId,
            SedeId,
            ProductoId,
            VarianteId,
            TipoMovimientoInventario.VENTA,
            new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.FromHours(-5)));
        repository.Movimientos.Add(esperado);
        repository.Movimientos.Add(CrearMovimiento(OtraEmpresaId, SedeId, ProductoId, VarianteId));
        repository.Movimientos.Add(CrearMovimiento(EmpresaId, OtraSedeId, ProductoId, VarianteId));
        repository.Movimientos.Add(CrearMovimiento(EmpresaId, SedeId, OtroProductoId, VarianteId));
        repository.Movimientos.Add(CrearMovimiento(EmpresaId, SedeId, ProductoId, null));
        repository.Movimientos.Add(CrearMovimiento(
            EmpresaId,
            SedeId,
            ProductoId,
            VarianteId,
            TipoMovimientoInventario.AJUSTE,
            new DateTimeOffset(2026, 7, 25, 23, 0, 0, TimeSpan.FromHours(-5))));
        var useCase = new ListarKardexUseCase(
            repository,
            new EmpresaActivaContextFake(EmpresaId));

        var movimientos = await useCase.EjecutarAsync(
            ProductoId,
            VarianteId,
            SedeId,
            new DateOnly(2026, 7, 26),
            new DateOnly(2026, 7, 26));

        var movimiento = Assert.Single(movimientos);
        Assert.Same(esperado, movimiento);
        Assert.Equal(TipoMovimientoInventario.VENTA, movimiento.TipoMovimiento);
        Assert.Equal(2m, movimiento.Cantidad);
        Assert.Equal(10m, movimiento.StockAnterior);
        Assert.Equal(8m, movimiento.StockPosterior);
    }

    [Fact]
    public async Task Listar_kardex_requiere_empresa_activa_y_rango_valido()
    {
        var repository = new MovimientoInventarioRepositoryFake();
        var sinEmpresa = new ListarKardexUseCase(
            repository,
            new EmpresaActivaContextFake(Guid.Empty, tieneEmpresaActiva: false));
        var useCase = new ListarKardexUseCase(
            repository,
            new EmpresaActivaContextFake(EmpresaId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sinEmpresa.EjecutarAsync(null, null, null, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.EjecutarAsync(null, null, null, new DateOnly(2026, 8, 1), new DateOnly(2026, 7, 31)));
    }

    private static MovimientoInventario CrearMovimiento(
        Guid empresaId,
        Guid sedeId,
        Guid productoId,
        Guid? productoVarianteId,
        TipoMovimientoInventario tipoMovimiento = TipoMovimientoInventario.AJUSTE,
        DateTimeOffset? fechaCreacion = null)
    {
        return new MovimientoInventario(
            Guid.NewGuid(),
            empresaId,
            sedeId,
            productoId,
            productoVarianteId,
            tipoMovimiento,
            2m,
            10m,
            8m,
            "TEST",
            Guid.NewGuid(),
            "Validacion kardex",
            fechaCreacion ?? new DateTimeOffset(2026, 7, 26, 9, 0, 0, TimeSpan.FromHours(-5)));
    }

    private sealed class MovimientoInventarioRepositoryFake : IMovimientoInventarioRepository
    {
        public List<MovimientoInventario> Movimientos { get; } = new();

        public Task AgregarAsync(
            MovimientoInventario movimiento,
            CancellationToken cancellationToken = default)
        {
            Movimientos.Add(movimiento);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<MovimientoInventario>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<MovimientoInventario>>(
                Movimientos.Where(movimiento => movimiento.EmpresaId == empresaId).ToArray());
        }
    }

    private sealed class EmpresaActivaContextFake(
        Guid empresaId,
        Guid? usuarioId = null,
        bool tieneEmpresaActiva = true) : IEmpresaActivaContext
    {
        public bool TieneEmpresaActiva { get; } = tieneEmpresaActiva;
        public Guid UsuarioId { get; } = usuarioId ?? Guid.NewGuid();
        public Guid EmpresaId { get; } = empresaId;
        public RolEmpresa Rol { get; } = RolEmpresa.Administrador;
    }
}
