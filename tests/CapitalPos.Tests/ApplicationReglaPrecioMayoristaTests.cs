using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationReglaPrecioMayoristaTests
{
    [Fact]
    public async Task Crear_regla_precio_mayorista_usa_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var reglaRepository = new ReglaPrecioMayoristaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        var useCase = new CrearReglaPrecioMayoristaUseCase(
            reglaRepository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));

        var regla = await useCase.EjecutarAsync(new CrearReglaPrecioMayoristaRequest(productoId, 12, 35m));

        Assert.Equal(empresaId, regla.EmpresaId);
        Assert.Equal(productoId, regla.ProductoId);
        Assert.Equal(12, regla.CantidadMinima);
        Assert.Equal(35m, regla.PrecioUnitarioMayorista);
        Assert.True(regla.Activa);
        Assert.Same(regla, reglaRepository.Reglas.Single());
    }

    [Fact]
    public async Task Crear_regla_precio_mayorista_falla_si_producto_es_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaBId, "Polo", 59m));
        var useCase = new CrearReglaPrecioMayoristaUseCase(
            new ReglaPrecioMayoristaRepositoryFake(),
            productoRepository,
            new EmpresaActivaContextFake(empresaAId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearReglaPrecioMayoristaRequest(productoId, 12, 35m)));
    }

    [Fact]
    public async Task Crear_regla_precio_mayorista_falla_si_duplica_cantidad_minima_activa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var reglaRepository = new ReglaPrecioMayoristaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await reglaRepository.AgregarAsync(new ReglaPrecioMayorista(
            Guid.NewGuid(),
            empresaId,
            productoId,
            12,
            35m));
        var useCase = new CrearReglaPrecioMayoristaUseCase(
            reglaRepository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearReglaPrecioMayoristaRequest(productoId, 12, 30m)));

        Assert.Single(reglaRepository.Reglas);
    }

    [Fact]
    public async Task Listar_reglas_precio_mayorista_filtra_por_empresa_y_producto()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var reglaRepository = new ReglaPrecioMayoristaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await reglaRepository.AgregarAsync(new ReglaPrecioMayorista(Guid.NewGuid(), empresaId, productoId, 24, 30m));
        await reglaRepository.AgregarAsync(new ReglaPrecioMayorista(Guid.NewGuid(), empresaId, productoId, 12, 35m));
        await reglaRepository.AgregarAsync(new ReglaPrecioMayorista(Guid.NewGuid(), empresaId, otroProductoId, 12, 35m));
        await reglaRepository.AgregarAsync(new ReglaPrecioMayorista(Guid.NewGuid(), otraEmpresaId, productoId, 12, 35m));
        var useCase = new ListarReglasPrecioMayoristaUseCase(
            reglaRepository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));

        var reglas = await useCase.EjecutarAsync(productoId);

        Assert.NotNull(reglas);
        Assert.Equal([12, 24], reglas.Select(regla => regla.CantidadMinima));
        Assert.All(reglas, regla =>
        {
            Assert.Equal(empresaId, regla.EmpresaId);
            Assert.Equal(productoId, regla.ProductoId);
        });
    }

    [Fact]
    public async Task Activar_regla_precio_mayorista_activa_inactiva_y_valida_duplicado()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var reglaId = Guid.NewGuid();
        var reglaRepository = new ReglaPrecioMayoristaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        var regla = new ReglaPrecioMayorista(reglaId, empresaId, productoId, 12, 35m, activa: false);
        await reglaRepository.AgregarAsync(regla);
        var useCase = new ActivarReglaPrecioMayoristaUseCase(
            reglaRepository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));

        var activada = await useCase.EjecutarAsync(productoId, reglaId);

        Assert.Same(regla, activada);
        Assert.True(regla.Activa);

        var duplicadaId = Guid.NewGuid();
        var duplicada = new ReglaPrecioMayorista(duplicadaId, empresaId, productoId, 12, 30m, activa: false);
        await reglaRepository.AgregarAsync(duplicada);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(productoId, duplicadaId));
    }

    [Fact]
    public async Task Desactivar_regla_precio_mayorista_desactiva_regla_del_producto()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var reglaId = Guid.NewGuid();
        var reglaRepository = new ReglaPrecioMayoristaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        var regla = new ReglaPrecioMayorista(reglaId, empresaId, productoId, 12, 35m);
        await reglaRepository.AgregarAsync(regla);
        var useCase = new DesactivarReglaPrecioMayoristaUseCase(
            reglaRepository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));

        var desactivada = await useCase.EjecutarAsync(productoId, reglaId);

        Assert.Same(regla, desactivada);
        Assert.False(regla.Activa);
    }

    private sealed class ReglaPrecioMayoristaRepositoryFake : IReglaPrecioMayoristaRepository
    {
        public List<ReglaPrecioMayorista> Reglas { get; } = [];

        public Task AgregarAsync(ReglaPrecioMayorista regla, CancellationToken cancellationToken = default)
        {
            Reglas.Add(regla);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ReglaPrecioMayorista> reglas = Reglas
                .Where(regla => regla.EmpresaId == empresaId && regla.ProductoId == productoId)
                .OrderBy(regla => regla.CantidadMinima)
                .ToArray();

            return Task.FromResult(reglas);
        }

        public Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarActivasPorProductosAsync(
            Guid empresaId,
            IReadOnlyCollection<Guid> productoIds,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ReglaPrecioMayorista> reglas = Reglas
                .Where(regla => regla.EmpresaId == empresaId && regla.Activa && productoIds.Contains(regla.ProductoId))
                .ToArray();

            return Task.FromResult(reglas);
        }

        public Task<ReglaPrecioMayorista?> ObtenerPorEmpresaYProductoAsync(
            Guid empresaId,
            Guid productoId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Reglas.FirstOrDefault(regla =>
                regla.EmpresaId == empresaId &&
                regla.ProductoId == productoId &&
                regla.Id == id));
        }

        public Task<bool> ExisteActivaPorCantidadMinimaAsync(
            Guid empresaId,
            Guid productoId,
            int cantidadMinima,
            Guid? excluirId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Reglas.Any(regla =>
                regla.EmpresaId == empresaId &&
                regla.ProductoId == productoId &&
                regla.CantidadMinima == cantidadMinima &&
                regla.Activa &&
                (!excluirId.HasValue || regla.Id != excluirId.Value)));
        }

        public Task ActualizarAsync(ReglaPrecioMayorista regla, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ProductoRepositoryFake : IProductoRepository
    {
        private readonly List<Producto> _productos = [];

        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            _productos.Add(producto);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Producto> productos = _productos
                .Where(producto => producto.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(productos);
        }

        public Task<Producto?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_productos.FirstOrDefault(producto =>
                producto.EmpresaId == empresaId &&
                producto.Id == id));
        }

        public Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class EmpresaActivaContextFake : IEmpresaActivaContext
    {
        public EmpresaActivaContextFake()
        {
        }

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
