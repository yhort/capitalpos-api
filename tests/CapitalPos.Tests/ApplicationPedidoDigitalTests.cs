using CapitalPos.Application.Clientes;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Pedidos;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationPedidoDigitalTests
{
    private static readonly Guid SedeIdPrueba = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PuntoVentaIdPrueba = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Crear_pedido_digital_con_producto_base_reserva_stock_libre_y_calcula_totales()
    {
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var pedidoRepository = new PedidoDigitalRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var clienteRepository = new ClienteRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        var movimientos = new MovimientoInventarioRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo subasta", 59m));
        await clienteRepository.AgregarAsync(new Cliente(clienteId, empresaId, "DNI", "12345678", "Cliente Digital"));
        var stock = new StockProducto(Guid.NewGuid(), empresaId, SedeIdPrueba, productoId, null, 10m);
        await stockRepository.GuardarAsync(stock);
        var useCase = CrearUseCase(
            pedidoRepository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ProductoPresentacionRepositoryFake(),
            clienteRepository,
            stockRepository,
            empresaId,
            movimientos);

        var pedido = await useCase.EjecutarAsync(new CrearPedidoDigitalRequest(
            clienteId,
            SedeIdPrueba,
            PuntoVentaIdPrueba,
            "FACEBOOK_SUBASTA",
            DateTimeOffset.UtcNow,
            [new CrearPedidoDigitalDetalleRequest(productoId, null, null, null, 2m, 59m)],
            "FB-001",
            "Pedido en vivo"));

        Assert.Equal(empresaId, pedido.EmpresaId);
        Assert.Equal(clienteId, pedido.ClienteId);
        Assert.Equal(SedeIdPrueba, pedido.SedeId);
        Assert.Equal(PuntoVentaIdPrueba, pedido.PuntoVentaId);
        Assert.Equal(CanalPedidoDigital.FACEBOOK_SUBASTA, pedido.CanalPedido);
        Assert.Equal(EstadoPedidoDigital.PendientePago, pedido.Estado);
        Assert.Equal(118m, pedido.Total);
        Assert.Equal(100m, pedido.Subtotal);
        Assert.Equal(18m, pedido.Igv);
        Assert.Equal("FB-001", pedido.ReferenciaExterna);
        Assert.Equal("Pedido en vivo", pedido.Observacion);
        Assert.Single(pedido.HistorialEstados);
        Assert.Same(pedido, pedidoRepository.Pedidos.Single());
        Assert.Equal(10m, stock.CantidadDisponible);
        Assert.Equal(2m, stock.CantidadReservada);
        Assert.Equal(8m, stock.CantidadLibre);
        var movimiento = Assert.Single(movimientos.Movimientos);
        Assert.Equal(TipoMovimientoInventario.RESERVA, movimiento.TipoMovimiento);
        Assert.Equal(2m, movimiento.Cantidad);
        Assert.Equal(10m, movimiento.StockAnterior);
        Assert.Equal(8m, movimiento.StockPosterior);
        Assert.Equal("PEDIDO_DIGITAL", movimiento.ReferenciaTipo);
        Assert.Equal(pedido.Id, movimiento.ReferenciaId);
    }

    [Fact]
    public async Task Crear_pedido_digital_con_variante_valida_producto_y_reserva_stock()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await varianteRepository.AgregarAsync(new ProductoVariante(varianteId, empresaId, productoId, talla: "M"));
        var stock = new StockProducto(Guid.NewGuid(), empresaId, SedeIdPrueba, productoId, varianteId, 5m);
        await stockRepository.GuardarAsync(stock);
        var useCase = CrearUseCase(
            new PedidoDigitalRepositoryFake(),
            productoRepository,
            varianteRepository,
            new ProductoPresentacionRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);

        var pedido = await useCase.EjecutarAsync(new CrearPedidoDigitalRequest(
            null,
            SedeIdPrueba,
            null,
            "WHATSAPP",
            DateTimeOffset.UtcNow,
            [new CrearPedidoDigitalDetalleRequest(productoId, varianteId, null, "Polo M", 1m, 59m)]));

        var detalle = Assert.Single(pedido.Detalles);
        Assert.Equal(varianteId, detalle.ProductoVarianteId);
        Assert.Null(detalle.ProductoPresentacionId);
        Assert.Equal(1m, detalle.CantidadBase);
        Assert.Equal(1m, stock.CantidadReservada);
        Assert.Equal(4m, stock.CantidadLibre);
    }

    [Fact]
    public async Task Crear_pedido_digital_con_presentacion_reserva_cantidad_base()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var presentacionId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        var presentacionRepository = new ProductoPresentacionRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await presentacionRepository.AgregarAsync(new ProductoPresentacion(
            presentacionId,
            empresaId,
            productoId,
            Guid.NewGuid(),
            12m,
            false,
            500m));
        var stock = new StockProducto(Guid.NewGuid(), empresaId, SedeIdPrueba, productoId, null, 30m);
        await stockRepository.GuardarAsync(stock);
        var useCase = CrearUseCase(
            new PedidoDigitalRepositoryFake(),
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            presentacionRepository,
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);

        var pedido = await useCase.EjecutarAsync(new CrearPedidoDigitalRequest(
            null,
            SedeIdPrueba,
            null,
            "INSTAGRAM",
            DateTimeOffset.UtcNow,
            [new CrearPedidoDigitalDetalleRequest(productoId, null, presentacionId, null, 2m, 450m)]));

        var detalle = Assert.Single(pedido.Detalles);
        Assert.Equal(presentacionId, detalle.ProductoPresentacionId);
        Assert.Equal(12m, detalle.FactorConversionAplicado);
        Assert.Equal(24m, detalle.CantidadBase);
        Assert.Equal(900m, detalle.Total);
        Assert.Equal(24m, stock.CantidadReservada);
        Assert.Equal(6m, stock.CantidadLibre);
    }

    [Fact]
    public async Task Crear_pedido_digital_falla_si_stock_libre_es_insuficiente()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await stockRepository.GuardarAsync(
            new StockProducto(Guid.NewGuid(), empresaId, SedeIdPrueba, productoId, null, 1m));
        var useCase = CrearUseCase(
            new PedidoDigitalRepositoryFake(),
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ProductoPresentacionRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(CrearRequest(productoId, "WHATSAPP", cantidad: 2m)));

        Assert.Contains("Stock libre insuficiente", ex.Message);
    }

    [Fact]
    public async Task Crear_pedido_digital_valida_empresa_activa_y_canal()
    {
        var useCaseSinEmpresa = CrearUseCase(
            new PedidoDigitalRepositoryFake(),
            new ProductoRepositoryFake(),
            new ProductoVarianteRepositoryFake(),
            new ProductoPresentacionRepositoryFake(),
            new ClienteRepositoryFake(),
            new StockProductoRepositoryFake(),
            null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCaseSinEmpresa.EjecutarAsync(CrearRequest(Guid.NewGuid(), "WHATSAPP")));

        var useCaseCanalInvalido = CrearUseCase(
            new PedidoDigitalRepositoryFake(),
            new ProductoRepositoryFake(),
            new ProductoVarianteRepositoryFake(),
            new ProductoPresentacionRepositoryFake(),
            new ClienteRepositoryFake(),
            new StockProductoRepositoryFake(),
            Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCaseCanalInvalido.EjecutarAsync(CrearRequest(Guid.NewGuid(), "SUBASTA_DESCONOCIDA")));
    }

    [Fact]
    public async Task Crear_pedido_digital_falla_si_producto_variante_presentacion_o_cliente_son_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var presentacionId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        var presentacionRepository = new ProductoPresentacionRepositoryFake();
        var clienteRepository = new ClienteRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaBId, "Polo", 59m));
        await varianteRepository.AgregarAsync(new ProductoVariante(varianteId, empresaBId, productoId, talla: "M"));
        await presentacionRepository.AgregarAsync(new ProductoPresentacion(
            presentacionId,
            empresaBId,
            productoId,
            Guid.NewGuid(),
            12m,
            false,
            500m));
        await clienteRepository.AgregarAsync(new Cliente(clienteId, empresaBId, "DNI", "12345678", "Cliente B"));
        var useCase = CrearUseCase(
            new PedidoDigitalRepositoryFake(),
            productoRepository,
            varianteRepository,
            presentacionRepository,
            clienteRepository,
            new StockProductoRepositoryFake(),
            empresaAId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(CrearRequest(productoId, "WHATSAPP")));

        await productoRepository.AgregarAsync(new Producto(productoId, empresaAId, "Polo", 59m));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearPedidoDigitalRequest(
                null,
                SedeIdPrueba,
                null,
                "WHATSAPP",
                DateTimeOffset.UtcNow,
                [new CrearPedidoDigitalDetalleRequest(productoId, varianteId, null, null, 1m, 59m)])));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearPedidoDigitalRequest(
                null,
                SedeIdPrueba,
                null,
                "WHATSAPP",
                DateTimeOffset.UtcNow,
                [new CrearPedidoDigitalDetalleRequest(productoId, null, presentacionId, null, 1m, 59m)])));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearPedidoDigitalRequest(
                clienteId,
                SedeIdPrueba,
                null,
                "WHATSAPP",
                DateTimeOffset.UtcNow,
                [new CrearPedidoDigitalDetalleRequest(productoId, null, null, null, 1m, 59m)])));
    }

    [Fact]
    public void Crear_pedido_digital_use_case_depende_de_stock_para_reservar()
    {
        var constructorTypes = typeof(CrearPedidoDigitalUseCase)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IStockProductoRepository), constructorTypes);
        Assert.Contains(typeof(IMovimientoInventarioRepository), constructorTypes);
    }

    private static CrearPedidoDigitalUseCase CrearUseCase(
        PedidoDigitalRepositoryFake pedidoRepository,
        ProductoRepositoryFake productoRepository,
        ProductoVarianteRepositoryFake varianteRepository,
        ProductoPresentacionRepositoryFake presentacionRepository,
        ClienteRepositoryFake clienteRepository,
        StockProductoRepositoryFake stockRepository,
        Guid? empresaId,
        MovimientoInventarioRepositoryFake? movimientos = null)
    {
        var sedeRepository = new SedeRepositoryFake();
        var puntoVentaRepository = new PuntoVentaRepositoryFake();
        if (empresaId.HasValue)
        {
            sedeRepository.Sedes.Add(new Sede(SedeIdPrueba, empresaId.Value, "Tienda principal", TipoSede.TIENDA));
            puntoVentaRepository.PuntosVenta.Add(new PuntoVenta(
                PuntoVentaIdPrueba,
                empresaId.Value,
                SedeIdPrueba,
                "Caja principal"));
        }

        return new CrearPedidoDigitalUseCase(
            pedidoRepository,
            productoRepository,
            varianteRepository,
            presentacionRepository,
            clienteRepository,
            sedeRepository,
            puntoVentaRepository,
            stockRepository,
            empresaId.HasValue
                ? new EmpresaActivaContextFake(empresaId.Value)
                : new EmpresaActivaContextFake(),
            movimientos);
    }

    private static CrearPedidoDigitalRequest CrearRequest(
        Guid productoId,
        string canal,
        decimal cantidad = 1m)
    {
        return new CrearPedidoDigitalRequest(
            null,
            SedeIdPrueba,
            null,
            canal,
            DateTimeOffset.UtcNow,
            [new CrearPedidoDigitalDetalleRequest(productoId, null, null, null, cantidad, 59m)]);
    }

    private sealed class PedidoDigitalRepositoryFake : IPedidoDigitalRepository
    {
        public List<PedidoDigital> Pedidos { get; } = [];

        public Task AgregarAsync(PedidoDigital pedido, CancellationToken cancellationToken = default)
        {
            Pedidos.Add(pedido);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<PedidoDigital>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PedidoDigital>>(
                Pedidos.Where(pedido => pedido.EmpresaId == empresaId).ToArray());
        }

        public Task<PedidoDigital?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Pedidos.FirstOrDefault(pedido =>
                pedido.EmpresaId == empresaId &&
                pedido.Id == id));
        }
    }

    private sealed class StockProductoRepositoryFake : IStockProductoRepository
    {
        private readonly List<StockProducto> _stocks = [];

        public Task<StockProducto?> ObtenerPorProductoAsync(
            Guid empresaId,
            Guid sedeId,
            Guid productoId,
            Guid? productoVarianteId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_stocks.FirstOrDefault(stock =>
                stock.EmpresaId == empresaId &&
                stock.SedeId == sedeId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId == productoVarianteId));
        }

        public Task<IReadOnlyCollection<StockProducto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<StockProducto>>(
                _stocks.Where(stock => stock.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<StockProducto>> ListarPorSedeAsync(
            Guid empresaId,
            Guid sedeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<StockProducto>>(
                _stocks.Where(stock => stock.EmpresaId == empresaId && stock.SedeId == sedeId).ToArray());
        }

        public Task GuardarAsync(StockProducto stock, CancellationToken cancellationToken = default)
        {
            if (!_stocks.Contains(stock))
            {
                _stocks.Add(stock);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MovimientoInventarioRepositoryFake : IMovimientoInventarioRepository
    {
        public List<MovimientoInventario> Movimientos { get; } = [];

        public Task AgregarAsync(MovimientoInventario movimiento, CancellationToken cancellationToken = default)
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

    private sealed class ProductoRepositoryFake : IProductoRepository
    {
        private readonly List<Producto> _productos = [];

        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            _productos.Add(producto);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Producto>>(
                _productos.Where(producto => producto.EmpresaId == empresaId).ToArray());
        }

        public Task<Producto?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
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

    private sealed class ProductoVarianteRepositoryFake : IProductoVarianteRepository
    {
        private readonly List<ProductoVariante> _variantes = [];

        public Task AgregarAsync(ProductoVariante variante, CancellationToken cancellationToken = default)
        {
            _variantes.Add(variante);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(Guid empresaId, Guid productoId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>(
                _variantes.Where(variante => variante.EmpresaId == empresaId && variante.ProductoId == productoId).ToArray());
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>(
                _variantes.Where(variante => variante.EmpresaId == empresaId).ToArray());
        }

        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_variantes.FirstOrDefault(variante =>
                variante.EmpresaId == empresaId &&
                variante.Id == id));
        }

        public Task ActualizarAsync(ProductoVariante variante, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteSkuAsync(Guid empresaId, string codigoSku, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> ExisteCodigoBarrasAsync(Guid empresaId, string codigoBarras, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class ProductoPresentacionRepositoryFake : IProductoPresentacionRepository
    {
        private readonly List<ProductoPresentacion> _presentaciones = [];

        public Task AgregarAsync(ProductoPresentacion presentacion, CancellationToken cancellationToken = default)
        {
            _presentaciones.Add(presentacion);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoPresentacion>> ListarPorProductoAsync(Guid empresaId, Guid productoId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoPresentacion>>(
                _presentaciones.Where(presentacion => presentacion.EmpresaId == empresaId && presentacion.ProductoId == productoId).ToArray());
        }

        public Task<ProductoPresentacion?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_presentaciones.FirstOrDefault(presentacion =>
                presentacion.EmpresaId == empresaId &&
                presentacion.Id == id));
        }

        public Task<bool> ExisteCodigoBarrasAsync(Guid empresaId, string codigoBarras, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class ClienteRepositoryFake : IClienteRepository
    {
        private readonly List<Cliente> _clientes = [];

        public Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            _clientes.Add(cliente);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Cliente>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Cliente>>(
                _clientes.Where(cliente => cliente.EmpresaId == empresaId).ToArray());
        }

        public Task<Cliente?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_clientes.FirstOrDefault(cliente =>
                cliente.EmpresaId == empresaId &&
                cliente.Id == id));
        }

        public Task ActualizarAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SedeRepositoryFake : ISedeRepository
    {
        public List<Sede> Sedes { get; } = [];

        public Task AgregarAsync(Sede sede, CancellationToken cancellationToken = default)
        {
            Sedes.Add(sede);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Sede>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Sede>>(
                Sedes.Where(sede => sede.EmpresaId == empresaId).ToArray());
        }

        public Task<Sede?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sedes.FirstOrDefault(sede =>
                sede.EmpresaId == empresaId &&
                sede.Id == id));
        }
    }

    private sealed class PuntoVentaRepositoryFake : IPuntoVentaRepository
    {
        public List<PuntoVenta> PuntosVenta { get; } = [];

        public Task AgregarAsync(PuntoVenta puntoVenta, CancellationToken cancellationToken = default)
        {
            PuntosVenta.Add(puntoVenta);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<PuntoVenta>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PuntoVenta>>(
                PuntosVenta.Where(puntoVenta => puntoVenta.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<PuntoVenta>> ListarPorSedeAsync(Guid empresaId, Guid sedeId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PuntoVenta>>(
                PuntosVenta.Where(puntoVenta => puntoVenta.EmpresaId == empresaId && puntoVenta.SedeId == sedeId).ToArray());
        }

        public Task<PuntoVenta?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PuntosVenta.FirstOrDefault(puntoVenta =>
                puntoVenta.EmpresaId == empresaId &&
                puntoVenta.Id == id));
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
