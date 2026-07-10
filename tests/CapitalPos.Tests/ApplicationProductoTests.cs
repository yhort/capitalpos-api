using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationProductoTests
{
    [Fact]
    public async Task Crear_producto_use_case_asigna_empresa_id_desde_contexto_activo()
    {
        var empresaId = Guid.NewGuid();
        var repository = new ProductoRepositoryFake();
        var empresaActiva = new EmpresaActivaContextFake(empresaId);
        var useCase = new CrearProductoUseCase(repository, empresaActiva);
        var request = new CrearProductoRequest(
            " Cafe Americano ",
            8.50m,
            " SKU-001 ",
            " 7750000000012 ",
            3.25m);

        var producto = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, producto.Id);
        Assert.Equal(empresaId, producto.EmpresaId);
        Assert.Equal("Cafe Americano", producto.Nombre);
        Assert.Equal("SKU-001", producto.CodigoSku);
        Assert.Equal("7750000000012", producto.CodigoBarras);
        Assert.Equal(8.50m, producto.PrecioVenta);
        Assert.Equal(3.25m, producto.Costo);
        Assert.Same(producto, repository.Productos.Single());
    }

    [Fact]
    public async Task Crear_producto_use_case_falla_si_no_hay_empresa_activa()
    {
        var repository = new ProductoRepositoryFake();
        var empresaActiva = new EmpresaActivaContextFake();
        var useCase = new CrearProductoUseCase(repository, empresaActiva);
        var request = new CrearProductoRequest("Cafe Americano", 8.50m);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Productos);
    }

    [Fact]
    public async Task Crear_producto_use_case_propaga_validaciones_de_dominio()
    {
        var repository = new ProductoRepositoryFake();
        var empresaActiva = new EmpresaActivaContextFake(Guid.NewGuid());
        var useCase = new CrearProductoUseCase(repository, empresaActiva);
        var request = new CrearProductoRequest(" ", 8.50m);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Productos);
    }

    [Fact]
    public async Task Listar_productos_use_case_lista_solo_empresa_activa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var repository = new ProductoRepositoryFake();
        var productoEmpresaA = new Producto(
            Guid.NewGuid(),
            empresaAId,
            "Cafe Americano",
            8.50m);
        var productoEmpresaB = new Producto(
            Guid.NewGuid(),
            empresaBId,
            "Te Helado",
            7.00m);
        await repository.AgregarAsync(productoEmpresaA);
        await repository.AgregarAsync(productoEmpresaB);
        var useCase = new ListarProductosUseCase(
            repository,
            new EmpresaActivaContextFake(empresaAId));

        var productos = await useCase.EjecutarAsync();

        Assert.Same(productoEmpresaA, Assert.Single(productos));
    }

    [Fact]
    public async Task Listar_productos_use_case_falla_si_no_hay_empresa_activa()
    {
        var repository = new ProductoRepositoryFake();
        var useCase = new ListarProductosUseCase(
            repository,
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync());
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
            IReadOnlyCollection<Producto> productos = Productos
                .Where(producto => producto.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(productos);
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
