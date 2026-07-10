using CapitalPos.Application.Clientes;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationVentaTests
{
    [Fact]
    public async Task Crear_venta_use_case_asigna_empresa_id_desde_contexto_y_calcula_totales()
    {
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        var clienteRepository = new ClienteRepositoryFake();
        await clienteRepository.AgregarAsync(new Cliente(clienteId, empresaId, "DNI", "12345678", "Juan Perez"));
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await varianteRepository.AgregarAsync(new ProductoVariante(varianteId, empresaId, productoId, talla: "M"));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            varianteRepository,
            clienteRepository,
            empresaId);
        var fecha = DateTimeOffset.UtcNow;
        var request = new CrearVentaRequest(
            fecha,
            clienteId,
            [
                new CrearVentaDetalleRequest(productoId, varianteId, 2m, 50m, 18m, 118m),
                new CrearVentaDetalleRequest(productoId, null, 1m, 20m, 3.60m, 23.60m)
            ]);

        var venta = await useCase.EjecutarAsync(request);

        Assert.Equal(empresaId, venta.EmpresaId);
        Assert.Equal(clienteId, venta.ClienteId);
        Assert.Equal(fecha, venta.Fecha);
        Assert.Equal(120m, venta.Subtotal);
        Assert.Equal(21.60m, venta.Igv);
        Assert.Equal(141.60m, venta.Total);
        Assert.Equal(2, venta.Detalles.Count);
        Assert.All(venta.Detalles, detalle =>
        {
            Assert.Equal(empresaId, detalle.EmpresaId);
            Assert.Equal(venta.Id, detalle.VentaId);
        });
        Assert.Same(venta, ventaRepository.Ventas.Single());
    }

    [Fact]
    public async Task Crear_venta_use_case_falla_si_no_hay_empresa_activa()
    {
        var useCase = CrearUseCase(
            new VentaRepositoryFake(),
            new ProductoRepositoryFake(),
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake());
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(Guid.NewGuid(), null, 1m, 10m, 0m, 10m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task Crear_venta_use_case_rechaza_producto_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaBId, "Polo", 59m));
        var useCase = CrearUseCase(
            new VentaRepositoryFake(),
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            empresaAId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(productoId, null, 1m, 10m, 0m, 10m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task Crear_venta_use_case_rechaza_cliente_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var clienteRepository = new ClienteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await clienteRepository.AgregarAsync(new Cliente(clienteId, empresaBId, "DNI", "12345678", "Juan Perez"));
        await productoRepository.AgregarAsync(new Producto(productoId, empresaAId, "Polo", 59m));
        var useCase = CrearUseCase(
            new VentaRepositoryFake(),
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            clienteRepository,
            empresaAId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            clienteId,
            [new CrearVentaDetalleRequest(productoId, null, 1m, 10m, 0m, 10m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task Crear_venta_use_case_rechaza_variante_de_otro_producto()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await varianteRepository.AgregarAsync(new ProductoVariante(varianteId, empresaId, otroProductoId, talla: "M"));
        var useCase = CrearUseCase(
            new VentaRepositoryFake(),
            productoRepository,
            varianteRepository,
            new ClienteRepositoryFake(),
            empresaId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(productoId, varianteId, 1m, 10m, 0m, 10m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
    }

    private static CrearVentaUseCase CrearUseCase(
        VentaRepositoryFake ventaRepository,
        ProductoRepositoryFake productoRepository,
        ProductoVarianteRepositoryFake varianteRepository,
        ClienteRepositoryFake clienteRepository,
        Guid? empresaId = null)
    {
        return new CrearVentaUseCase(
            ventaRepository,
            productoRepository,
            varianteRepository,
            clienteRepository,
            empresaId.HasValue
                ? new EmpresaActivaContextFake(empresaId.Value)
                : new EmpresaActivaContextFake());
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
            IReadOnlyCollection<Venta> ventas = Ventas
                .Where(venta => venta.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(ventas);
        }

        public Task<Venta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var venta = Ventas.SingleOrDefault(venta =>
                venta.EmpresaId == empresaId && venta.Id == id);

            return Task.FromResult(venta);
        }
    }

    private sealed class ProductoRepositoryFake : IProductoRepository
    {
        private readonly List<Producto> _productos = new();

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
            var producto = _productos.SingleOrDefault(producto =>
                producto.EmpresaId == empresaId && producto.Id == id);

            return Task.FromResult(producto);
        }

        public Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ProductoVarianteRepositoryFake : IProductoVarianteRepository
    {
        private readonly List<ProductoVariante> _variantes = new();

        public Task AgregarAsync(ProductoVariante variante, CancellationToken cancellationToken = default)
        {
            _variantes.Add(variante);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ProductoVariante> variantes = _variantes
                .Where(variante => variante.EmpresaId == empresaId && variante.ProductoId == productoId)
                .ToArray();

            return Task.FromResult(variantes);
        }

        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var variante = _variantes.SingleOrDefault(variante =>
                variante.EmpresaId == empresaId && variante.Id == id);

            return Task.FromResult(variante);
        }
    }

    private sealed class ClienteRepositoryFake : IClienteRepository
    {
        private readonly List<Cliente> _clientes = new();

        public Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            _clientes.Add(cliente);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Cliente>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Cliente> clientes = _clientes
                .Where(cliente => cliente.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(clientes);
        }

        public Task<Cliente?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var cliente = _clientes.SingleOrDefault(cliente =>
                cliente.EmpresaId == empresaId && cliente.Id == id);

            return Task.FromResult(cliente);
        }

        public Task ActualizarAsync(Cliente cliente, CancellationToken cancellationToken = default)
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
