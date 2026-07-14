using CapitalPos.Application.Inventario;
using CapitalPos.Application.Persistence;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationStockProductoTests
{
    [Fact]
    public async Task Ajustar_stock_crea_stock_para_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new StockProductoRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(CrearProducto(empresaId, productoId));
        var useCase = new AjustarStockProductoUseCase(
            repository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaId));

        var stock = await useCase.EjecutarAsync(new AjustarStockProductoRequest(
            productoId,
            null,
            12m));

        Assert.Equal(empresaId, stock.EmpresaId);
        Assert.Equal(productoId, stock.ProductoId);
        Assert.Equal(12m, stock.CantidadDisponible);
        Assert.Same(stock, repository.Stocks.Single());
    }

    [Fact]
    public async Task Ajustar_stock_actualiza_stock_existente_de_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new StockProductoRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(CrearProducto(empresaId, productoId));
        var existente = new StockProducto(
            Guid.NewGuid(),
            empresaId,
            productoId,
            null,
            5m);
        await repository.GuardarAsync(existente);
        var useCase = new AjustarStockProductoUseCase(
            repository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaId));

        var stock = await useCase.EjecutarAsync(new AjustarStockProductoRequest(
            productoId,
            null,
            15m));

        Assert.Same(existente, stock);
        Assert.Equal(15m, stock.CantidadDisponible);
        Assert.Single(repository.Stocks);
    }

    [Fact]
    public async Task Ajustar_stock_falla_si_no_hay_empresa_activa()
    {
        var repository = new StockProductoRepositoryFake();
        var useCase = new AjustarStockProductoUseCase(
            repository,
            new ProductoRepositoryFake(),
            new ProductoVarianteRepositoryFake(),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new AjustarStockProductoRequest(
                Guid.NewGuid(),
                null,
                10m)));
        Assert.Empty(repository.Stocks);
    }

    [Fact]
    public async Task Ajustar_stock_falla_si_producto_pertenece_a_otra_empresa()
    {
        var empresaActivaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new StockProductoRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        var useCase = new AjustarStockProductoUseCase(
            repository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaActivaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new AjustarStockProductoRequest(
                productoId,
                null,
                10m)));

        Assert.Contains("producto", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.Stocks);
    }

    [Fact]
    public async Task Ajustar_stock_falla_si_variante_no_pertenece_al_producto_activo()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var repository = new StockProductoRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        await productoRepository.AgregarAsync(CrearProducto(empresaId, productoId));
        await varianteRepository.AgregarAsync(new ProductoVariante(
            varianteId,
            empresaId,
            otroProductoId,
            talla: "M"));
        var useCase = new AjustarStockProductoUseCase(
            repository,
            productoRepository,
            varianteRepository,
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new AjustarStockProductoRequest(
                productoId,
                varianteId,
                10m)));

        Assert.Contains("variante", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.Stocks);
    }

    [Fact]
    public async Task Obtener_stock_usa_empresa_activa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new StockProductoRepositoryFake();
        var stockA = new StockProducto(Guid.NewGuid(), empresaAId, productoId, null, 10m);
        var stockB = new StockProducto(Guid.NewGuid(), empresaBId, productoId, null, 20m);
        await repository.GuardarAsync(stockA);
        await repository.GuardarAsync(stockB);
        var useCase = new ObtenerStockProductoUseCase(
            repository,
            new EmpresaActivaContextFake(empresaAId));

        var stock = await useCase.EjecutarAsync(productoId);

        Assert.Same(stockA, stock);
        Assert.NotSame(stockB, stock);
    }

    [Fact]
    public async Task Obtener_stock_falla_si_no_hay_empresa_activa()
    {
        var useCase = new ObtenerStockProductoUseCase(
            new StockProductoRepositoryFake(),
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(Guid.NewGuid()));
    }

    private static Producto CrearProducto(Guid empresaId, Guid productoId)
    {
        return new Producto(productoId, empresaId, "Producto prueba", 10m);
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
            return Task.FromResult(Productos.FirstOrDefault(producto =>
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

        public Task AgregarAsync(ProductoVariante variante, CancellationToken cancellationToken = default)
        {
            Variantes.Add(variante);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>(
                Variantes.Where(variante =>
                    variante.EmpresaId == empresaId &&
                    variante.ProductoId == productoId).ToArray());
        }

        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.FirstOrDefault(variante =>
                variante.EmpresaId == empresaId &&
                variante.Id == id));
        }
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class StockProductoRepositoryFake : IStockProductoRepository
    {
        public List<StockProducto> Stocks { get; } = new();

        public Task<StockProducto?> ObtenerPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            Guid? productoVarianteId = null,
            CancellationToken cancellationToken = default)
        {
            var stock = Stocks.SingleOrDefault(stock =>
                stock.EmpresaId == empresaId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId == productoVarianteId);

            return Task.FromResult(stock);
        }

        public Task GuardarAsync(
            StockProducto stock,
            CancellationToken cancellationToken = default)
        {
            var index = Stocks.FindIndex(actual => actual.Id == stock.Id);
            if (index >= 0)
            {
                Stocks[index] = stock;
            }
            else
            {
                Stocks.Add(stock);
            }

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
