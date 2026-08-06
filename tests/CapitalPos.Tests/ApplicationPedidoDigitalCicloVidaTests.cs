using CapitalPos.Application.Caja;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Pedidos;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationPedidoDigitalCicloVidaTests
{
    private static readonly Guid SedeIdPrueba = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PuntoVentaIdPrueba = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Cancelar_pedido_libera_reserva_y_registra_liberacion_en_kardex()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var pedidoRepository = new PedidoDigitalRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        var movimientos = new MovimientoInventarioRepositoryFake();
        var stock = new StockProducto(Guid.NewGuid(), empresaId, SedeIdPrueba, productoId, null, 10m);
        stock.Reservar(2m);
        await stockRepository.GuardarAsync(stock);

        var pedido = CrearPedidoConDetalle(empresaId, productoId, cantidad: 2m, precio: 59m);
        await pedidoRepository.AgregarAsync(pedido);

        var useCase = new CancelarPedidoDigitalUseCase(
            pedidoRepository,
            stockRepository,
            new EmpresaActivaContextFake(empresaId),
            movimientos);

        var cancelado = await useCase.EjecutarAsync(
            pedido.Id,
            new CancelarPedidoDigitalRequest("Cliente cancelo"),
            CancellationToken.None);

        Assert.NotNull(cancelado);
        Assert.Equal(EstadoPedidoDigital.Cancelado, cancelado.Estado);
        Assert.True(pedidoRepository.Guardado);
        Assert.Equal(10m, stock.CantidadDisponible);
        Assert.Equal(0m, stock.CantidadReservada);
        Assert.Equal(10m, stock.CantidadLibre);
        var movimiento = Assert.Single(movimientos.Movimientos);
        Assert.Equal(TipoMovimientoInventario.LIBERACION_RESERVA, movimiento.TipoMovimiento);
        Assert.Equal(2m, movimiento.Cantidad);
        Assert.Equal(8m, movimiento.StockAnterior);
        Assert.Equal(10m, movimiento.StockPosterior);
        Assert.Equal("PEDIDO_DIGITAL", movimiento.ReferenciaTipo);
        Assert.Equal(pedido.Id, movimiento.ReferenciaId);
    }

    [Fact]
    public async Task Cancelar_pedido_inexistente_en_empresa_activa_retorna_null()
    {
        var useCase = new CancelarPedidoDigitalUseCase(
            new PedidoDigitalRepositoryFake(),
            new StockProductoRepositoryFake(),
            new EmpresaActivaContextFake(Guid.NewGuid()));

        var resultado = await useCase.EjecutarAsync(
            Guid.NewGuid(),
            new CancelarPedidoDigitalRequest(),
            CancellationToken.None);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task Convertir_pedido_confirma_reserva_crea_venta_marketing_y_marca_entregado()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var pedidoRepository = new PedidoDigitalRepositoryFake();
        var ventaRepository = new VentaRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        var sesionCajaRepository = new SesionCajaRepositoryFake();
        var puntoVentaRepository = new PuntoVentaRepositoryFake();
        var movimientos = new MovimientoInventarioRepositoryFake();

        puntoVentaRepository.PuntosVenta.Add(new PuntoVenta(
            PuntoVentaIdPrueba,
            empresaId,
            SedeIdPrueba,
            "Caja principal"));
        sesionCajaRepository.Sesiones.Add(new SesionCaja(
            Guid.NewGuid(),
            empresaId,
            SedeIdPrueba,
            PuntoVentaIdPrueba,
            100m));

        var stock = new StockProducto(Guid.NewGuid(), empresaId, SedeIdPrueba, productoId, null, 10m);
        stock.Reservar(2m);
        await stockRepository.GuardarAsync(stock);

        var pedido = CrearPedidoConDetalle(
            empresaId,
            productoId,
            cantidad: 2m,
            precio: 59m,
            puntoVentaId: PuntoVentaIdPrueba);
        await pedidoRepository.AgregarAsync(pedido);

        var useCase = new ConvertirPedidoDigitalAVentaUseCase(
            pedidoRepository,
            ventaRepository,
            stockRepository,
            sesionCajaRepository,
            puntoVentaRepository,
            new EmpresaActivaContextFake(empresaId),
            movimientos);

        var resultado = await useCase.EjecutarAsync(
            pedido.Id,
            new ConvertirPedidoDigitalAVentaRequest(Observacion: "Cobrado en caja"),
            CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Equal(EstadoPedidoDigital.Entregado, resultado.Pedido.Estado);
        Assert.Equal(CanalVenta.MARKETING, resultado.Venta.CanalVenta);
        Assert.Equal(pedido.SedeId, resultado.Venta.SedeId);
        Assert.Equal(PuntoVentaIdPrueba, resultado.Venta.PuntoVentaId);
        Assert.Equal(118m, resultado.Venta.Total);
        Assert.Same(resultado.Venta, Assert.Single(ventaRepository.Ventas));
        Assert.True(pedidoRepository.Guardado);
        Assert.Equal(8m, stock.CantidadDisponible);
        Assert.Equal(0m, stock.CantidadReservada);
        Assert.Equal(8m, stock.CantidadLibre);
        var movimiento = Assert.Single(movimientos.Movimientos);
        Assert.Equal(TipoMovimientoInventario.VENTA, movimiento.TipoMovimiento);
        Assert.Equal(2m, movimiento.Cantidad);
        Assert.Equal(10m, movimiento.StockAnterior);
        Assert.Equal(8m, movimiento.StockPosterior);
        Assert.Equal("PEDIDO_DIGITAL", movimiento.ReferenciaTipo);
    }

    [Fact]
    public async Task Convertir_pedido_exige_sesion_de_caja_abierta()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var pedidoRepository = new PedidoDigitalRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        var puntoVentaRepository = new PuntoVentaRepositoryFake();
        puntoVentaRepository.PuntosVenta.Add(new PuntoVenta(
            PuntoVentaIdPrueba,
            empresaId,
            SedeIdPrueba,
            "Caja principal"));

        var stock = new StockProducto(Guid.NewGuid(), empresaId, SedeIdPrueba, productoId, null, 5m);
        stock.Reservar(1m);
        await stockRepository.GuardarAsync(stock);
        var pedido = CrearPedidoConDetalle(empresaId, productoId, 1m, 59m, PuntoVentaIdPrueba);
        await pedidoRepository.AgregarAsync(pedido);

        var useCase = new ConvertirPedidoDigitalAVentaUseCase(
            pedidoRepository,
            new VentaRepositoryFake(),
            stockRepository,
            new SesionCajaRepositoryFake(),
            puntoVentaRepository,
            new EmpresaActivaContextFake(empresaId));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(pedido.Id, new ConvertirPedidoDigitalAVentaRequest(), CancellationToken.None));

        Assert.Contains("sesion de caja", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoPedidoDigital.PendientePago, pedido.Estado);
        Assert.Equal(1m, stock.CantidadReservada);
        Assert.Equal(5m, stock.CantidadDisponible);
    }

    [Fact]
    public async Task Convertir_pedido_cancelado_falla()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var pedidoRepository = new PedidoDigitalRepositoryFake();
        var puntoVentaRepository = new PuntoVentaRepositoryFake();
        puntoVentaRepository.PuntosVenta.Add(new PuntoVenta(
            PuntoVentaIdPrueba,
            empresaId,
            SedeIdPrueba,
            "Caja"));
        var pedido = CrearPedidoConDetalle(empresaId, productoId, 1m, 59m, PuntoVentaIdPrueba);
        pedido.Cancelar();
        await pedidoRepository.AgregarAsync(pedido);

        var useCase = new ConvertirPedidoDigitalAVentaUseCase(
            pedidoRepository,
            new VentaRepositoryFake(),
            new StockProductoRepositoryFake(),
            new SesionCajaRepositoryFake(),
            puntoVentaRepository,
            new EmpresaActivaContextFake(empresaId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(pedido.Id, new ConvertirPedidoDigitalAVentaRequest(), CancellationToken.None));
    }

    private static PedidoDigital CrearPedidoConDetalle(
        Guid empresaId,
        Guid productoId,
        decimal cantidad,
        decimal precio,
        Guid? puntoVentaId = null)
    {
        var pedidoId = Guid.NewGuid();
        var detalle = new PedidoDigitalDetalle(
            Guid.NewGuid(),
            empresaId,
            pedidoId,
            productoId,
            "Polo digital",
            cantidad,
            precio);
        return new PedidoDigital(
            pedidoId,
            empresaId,
            SedeIdPrueba,
            CanalPedidoDigital.FACEBOOK_SUBASTA,
            DateTimeOffset.UtcNow,
            [detalle],
            puntoVentaId: puntoVentaId);
    }

    private sealed class PedidoDigitalRepositoryFake : IPedidoDigitalRepository
    {
        public List<PedidoDigital> Pedidos { get; } = [];

        public bool Guardado { get; private set; }

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

        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            Guardado = true;
            return Task.CompletedTask;
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

    private sealed class PuntoVentaRepositoryFake : IPuntoVentaRepository
    {
        public List<PuntoVenta> PuntosVenta { get; } = [];

        public Task AgregarAsync(PuntoVenta puntoVenta, CancellationToken cancellationToken = default)
        {
            PuntosVenta.Add(puntoVenta);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<PuntoVenta>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PuntoVenta>>(
                PuntosVenta.Where(puntoVenta => puntoVenta.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<PuntoVenta>> ListarPorSedeAsync(
            Guid empresaId,
            Guid sedeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PuntoVenta>>(
                PuntosVenta.Where(puntoVenta => puntoVenta.EmpresaId == empresaId && puntoVenta.SedeId == sedeId).ToArray());
        }

        public Task<PuntoVenta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PuntosVenta.FirstOrDefault(puntoVenta =>
                puntoVenta.EmpresaId == empresaId &&
                puntoVenta.Id == id));
        }
    }

    private sealed class SesionCajaRepositoryFake : ISesionCajaRepository
    {
        public List<SesionCaja> Sesiones { get; } = [];

        public Task AgregarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
        {
            Sesiones.Add(sesionCaja);
            return Task.CompletedTask;
        }

        public Task<SesionCaja?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sesiones.FirstOrDefault(sesion =>
                sesion.EmpresaId == empresaId &&
                sesion.Id == id));
        }

        public Task<SesionCaja?> ObtenerAbiertaPorPuntoVentaAsync(
            Guid empresaId,
            Guid puntoVentaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sesiones.FirstOrDefault(sesion =>
                sesion.EmpresaId == empresaId &&
                sesion.PuntoVentaId == puntoVentaId &&
                sesion.Estado == EstadoSesionCaja.Abierta));
        }

        public Task GuardarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
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
                    venta.EmpresaId == empresaId &&
                    venta.Estado == EstadoVenta.Registrada &&
                    venta.Fecha >= desde &&
                    venta.Fecha < hastaExclusivo).ToArray());
        }

        public Task<Venta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Ventas.FirstOrDefault(venta =>
                venta.EmpresaId == empresaId &&
                venta.Id == id));
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
