using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationProductoPresentacionTests
{
    [Fact]
    public async Task Listar_unidades_medida_devuelve_solo_activas()
    {
        var repository = new UnidadMedidaRepositoryFake();
        await repository.AgregarAsync(new UnidadMedida(Guid.NewGuid(), "UND", "Unidad"));
        await repository.AgregarAsync(new UnidadMedida(Guid.NewGuid(), "CAJ", "Caja", activa: false));
        var useCase = new ListarUnidadesMedidaUseCase(repository);

        var unidades = await useCase.EjecutarAsync();

        var unidad = Assert.Single(unidades);
        Assert.Equal("UND", unidad.Codigo);
    }

    [Fact]
    public async Task Crear_presentacion_use_case_asigna_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var presentacionRepository = new ProductoPresentacionRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var unidadRepository = new UnidadMedidaRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Producto", 10m));
        await unidadRepository.AgregarAsync(new UnidadMedida(unidadId, "CAJ", "Caja"));
        var useCase = CrearUseCase(presentacionRepository, productoRepository, unidadRepository, empresaId);

        var detalle = await useCase.EjecutarAsync(new CrearProductoPresentacionRequest(
            productoId,
            unidadId,
            12m,
            EsUnidadBase: false,
            100m,
            " 7750000000104 "));

        Assert.Equal(empresaId, detalle.Presentacion.EmpresaId);
        Assert.Equal(productoId, detalle.Presentacion.ProductoId);
        Assert.Equal(unidadId, detalle.Presentacion.UnidadMedidaId);
        Assert.Equal("CAJ", detalle.UnidadMedida.Codigo);
        Assert.Equal("7750000000104", detalle.Presentacion.CodigoBarras);
        Assert.Same(detalle.Presentacion, presentacionRepository.Presentaciones.Single());
    }

    [Fact]
    public async Task Crear_presentacion_falla_si_producto_no_pertenece_a_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var presentacionRepository = new ProductoPresentacionRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var unidadRepository = new UnidadMedidaRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, Guid.NewGuid(), "Producto", 10m));
        await unidadRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad"));
        var useCase = CrearUseCase(presentacionRepository, productoRepository, unidadRepository, empresaId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearProductoPresentacionRequest(productoId, unidadId, 1m, true, 10m)));

        Assert.Contains("producto", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(presentacionRepository.Presentaciones);
    }

    [Fact]
    public async Task Crear_presentacion_falla_si_unidad_no_existe_o_inactiva()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var presentacionRepository = new ProductoPresentacionRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var unidadRepository = new UnidadMedidaRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Producto", 10m));
        await unidadRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad", activa: false));
        var useCase = CrearUseCase(presentacionRepository, productoRepository, unidadRepository, empresaId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearProductoPresentacionRequest(productoId, unidadId, 1m, true, 10m)));

        Assert.Contains("unidad", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(presentacionRepository.Presentaciones);
    }

    [Fact]
    public async Task Crear_presentacion_falla_si_codigo_barras_esta_duplicado_en_empresa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var presentacionRepository = new ProductoPresentacionRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var unidadRepository = new UnidadMedidaRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Producto", 10m));
        await unidadRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad"));
        await presentacionRepository.AgregarAsync(new ProductoPresentacion(
            Guid.NewGuid(),
            empresaId,
            productoId,
            unidadId,
            1m,
            true,
            10m,
            "7750000000104"));
        var useCase = CrearUseCase(presentacionRepository, productoRepository, unidadRepository, empresaId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearProductoPresentacionRequest(
                productoId,
                unidadId,
                2m,
                false,
                20m,
                " 7750000000104 ")));

        Assert.Contains("codigo de barras", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(presentacionRepository.Presentaciones);
    }

    [Fact]
    public async Task Listar_presentaciones_filtra_producto_y_activas()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var presentacionRepository = new ProductoPresentacionRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var unidadRepository = new UnidadMedidaRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Producto", 10m));
        await unidadRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad"));
        await presentacionRepository.AgregarAsync(new ProductoPresentacion(
            Guid.NewGuid(),
            empresaId,
            productoId,
            unidadId,
            1m,
            true,
            10m));
        await presentacionRepository.AgregarAsync(new ProductoPresentacion(
            Guid.NewGuid(),
            empresaId,
            productoId,
            unidadId,
            2m,
            false,
            20m,
            activa: false));
        var useCase = new ListarProductoPresentacionesUseCase(
            presentacionRepository,
            productoRepository,
            unidadRepository,
            new EmpresaActivaContextFake(empresaId));

        var presentaciones = await useCase.EjecutarAsync(productoId);

        Assert.NotNull(presentaciones);
        Assert.Single(presentaciones);
    }

    private static CrearProductoPresentacionUseCase CrearUseCase(
        ProductoPresentacionRepositoryFake presentacionRepository,
        ProductoRepositoryFake productoRepository,
        UnidadMedidaRepositoryFake unidadRepository,
        Guid empresaId)
    {
        return new CrearProductoPresentacionUseCase(
            presentacionRepository,
            productoRepository,
            unidadRepository,
            new EmpresaActivaContextFake(empresaId));
    }

    private sealed class ProductoPresentacionRepositoryFake : IProductoPresentacionRepository
    {
        public List<ProductoPresentacion> Presentaciones { get; } = new();

        public Task AgregarAsync(
            ProductoPresentacion presentacion,
            CancellationToken cancellationToken = default)
        {
            Presentaciones.Add(presentacion);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoPresentacion>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoPresentacion>>(
                Presentaciones.Where(presentacion =>
                    presentacion.EmpresaId == empresaId &&
                    presentacion.ProductoId == productoId).ToArray());
        }

        public Task<ProductoPresentacion?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Presentaciones.SingleOrDefault(presentacion =>
                presentacion.EmpresaId == empresaId &&
                presentacion.Id == id));
        }

        public Task<bool> ExisteCodigoBarrasAsync(
            Guid empresaId,
            string codigoBarras,
            CancellationToken cancellationToken = default)
        {
            var codigoNormalizado = codigoBarras.Trim();

            return Task.FromResult(Presentaciones.Any(presentacion =>
                presentacion.EmpresaId == empresaId &&
                presentacion.CodigoBarras == codigoNormalizado));
        }
    }

    private sealed class UnidadMedidaRepositoryFake : IUnidadMedidaRepository
    {
        public List<UnidadMedida> Unidades { get; } = new();

        public Task AgregarAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken = default)
        {
            Unidades.Add(unidadMedida);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<UnidadMedida>> ListarAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<UnidadMedida>>(Unidades.ToArray());
        }

        public Task<UnidadMedida?> ObtenerPorIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Unidades.SingleOrDefault(unidad => unidad.Id == id));
        }

        public Task<UnidadMedida?> ObtenerPorCodigoAsync(
            string codigo,
            CancellationToken cancellationToken = default)
        {
            var codigoNormalizado = codigo.Trim().ToUpperInvariant();

            return Task.FromResult(Unidades.SingleOrDefault(unidad => unidad.Codigo == codigoNormalizado));
        }
    }

    private sealed class ProductoRepositoryFake : IProductoRepository
    {
        public List<Producto> Productos { get; } = new();

        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            Productos.Add(producto);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Producto>>(
                Productos.Where(producto => producto.EmpresaId == empresaId).ToArray());
        }

        public Task<Producto?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Productos.SingleOrDefault(producto =>
                producto.EmpresaId == empresaId &&
                producto.Id == id));
        }

        public Task ActualizarAsync(
            Producto producto,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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
