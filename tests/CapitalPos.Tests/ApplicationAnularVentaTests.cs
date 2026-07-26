using CapitalPos.Application.Inventario;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationAnularVentaTests
{
    [Fact]
    public async Task Anular_restituye_cantidad_base_y_conserva_pagos()
    {
        var empresaId = Guid.NewGuid(); var sedeId = Guid.NewGuid(); var productoId = Guid.NewGuid(); var varianteId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(Guid.NewGuid(), empresaId, ventaId, productoId, 1m, 12m, 0m, 12m, varianteId, null, 12m, 12m);
        var pago = new VentaPago(Guid.NewGuid(), empresaId, ventaId, MetodoPago.EFECTIVO, 12m);
        var venta = new Venta(ventaId, empresaId, DateTimeOffset.UtcNow, 12m, 0m, 12m, [detalle], sedeId, Guid.NewGuid(), pagos: [pago]);
        var ventas = new VentasFake(venta);
        var stocks = new StocksFake(new StockProducto(Guid.NewGuid(), empresaId, sedeId, productoId, varianteId, 3m));
        var useCase = new AnularVentaUseCase(ventas, stocks, new ComprobantesFake(), new EmpresaFake(empresaId));

        var result = await useCase.EjecutarAsync(ventaId, new AnularVentaRequest("Cliente desistió"));

        Assert.Equal(EstadoVenta.Anulada, result!.Estado);
        Assert.Equal("Cliente desistió", result.ObservacionAnulacion);
        Assert.NotNull(result.FechaAnulacion);
        Assert.Single(result.Pagos);
        Assert.Equal(15m, stocks.Stock.CantidadDisponible);
        Assert.True(ventas.Guardado);
    }

    [Fact]
    public async Task Anular_con_comprobante_falla_sin_revertir_stock()
    {
        var empresaId = Guid.NewGuid(); var sedeId = Guid.NewGuid(); var productoId = Guid.NewGuid(); var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(Guid.NewGuid(), empresaId, ventaId, productoId, 1m, 1m, 0m, 1m);
        var venta = new Venta(ventaId, empresaId, DateTimeOffset.UtcNow, 1m, 0m, 1m, [detalle], sedeId, Guid.NewGuid());
        var stocks = new StocksFake(new StockProducto(Guid.NewGuid(), empresaId, sedeId, productoId, null, 3m));
        var useCase = new AnularVentaUseCase(new VentasFake(venta), stocks, new ComprobantesFake(true), new EmpresaFake(empresaId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(ventaId, new AnularVentaRequest()));
        Assert.Equal(EstadoVenta.Registrada, venta.Estado);
        Assert.Equal(3m, stocks.Stock.CantidadDisponible);
    }

    private sealed class EmpresaFake(Guid empresaId) : IEmpresaActivaContext { public bool TieneEmpresaActiva => true; public Guid UsuarioId => Guid.NewGuid(); public Guid EmpresaId => empresaId; public RolEmpresa Rol => RolEmpresa.Administrador; }
    private sealed class VentasFake(Venta venta) : IVentaRepository { public bool Guardado { get; private set; } public Task AgregarAsync(Venta v, CancellationToken c = default) => Task.CompletedTask; public Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(Guid e, CancellationToken c = default) => Task.FromResult<IReadOnlyCollection<Venta>>([venta]); public Task<IReadOnlyCollection<Venta>> ListarRegistradasPorEmpresaYFechaAsync(Guid e, DateTimeOffset d, DateTimeOffset h, CancellationToken c = default) => Task.FromResult<IReadOnlyCollection<Venta>>([venta]); public Task<Venta?> ObtenerPorEmpresaAsync(Guid e, Guid id, CancellationToken c = default) => Task.FromResult(e == venta.EmpresaId && id == venta.Id ? venta : null)!; public Task GuardarCambiosAsync(CancellationToken c = default) { Guardado = true; return Task.CompletedTask; } }
    private sealed class StocksFake(StockProducto stock) : IStockProductoRepository { public StockProducto Stock => stock; public Task<StockProducto?> ObtenerPorProductoAsync(Guid e, Guid s, Guid p, Guid? v = null, CancellationToken c = default) => Task.FromResult(e == stock.EmpresaId && s == stock.SedeId && p == stock.ProductoId && v == stock.ProductoVarianteId ? stock : null)!; public Task<IReadOnlyCollection<StockProducto>> ListarPorEmpresaAsync(Guid e, CancellationToken c = default) => Task.FromResult<IReadOnlyCollection<StockProducto>>([stock]); public Task<IReadOnlyCollection<StockProducto>> ListarPorSedeAsync(Guid e, Guid s, CancellationToken c = default) => Task.FromResult<IReadOnlyCollection<StockProducto>>([stock]); public Task GuardarAsync(StockProducto s, CancellationToken c = default) => Task.CompletedTask; }
    private sealed class ComprobantesFake(bool existe = false) : IComprobanteRepository { public Task AgregarAsync(Comprobante c, CancellationToken t = default) => Task.CompletedTask; public Task<bool> ExistePorVentaAsync(Guid e, Guid v, CancellationToken t = default) => Task.FromResult(existe); }
}
