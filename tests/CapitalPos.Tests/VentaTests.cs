using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class VentaTests
{
    [Fact]
    public void Crear_venta_valida_con_detalle()
    {
        var ventaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var fecha = DateTimeOffset.UtcNow;
        var fechaCreacion = DateTimeOffset.UtcNow;
        var detalle = CrearDetalle(ventaId, empresaId, total: 118m, igv: 18m);

        var venta = new Venta(
            ventaId,
            empresaId,
            fecha,
            100m,
            18m,
            118m,
            [detalle],
            clienteId,
            fechaCreacion: fechaCreacion);

        Assert.Equal(ventaId, venta.Id);
        Assert.Equal(empresaId, venta.EmpresaId);
        Assert.Equal(clienteId, venta.ClienteId);
        Assert.Equal(CanalVenta.TIENDA, venta.CanalVenta);
        Assert.Null(venta.PuntoVentaId);
        Assert.Null(venta.VendedorId);
        Assert.Equal(fecha, venta.Fecha);
        Assert.Equal(100m, venta.Subtotal);
        Assert.Equal(18m, venta.Igv);
        Assert.Equal(118m, venta.Total);
        Assert.Equal(EstadoVenta.Registrada, venta.Estado);
        Assert.Equal(fechaCreacion, venta.FechaCreacion);
        Assert.Same(detalle, Assert.Single(venta.Detalles));
    }

    [Fact]
    public void Permite_cliente_opcional()
    {
        var ventaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var detalle = CrearDetalle(ventaId, empresaId, total: 10m, igv: 0m);

        var venta = new Venta(
            ventaId,
            empresaId,
            DateTimeOffset.UtcNow,
            10m,
            0m,
            10m,
            [detalle]);

        Assert.Null(venta.ClienteId);
        Assert.Equal(CanalVenta.TIENDA, venta.CanalVenta);
    }

    [Fact]
    public void Permite_dimensiones_comerciales_opcionales()
    {
        var ventaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var puntoVentaId = Guid.NewGuid();
        var vendedorId = Guid.NewGuid();
        var detalle = CrearDetalle(ventaId, empresaId, total: 10m, igv: 0m);

        var venta = new Venta(
            ventaId,
            empresaId,
            DateTimeOffset.UtcNow,
            10m,
            0m,
            10m,
            [detalle],
            canalVenta: CanalVenta.PROVINCIA,
            puntoVentaId: puntoVentaId,
            vendedorId: vendedorId);

        Assert.Equal(CanalVenta.PROVINCIA, venta.CanalVenta);
        Assert.Equal(puntoVentaId, venta.PuntoVentaId);
        Assert.Equal(vendedorId, venta.VendedorId);
    }

    [Fact]
    public void Rechaza_canal_venta_invalido()
    {
        var ventaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Venta(
                ventaId,
                empresaId,
                DateTimeOffset.UtcNow,
                10m,
                0m,
                10m,
                [CrearDetalle(ventaId, empresaId, total: 10m, igv: 0m)],
                canalVenta: (CanalVenta)999));
    }

    [Fact]
    public void Rechaza_empresa_id_vacio()
    {
        var ventaId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new Venta(
                ventaId,
                Guid.Empty,
                DateTimeOffset.UtcNow,
                10m,
                0m,
                10m,
                [CrearDetalle(ventaId, Guid.NewGuid(), total: 10m, igv: 0m)]));
    }

    [Fact]
    public void Rechaza_venta_sin_detalles()
    {
        Assert.Throws<ArgumentException>(() =>
            new Venta(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                10m,
                0m,
                10m,
                []));
    }

    [Fact]
    public void Rechaza_detalle_de_otra_empresa_o_venta()
    {
        var ventaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var detalle = CrearDetalle(Guid.NewGuid(), empresaId, total: 10m, igv: 0m);

        Assert.Throws<ArgumentException>(() =>
            new Venta(
                ventaId,
                empresaId,
                DateTimeOffset.UtcNow,
                10m,
                0m,
                10m,
                [detalle]));
    }

    [Fact]
    public void Rechaza_totales_que_no_coinciden_con_detalles()
    {
        var ventaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var detalle = CrearDetalle(ventaId, empresaId, total: 118m, igv: 18m);

        Assert.Throws<ArgumentException>(() =>
            new Venta(
                ventaId,
                empresaId,
                DateTimeOffset.UtcNow,
                90m,
                18m,
                118m,
                [detalle]));
    }

    [Fact]
    public void Anular_cambia_estado()
    {
        var ventaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var venta = new Venta(
            ventaId,
            empresaId,
            DateTimeOffset.UtcNow,
            10m,
            0m,
            10m,
            [CrearDetalle(ventaId, empresaId, total: 10m, igv: 0m)]);

        venta.Anular();

        Assert.Equal(EstadoVenta.Anulada, venta.Estado);
    }

    private static VentaDetalle CrearDetalle(
        Guid ventaId,
        Guid empresaId,
        decimal total,
        decimal igv)
    {
        return new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            Guid.NewGuid(),
            1m,
            total - igv,
            igv,
            total);
    }
}
