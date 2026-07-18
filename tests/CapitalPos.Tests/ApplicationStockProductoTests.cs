using CapitalPos.Application.Inventario;
using CapitalPos.Application.Persistence;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationStockProductoTests
{
    private static readonly Guid SedeIdPrueba = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtraSedeIdPrueba = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

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
            CrearSedeRepository(empresaId),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaId));

        var stock = await useCase.EjecutarAsync(new AjustarStockProductoRequest(
            SedeIdPrueba,
            productoId,
            null,
            12m));

        Assert.Equal(empresaId, stock.EmpresaId);
        Assert.Equal(SedeIdPrueba, stock.SedeId);
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
            SedeIdPrueba,
            productoId,
            null,
            5m);
        await repository.GuardarAsync(existente);
        var useCase = new AjustarStockProductoUseCase(
            repository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            CrearSedeRepository(empresaId),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaId));

        var stock = await useCase.EjecutarAsync(new AjustarStockProductoRequest(
            SedeIdPrueba,
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
            new SedeRepositoryFake(),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new AjustarStockProductoRequest(
                SedeIdPrueba,
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
            CrearSedeRepository(empresaActivaId),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaActivaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new AjustarStockProductoRequest(
                SedeIdPrueba,
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
            CrearSedeRepository(empresaId),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new AjustarStockProductoRequest(
                SedeIdPrueba,
                productoId,
                varianteId,
                10m)));

        Assert.Contains("variante", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.Stocks);
    }

    [Fact]
    public async Task Ajustar_stock_falla_si_sede_no_pertenece_a_empresa_activa()
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
            new SedeRepositoryFake(),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new AjustarStockProductoRequest(
                SedeIdPrueba,
                productoId,
                null,
                10m)));

        Assert.Contains("sede", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.Stocks);
    }

    [Fact]
    public async Task Ajustar_stock_en_sede_a_no_afecta_sede_b()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new StockProductoRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(CrearProducto(empresaId, productoId));
        var stockA = new StockProducto(Guid.NewGuid(), empresaId, SedeIdPrueba, productoId, null, 10m);
        var stockB = new StockProducto(Guid.NewGuid(), empresaId, OtraSedeIdPrueba, productoId, null, 20m);
        await repository.GuardarAsync(stockA);
        await repository.GuardarAsync(stockB);
        var useCase = new AjustarStockProductoUseCase(
            repository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            CrearSedeRepository(empresaId, OtraSedeIdPrueba),
            new UnitOfWorkFake(),
            new EmpresaActivaContextFake(empresaId));

        var stock = await useCase.EjecutarAsync(new AjustarStockProductoRequest(
            OtraSedeIdPrueba,
            productoId,
            null,
            30m));

        Assert.Same(stockB, stock);
        Assert.Equal(10m, stockA.CantidadDisponible);
        Assert.Equal(30m, stockB.CantidadDisponible);
    }

    [Fact]
    public async Task Obtener_stock_usa_empresa_activa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new StockProductoRepositoryFake();
        var stockA = new StockProducto(Guid.NewGuid(), empresaAId, SedeIdPrueba, productoId, null, 10m);
        var stockB = new StockProducto(Guid.NewGuid(), empresaBId, SedeIdPrueba, productoId, null, 20m);
        await repository.GuardarAsync(stockA);
        await repository.GuardarAsync(stockB);
        var useCase = new ObtenerStockProductoUseCase(
            repository,
            CrearSedeRepository(empresaAId),
            new EmpresaActivaContextFake(empresaAId));

        var stock = await useCase.EjecutarAsync(SedeIdPrueba, productoId);

        Assert.Same(stockA, stock);
        Assert.NotSame(stockB, stock);
    }

    [Fact]
    public async Task Obtener_stock_falla_si_no_hay_empresa_activa()
    {
        var useCase = new ObtenerStockProductoUseCase(
            new StockProductoRepositoryFake(),
            new SedeRepositoryFake(),
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(SedeIdPrueba, Guid.NewGuid()));
    }

    private static SedeRepositoryFake CrearSedeRepository(Guid empresaId, Guid? sedeId = null)
    {
        var repository = new SedeRepositoryFake();
        repository.Sedes.Add(new Sede(
            sedeId ?? SedeIdPrueba,
            empresaId,
            "Sede prueba",
            TipoSede.TIENDA));

        return repository;
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

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>(
                Variantes.Where(variante => variante.EmpresaId == empresaId).ToArray());
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

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class SedeRepositoryFake : ISedeRepository
    {
        public List<Sede> Sedes { get; } = new();

        public Task AgregarAsync(Sede sede, CancellationToken cancellationToken = default)
        {
            Sedes.Add(sede);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Sede>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Sede>>(
                Sedes.Where(sede => sede.EmpresaId == empresaId).ToArray());
        }

        public Task<Sede?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sedes.FirstOrDefault(sede =>
                sede.EmpresaId == empresaId &&
                sede.Id == id));
        }
    }

    private sealed class StockProductoRepositoryFake : IStockProductoRepository
    {
        public List<StockProducto> Stocks { get; } = new();

        public Task<StockProducto?> ObtenerPorProductoAsync(
            Guid empresaId,
            Guid sedeId,
            Guid productoId,
            Guid? productoVarianteId = null,
            CancellationToken cancellationToken = default)
        {
            var stock = Stocks.SingleOrDefault(stock =>
                stock.EmpresaId == empresaId &&
                stock.SedeId == sedeId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId == productoVarianteId);

            return Task.FromResult(stock);
        }

        public Task<IReadOnlyCollection<StockProducto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<StockProducto>>(
                Stocks.Where(stock => stock.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<StockProducto>> ListarPorSedeAsync(
            Guid empresaId,
            Guid sedeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<StockProducto>>(
                Stocks.Where(stock => stock.EmpresaId == empresaId && stock.SedeId == sedeId).ToArray());
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
