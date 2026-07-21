using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ProductoPresentacionTests
{
    [Fact]
    public void Crear_presentacion_valida()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var unidadMedidaId = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var presentacion = new ProductoPresentacion(
            id,
            empresaId,
            productoId,
            unidadMedidaId,
            12m,
            esUnidadBase: false,
            100m,
            " 7750000000104 ",
            fechaCreacion: fechaCreacion);

        Assert.Equal(id, presentacion.Id);
        Assert.Equal(empresaId, presentacion.EmpresaId);
        Assert.Equal(productoId, presentacion.ProductoId);
        Assert.Equal(unidadMedidaId, presentacion.UnidadMedidaId);
        Assert.Equal(12m, presentacion.FactorConversion);
        Assert.False(presentacion.EsUnidadBase);
        Assert.Equal(100m, presentacion.PrecioVenta);
        Assert.Equal("7750000000104", presentacion.CodigoBarras);
        Assert.True(presentacion.Activa);
        Assert.Equal(fechaCreacion, presentacion.FechaCreacion);
    }

    [Fact]
    public void Permite_codigo_barras_opcional()
    {
        var presentacion = new ProductoPresentacion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1m,
            esUnidadBase: true,
            10m);

        Assert.Equal(string.Empty, presentacion.CodigoBarras);
    }

    [Fact]
    public void Rechaza_identificadores_vacios()
    {
        Assert.Throws<ArgumentException>(() => new ProductoPresentacion(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1m,
            true,
            10m));
        Assert.Throws<ArgumentException>(() => new ProductoPresentacion(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            1m,
            true,
            10m));
        Assert.Throws<ArgumentException>(() => new ProductoPresentacion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            1m,
            true,
            10m));
        Assert.Throws<ArgumentException>(() => new ProductoPresentacion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            1m,
            true,
            10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_factor_conversion_no_positivo(decimal factorConversion)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProductoPresentacion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            factorConversion,
            true,
            10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_precio_venta_no_positivo(decimal precioVenta)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProductoPresentacion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1m,
            true,
            precioVenta));
    }

    [Fact]
    public void Activar_y_desactivar_cambian_estado()
    {
        var presentacion = new ProductoPresentacion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1m,
            true,
            10m);

        presentacion.Desactivar();
        Assert.False(presentacion.Activa);

        presentacion.Activar();
        Assert.True(presentacion.Activa);
    }
}
