using CapitalPos.Application.Clientes;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Series;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationAnularVentaTests
{
    [Fact]
    public async Task Anular_sin_cpe_restituye_cantidad_base_y_conserva_pagos()
    {
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(
            Guid.NewGuid(), empresaId, ventaId, productoId, 1m, 12m, 0m, 12m, varianteId, null, 12m, 12m);
        var pago = new VentaPago(Guid.NewGuid(), empresaId, ventaId, MetodoPago.EFECTIVO, 12m);
        var venta = new Venta(
            ventaId, empresaId, DateTimeOffset.UtcNow, 12m, 0m, 12m, [detalle], sedeId, Guid.NewGuid(), pagos: [pago]);
        var ventas = new VentasFake(venta);
        var stocks = new StocksFake(new StockProducto(Guid.NewGuid(), empresaId, sedeId, productoId, varianteId, 3m));
        var useCase = CrearUseCase(ventas, stocks, new ComprobantesFake(), empresaId);

        var result = await useCase.EjecutarAsync(ventaId, new AnularVentaRequest("Cliente desistió"));

        Assert.Equal(EstadoVenta.Anulada, result!.Estado);
        Assert.Equal("Cliente desistió", result.ObservacionAnulacion);
        Assert.NotNull(result.FechaAnulacion);
        Assert.Single(result.Pagos);
        Assert.Equal(15m, stocks.Stock.CantidadDisponible);
        Assert.True(ventas.Guardado);
    }

    [Fact]
    public async Task Anular_con_cpe_aceptado_y_nc_rechazada_no_revierte_stock()
    {
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(Guid.NewGuid(), empresaId, ventaId, productoId, 1m, 1m, 0m, 1m);
        var venta = new Venta(ventaId, empresaId, DateTimeOffset.UtcNow, 1m, 0m, 1m, [detalle], sedeId, Guid.NewGuid());
        var stocks = new StocksFake(new StockProducto(Guid.NewGuid(), empresaId, sedeId, productoId, null, 3m));
        var emision = new Comprobante(Guid.NewGuid(), empresaId, ventaId, "03", "B001", 1, "ACEPTADO");
        var emitirNc = new EmitirNotaCreditoFake(
            new EmitirNotaCreditoDesdeVentaResult(
                new CpeGatewayResponse(
                    400,
                    false,
                    """{"ok":false,"mensaje":"Rechazado","data":{"ok":false,"estado":"RECHAZADO","mensaje":"CDR rechazado"}}""",
                    "application/json"),
                "07",
                "BC01",
                1,
                emision.Id,
                "03",
                "B001",
                1,
                "01",
                "Anulacion"));
        var useCase = CrearUseCase(
            new VentasFake(venta),
            stocks,
            new ComprobantesFake(emision: emision),
            empresaId,
            emitirNc);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.EjecutarAsync(ventaId, new AnularVentaRequest()));

        Assert.Contains("nota de credito", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoVenta.Registrada, venta.Estado);
        Assert.Equal(3m, stocks.Stock.CantidadDisponible);
        Assert.True(emitirNc.Invocado);
    }

    [Fact]
    public async Task Anular_con_cpe_aceptado_y_nc_aceptada_revierte_stock_y_registra_nc()
    {
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(Guid.NewGuid(), empresaId, ventaId, productoId, 1m, 1m, 0m, 1m);
        var venta = new Venta(ventaId, empresaId, DateTimeOffset.UtcNow, 1m, 0m, 1m, [detalle], sedeId, Guid.NewGuid());
        var stocks = new StocksFake(new StockProducto(Guid.NewGuid(), empresaId, sedeId, productoId, null, 3m));
        var emision = new Comprobante(Guid.NewGuid(), empresaId, ventaId, "03", "B001", 10, "ACEPTADO");
        var registrar = new RegistrarComprobanteFake(empresaId);
        var emitirNc = new EmitirNotaCreditoFake(
            new EmitirNotaCreditoDesdeVentaResult(
                new CpeGatewayResponse(
                    200,
                    true,
                    """{"ok":true,"data":{"ok":true,"estado":"SIMULADO","mensaje":"OK","hash":"h","nombreXml":"x.xml","nombreZip":"x.zip","nombreCdr":"R-x.zip"}}""",
                    "application/json"),
                "07",
                "BC01",
                1,
                emision.Id,
                "03",
                "B001",
                10,
                "01",
                "Anulacion de la operacion"));
        var useCase = CrearUseCase(
            new VentasFake(venta),
            stocks,
            new ComprobantesFake(emision: emision),
            empresaId,
            emitirNc,
            registrar);

        var result = await useCase.EjecutarAsync(ventaId, new AnularVentaRequest("Cliente desistio"));

        Assert.Equal(EstadoVenta.Anulada, result!.Estado);
        Assert.Equal(4m, stocks.Stock.CantidadDisponible);
        Assert.NotNull(registrar.UltimoRequest);
        Assert.Equal("07", registrar.UltimoRequest!.TipoComprobante);
        Assert.Equal(emision.Id, registrar.UltimoRequest.ComprobanteAfectadoId);
        Assert.Equal("01", registrar.UltimoRequest.CodigoMotivo);
        Assert.Equal("BC01", registrar.UltimoRequest.Serie);
    }

    [Fact]
    public async Task Anular_con_nc_ya_aceptada_no_vuelve_a_emitir_y_revierte_stock()
    {
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(Guid.NewGuid(), empresaId, ventaId, productoId, 1m, 1m, 0m, 1m);
        var venta = new Venta(ventaId, empresaId, DateTimeOffset.UtcNow, 1m, 0m, 1m, [detalle], sedeId, Guid.NewGuid());
        var stocks = new StocksFake(new StockProducto(Guid.NewGuid(), empresaId, sedeId, productoId, null, 2m));
        var emision = new Comprobante(Guid.NewGuid(), empresaId, ventaId, "03", "B001", 1, "ACEPTADO");
        var nc = new Comprobante(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            "07",
            "BC01",
            1,
            "ACEPTADO",
            comprobanteAfectadoId: emision.Id,
            tipoComprobanteAfectado: "03",
            serieAfectada: "B001",
            correlativoAfectado: 1,
            codigoMotivo: "01",
            descripcionMotivo: "Anulacion");
        var emitirNc = new EmitirNotaCreditoFake(null);
        var useCase = CrearUseCase(
            new VentasFake(venta),
            stocks,
            new ComprobantesFake(emision: emision, notaCredito: nc),
            empresaId,
            emitirNc);

        var result = await useCase.EjecutarAsync(ventaId, new AnularVentaRequest());

        Assert.Equal(EstadoVenta.Anulada, result!.Estado);
        Assert.Equal(3m, stocks.Stock.CantidadDisponible);
        Assert.False(emitirNc.Invocado);
    }

    private static AnularVentaUseCase CrearUseCase(
        VentasFake ventas,
        StocksFake stocks,
        ComprobantesFake comprobantes,
        Guid empresaId,
        EmitirNotaCreditoFake? emitirNc = null,
        RegistrarComprobanteFake? registrar = null)
    {
        return new AnularVentaUseCase(
            ventas,
            stocks,
            comprobantes,
            emitirNc ?? new EmitirNotaCreditoFake(null),
            registrar ?? new RegistrarComprobanteFake(empresaId),
            new EmpresaFake(empresaId));
    }

    private sealed class EmpresaFake(Guid empresaId) : IEmpresaActivaContext
    {
        public bool TieneEmpresaActiva => true;
        public Guid UsuarioId => Guid.NewGuid();
        public Guid EmpresaId => empresaId;
        public RolEmpresa Rol => RolEmpresa.Administrador;
    }

    private sealed class VentasFake(Venta venta) : IVentaRepository
    {
        public bool Guardado { get; private set; }

        public Task AgregarAsync(Venta v, CancellationToken c = default) => Task.CompletedTask;

        public Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(Guid e, CancellationToken c = default) =>
            Task.FromResult<IReadOnlyCollection<Venta>>([venta]);

        public Task<IReadOnlyCollection<Venta>> ListarRegistradasPorEmpresaYFechaAsync(
            Guid e, DateTimeOffset d, DateTimeOffset h, CancellationToken c = default) =>
            Task.FromResult<IReadOnlyCollection<Venta>>([venta]);

        public Task<Venta?> ObtenerPorEmpresaAsync(Guid e, Guid id, CancellationToken c = default) =>
            Task.FromResult(e == venta.EmpresaId && id == venta.Id ? venta : null)!;

        public Task GuardarCambiosAsync(CancellationToken c = default)
        {
            Guardado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StocksFake(StockProducto stock) : IStockProductoRepository
    {
        public StockProducto Stock => stock;

        public Task<StockProducto?> ObtenerPorProductoAsync(
            Guid e, Guid s, Guid p, Guid? v = null, CancellationToken c = default) =>
            Task.FromResult(
                e == stock.EmpresaId && s == stock.SedeId && p == stock.ProductoId && v == stock.ProductoVarianteId
                    ? stock
                    : null)!;

        public Task<IReadOnlyCollection<StockProducto>> ListarPorEmpresaAsync(Guid e, CancellationToken c = default) =>
            Task.FromResult<IReadOnlyCollection<StockProducto>>([stock]);

        public Task<IReadOnlyCollection<StockProducto>> ListarPorSedeAsync(Guid e, Guid s, CancellationToken c = default) =>
            Task.FromResult<IReadOnlyCollection<StockProducto>>([stock]);

        public Task GuardarAsync(StockProducto s, CancellationToken c = default) => Task.CompletedTask;
    }

    private sealed class ComprobantesFake(
        Comprobante? emision = null,
        Comprobante? notaCredito = null) : IComprobanteRepository
    {
        public Task AgregarAsync(Comprobante c, CancellationToken t = default) => Task.CompletedTask;

        public Task<bool> ExistePorVentaAsync(Guid e, Guid v, CancellationToken t = default) =>
            Task.FromResult(emision is not null || notaCredito is not null);

        public Task<Comprobante?> ObtenerEmisionAceptadaPorVentaAsync(
            Guid empresaId, Guid ventaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                emision is not null && emision.EmpresaId == empresaId && emision.VentaId == ventaId
                    ? emision
                    : null);

        public Task<Comprobante?> ObtenerNotaCreditoAceptadaPorComprobanteAfectadoAsync(
            Guid empresaId, Guid comprobanteAfectadoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                notaCredito is not null &&
                notaCredito.EmpresaId == empresaId &&
                notaCredito.ComprobanteAfectadoId == comprobanteAfectadoId
                    ? notaCredito
                    : null);
    }

    private sealed class EmitirNotaCreditoFake : EmitirNotaCreditoDesdeVentaUseCase
    {
        private readonly EmitirNotaCreditoDesdeVentaResult? _result;

        public EmitirNotaCreditoFake(EmitirNotaCreditoDesdeVentaResult? result)
            : base(
                new NullVentaRepository(),
                new ComprobantesFake(),
                new NullConfiguracionFiscalRepository(),
                new NullClienteRepository(),
                new NullProductoRepository(),
                new NullPresentacionRepository(),
                new NullUnidadRepository(),
                new NullVarianteRepository(),
                new NullSerieRepository(),
                new NullCpeGateway(),
                new EmpresaFake(Guid.NewGuid()))
        {
            _result = result;
        }

        public bool Invocado { get; private set; }

        public override Task<EmitirNotaCreditoDesdeVentaResult?> EjecutarAsync(
            Guid ventaId,
            EmitirNotaCreditoDesdeVentaRequest request,
            CancellationToken cancellationToken = default)
        {
            Invocado = true;
            return Task.FromResult(_result);
        }
    }

    private sealed class RegistrarComprobanteFake : RegistrarComprobanteCpeUseCase
    {
        public RegistrarComprobanteFake(Guid empresaId)
            : base(new ComprobantesFake(), new NullVentaRepository(), new EmpresaFake(empresaId))
        {
        }

        public RegistrarComprobanteCpeRequest? UltimoRequest { get; private set; }

        public override Task<Comprobante?> EjecutarAsync(
            RegistrarComprobanteCpeRequest request,
            CancellationToken cancellationToken = default)
        {
            UltimoRequest = request;
            return Task.FromResult<Comprobante?>(
                new Comprobante(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    request.VentaId,
                    request.TipoComprobante,
                    request.Serie,
                    request.Correlativo,
                    request.EstadoCpe,
                    request.Mensaje,
                    request.Hash,
                    request.NombreXml,
                    request.NombreZip,
                    request.NombreCdr,
                    comprobanteAfectadoId: request.ComprobanteAfectadoId,
                    tipoComprobanteAfectado: request.TipoComprobanteAfectado,
                    serieAfectada: request.SerieAfectada,
                    correlativoAfectado: request.CorrelativoAfectado,
                    codigoMotivo: request.CodigoMotivo,
                    descripcionMotivo: request.DescripcionMotivo));
        }
    }

    private sealed class NullVentaRepository : IVentaRepository
    {
        public Task AgregarAsync(Venta venta, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Venta>>([]);
        public Task<IReadOnlyCollection<Venta>> ListarRegistradasPorEmpresaYFechaAsync(Guid empresaId, DateTimeOffset desde, DateTimeOffset hasta, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Venta>>([]);
        public Task<Venta?> ObtenerPorEmpresaAsync(Guid empresaId, Guid ventaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Venta?>(null);
        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NullConfiguracionFiscalRepository : IConfiguracionFiscalEmpresaRepository
    {
        public Task<ConfiguracionFiscalEmpresa?> ObtenerPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConfiguracionFiscalEmpresa?>(null);
        public Task GuardarAsync(ConfiguracionFiscalEmpresa configuracion, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NullClienteRepository : IClienteRepository
    {
        public Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<Cliente>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Cliente>>([]);
        public Task<Cliente?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Cliente?>(null);
        public Task ActualizarAsync(Cliente cliente, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NullProductoRepository : IProductoRepository
    {
        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Producto>>([]);
        public Task<Producto?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Producto?>(null);
        public Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NullPresentacionRepository : IProductoPresentacionRepository
    {
        public Task AgregarAsync(ProductoPresentacion presentacion, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<ProductoPresentacion>> ListarPorProductoAsync(Guid empresaId, Guid productoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ProductoPresentacion>>([]);
        public Task<ProductoPresentacion?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductoPresentacion?>(null);
        public Task<bool> ExisteCodigoBarrasAsync(Guid empresaId, string codigoBarras, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class NullUnidadRepository : IUnidadMedidaRepository
    {
        public Task AgregarAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<UnidadMedida>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<UnidadMedida>>([]);
        public Task<UnidadMedida?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<UnidadMedida?>(null);
        public Task<UnidadMedida?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancellationToken = default) =>
            Task.FromResult<UnidadMedida?>(null);
    }

    private sealed class NullVarianteRepository : IProductoVarianteRepository
    {
        public Task AgregarAsync(ProductoVariante variante, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(Guid empresaId, Guid productoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ProductoVariante>>([]);
        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ProductoVariante>>([]);
        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductoVariante?>(null);
        public Task ActualizarAsync(ProductoVariante variante, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExisteSkuAsync(Guid empresaId, string codigoSku, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> ExisteCodigoBarrasAsync(Guid empresaId, string codigoBarras, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class NullSerieRepository : ISerieComprobanteRepository
    {
        public Task AgregarAsync(SerieComprobante serie, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<SerieComprobante>> ListarPorSedeAsync(Guid empresaId, Guid sedeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<SerieComprobante>>([]);
        public Task<SerieComprobante?> ObtenerActivaAsync(Guid empresaId, Guid sedeId, string tipoComprobante, string serie, CancellationToken cancellationToken = default) =>
            Task.FromResult<SerieComprobante?>(null);
        public Task<SerieComprobante?> ObtenerActivaPorSedeYTipoAsync(Guid empresaId, Guid sedeId, string tipoComprobante, CancellationToken cancellationToken = default) =>
            Task.FromResult<SerieComprobante?>(null);
        public Task GuardarAsync(SerieComprobante serie, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NullCpeGateway : ICpeGateway
    {
        public Task<CpeGatewayResponse> EmitirAsync(System.Text.Json.JsonElement request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CpeGatewayResponse(500, false, string.Empty, string.Empty));
        public Task<CpeGatewayResponse> ObtenerEstadoAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CpeGatewayResponse(500, false, string.Empty, string.Empty));
    }
}
