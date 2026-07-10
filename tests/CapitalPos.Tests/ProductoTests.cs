using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ProductoTests
{
    [Fact]
    public void Crear_producto_valido()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var producto = new Producto(
            id,
            empresaId,
            " Cafe Americano ",
            8.50m,
            " SKU-001 ",
            " 7750000000012 ",
            3.25m,
            fechaCreacion: fechaCreacion);

        Assert.Equal(id, producto.Id);
        Assert.Equal(empresaId, producto.EmpresaId);
        Assert.Equal("Cafe Americano", producto.Nombre);
        Assert.Equal("SKU-001", producto.CodigoSku);
        Assert.Equal("7750000000012", producto.CodigoBarras);
        Assert.Equal(8.50m, producto.PrecioVenta);
        Assert.Equal(3.25m, producto.Costo);
        Assert.True(producto.Activo);
        Assert.Equal(fechaCreacion, producto.FechaCreacion);
    }

    [Fact]
    public void Permite_codigos_y_costo_opcionales()
    {
        var producto = new Producto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Producto sin codigos",
            10m);

        Assert.Equal(string.Empty, producto.CodigoSku);
        Assert.Equal(string.Empty, producto.CodigoBarras);
        Assert.Null(producto.Costo);
    }

    [Fact]
    public void Rechaza_identificador_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Producto(
                Guid.Empty,
                Guid.NewGuid(),
                "Producto",
                10m));
    }

    [Fact]
    public void Rechaza_empresa_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Producto(
                Guid.NewGuid(),
                Guid.Empty,
                "Producto",
                10m));
    }

    [Fact]
    public void Rechaza_nombre_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Producto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                " ",
                10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_precio_venta_no_positivo(decimal precioVenta)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Producto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Producto",
                precioVenta));
    }

    [Fact]
    public void Rechaza_costo_negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Producto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Producto",
                10m,
                costo: -0.01m));
    }

    [Fact]
    public void Actualizar_datos_basicos_normaliza_y_actualiza_campos()
    {
        var producto = new Producto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Producto",
            10m);

        producto.ActualizarDatosBasicos(
            " Cafe Latte ",
            12.50m,
            " SKU-002 ",
            " 7750000000029 ",
            4.10m);

        Assert.Equal("Cafe Latte", producto.Nombre);
        Assert.Equal("SKU-002", producto.CodigoSku);
        Assert.Equal("7750000000029", producto.CodigoBarras);
        Assert.Equal(12.50m, producto.PrecioVenta);
        Assert.Equal(4.10m, producto.Costo);
    }

    [Fact]
    public void Activar_y_desactivar_cambian_estado()
    {
        var producto = new Producto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Producto",
            10m);

        producto.Desactivar();
        Assert.False(producto.Activo);

        producto.Activar();
        Assert.True(producto.Activo);
    }
}
