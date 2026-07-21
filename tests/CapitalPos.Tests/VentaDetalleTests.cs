using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class VentaDetalleTests
{
    [Fact]
    public void Crear_detalle_valido()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var productoVarianteId = Guid.NewGuid();
        var productoPresentacionId = Guid.NewGuid();

        var detalle = new VentaDetalle(
            id,
            empresaId,
            ventaId,
            productoId,
            2m,
            50m,
            18m,
            118m,
            productoVarianteId,
            productoPresentacionId);

        Assert.Equal(id, detalle.Id);
        Assert.Equal(empresaId, detalle.EmpresaId);
        Assert.Equal(ventaId, detalle.VentaId);
        Assert.Equal(productoId, detalle.ProductoId);
        Assert.Equal(productoVarianteId, detalle.ProductoVarianteId);
        Assert.Equal(productoPresentacionId, detalle.ProductoPresentacionId);
        Assert.Equal(2m, detalle.Cantidad);
        Assert.Equal(50m, detalle.PrecioUnitario);
        Assert.Equal(18m, detalle.Igv);
        Assert.Equal(118m, detalle.Total);
    }

    [Fact]
    public void Permite_variante_opcional()
    {
        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1m,
            10m,
            0m,
            10m);

        Assert.Null(detalle.ProductoVarianteId);
        Assert.Null(detalle.ProductoPresentacionId);
    }

    [Fact]
    public void Rechaza_presentacion_id_vacia()
    {
        Assert.Throws<ArgumentException>(() =>
            new VentaDetalle(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                1m,
                10m,
                0m,
                10m,
                productoPresentacionId: Guid.Empty));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_cantidad_no_positiva(decimal cantidad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new VentaDetalle(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                cantidad,
                10m,
                0m,
                10m));
    }

    [Fact]
    public void Rechaza_igv_negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new VentaDetalle(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                1m,
                10m,
                -0.01m,
                10m));
    }
}
