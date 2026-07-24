using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationProductoVarianteTests
{
    [Fact]
    public async Task Crear_variante_use_case_asigna_empresa_id_desde_contexto_activo()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new ProductoVarianteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        var empresaActiva = new EmpresaActivaContextFake(empresaId);
        var useCase = new CrearProductoVarianteUseCase(repository, productoRepository, empresaActiva);
        var request = new CrearProductoVarianteRequest(
            productoId,
            " M ",
            " Azul ",
            " SKU-AZ-M ",
            " 7750000000104 ");

        var variante = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, variante.Id);
        Assert.Equal(empresaId, variante.EmpresaId);
        Assert.Equal(productoId, variante.ProductoId);
        Assert.Equal("M", variante.Talla);
        Assert.Equal("Azul", variante.Color);
        Assert.Equal("SKU-AZ-M", variante.CodigoSku);
        Assert.Equal("7750000000104", variante.CodigoBarras);
        Assert.Same(variante, repository.Variantes.Single());
    }

    [Fact]
    public async Task Crear_variante_use_case_falla_si_no_hay_empresa_activa()
    {
        var repository = new ProductoVarianteRepositoryFake();
        var useCase = new CrearProductoVarianteUseCase(
            repository,
            new ProductoRepositoryFake(),
            new EmpresaActivaContextFake());
        var request = new CrearProductoVarianteRequest(
            Guid.NewGuid(),
            Talla: "M");

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Variantes);
    }

    [Fact]
    public async Task Crear_variante_use_case_propaga_validaciones_de_dominio()
    {
        var repository = new ProductoVarianteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var productoId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        var useCase = new CrearProductoVarianteUseCase(
            repository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));
        var request = new CrearProductoVarianteRequest(
            productoId);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Variantes);
    }

    [Fact]
    public async Task Crear_variante_use_case_falla_si_producto_no_pertenece_a_empresa_activa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new ProductoVarianteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaBId, "Polo", 59m));
        var useCase = new CrearProductoVarianteUseCase(
            repository,
            productoRepository,
            new EmpresaActivaContextFake(empresaAId));
        var request = new CrearProductoVarianteRequest(
            productoId,
            Talla: "M");

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Variantes);
    }

    [Fact]
    public async Task Crear_variante_use_case_rechaza_sku_duplicado_por_empresa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new ProductoVarianteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await repository.AgregarAsync(new ProductoVariante(
            Guid.NewGuid(),
            empresaId,
            productoId,
            codigoSku: "SKU-001"));
        var useCase = new CrearProductoVarianteUseCase(
            repository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));
        var request = new CrearProductoVarianteRequest(
            productoId,
            CodigoSku: " SKU-001 ");

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Single(repository.Variantes);
    }

    [Fact]
    public async Task Listar_variantes_use_case_lista_solo_producto_y_empresa_activa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoAId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var repository = new ProductoVarianteRepositoryFake();
        var varianteEmpresaA = new ProductoVariante(
            Guid.NewGuid(),
            empresaAId,
            productoAId,
            talla: "M");
        var varianteOtroProducto = new ProductoVariante(
            Guid.NewGuid(),
            empresaAId,
            otroProductoId,
            talla: "L");
        var varianteEmpresaB = new ProductoVariante(
            Guid.NewGuid(),
            empresaBId,
            productoAId,
            talla: "S");
        await repository.AgregarAsync(varianteEmpresaA);
        await repository.AgregarAsync(varianteOtroProducto);
        await repository.AgregarAsync(varianteEmpresaB);
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoAId, empresaAId, "Polo", 59m));
        var useCase = new ListarProductoVariantesUseCase(
            repository,
            productoRepository,
            new EmpresaActivaContextFake(empresaAId));

        var variantes = await useCase.EjecutarAsync(productoAId);

        Assert.NotNull(variantes);
        Assert.Same(varianteEmpresaA, Assert.Single(variantes));
    }

    [Fact]
    public async Task Listar_variantes_use_case_falla_si_no_hay_empresa_activa()
    {
        var repository = new ProductoVarianteRepositoryFake();
        var useCase = new ListarProductoVariantesUseCase(
            repository,
            new ProductoRepositoryFake(),
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Activar_y_desactivar_variante_validan_producto_y_empresa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var repository = new ProductoVarianteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await repository.AgregarAsync(new ProductoVariante(
            varianteId,
            empresaId,
            productoId,
            talla: "M"));
        var desactivarUseCase = new DesactivarProductoVarianteUseCase(
            repository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));
        var activarUseCase = new ActivarProductoVarianteUseCase(
            repository,
            productoRepository,
            new EmpresaActivaContextFake(empresaId));

        var desactivada = await desactivarUseCase.EjecutarAsync(productoId, varianteId);
        var activada = await activarUseCase.EjecutarAsync(productoId, varianteId);

        Assert.NotNull(desactivada);
        Assert.NotNull(activada);
        Assert.True(activada.Activo);
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

        public Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ProductoVarianteRepositoryFake : IProductoVarianteRepository
    {
        public List<ProductoVariante> Variantes { get; } = new();

        public Task AgregarAsync(
            ProductoVariante variante,
            CancellationToken cancellationToken = default)
        {
            Variantes.Add(variante);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ProductoVariante> variantes = Variantes
                .Where(variante => variante.EmpresaId == empresaId && variante.ProductoId == productoId)
                .ToArray();

            return Task.FromResult(variantes);
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ProductoVariante> variantes = Variantes
                .Where(variante => variante.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(variantes);
        }

        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var variante = Variantes.SingleOrDefault(variante =>
                variante.EmpresaId == empresaId && variante.Id == id);

            return Task.FromResult(variante);
        }

        public Task ActualizarAsync(
            ProductoVariante variante,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteSkuAsync(
            Guid empresaId,
            string codigoSku,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.Any(variante =>
                variante.EmpresaId == empresaId &&
                variante.CodigoSku == codigoSku.Trim()));
        }

        public Task<bool> ExisteCodigoBarrasAsync(
            Guid empresaId,
            string codigoBarras,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.Any(variante =>
                variante.EmpresaId == empresaId &&
                variante.CodigoBarras == codigoBarras.Trim()));
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
