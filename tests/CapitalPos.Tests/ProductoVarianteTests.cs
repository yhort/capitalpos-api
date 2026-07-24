using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ProductoVarianteTests
{
    [Fact]
    public void Crear_variante_valida()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var variante = new ProductoVariante(
            id,
            empresaId,
            productoId,
            " M ",
            " Azul ",
            " SKU-AZ-M ",
            " 7750000000104 ",
            fechaCreacion: fechaCreacion);

        Assert.Equal(id, variante.Id);
        Assert.Equal(empresaId, variante.EmpresaId);
        Assert.Equal(productoId, variante.ProductoId);
        Assert.Equal("M", variante.Talla);
        Assert.Equal("Azul", variante.Color);
        Assert.Equal("SKU-AZ-M", variante.CodigoSku);
        Assert.Equal("7750000000104", variante.CodigoBarras);
        Assert.True(variante.Activo);
        Assert.Equal(fechaCreacion, variante.FechaCreacion);
    }

    [Fact]
    public void Permite_variante_solo_con_talla_o_color()
    {
        var variante = new ProductoVariante(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            talla: "L");

        Assert.Equal("L", variante.Talla);
        Assert.Equal(string.Empty, variante.Color);
        Assert.Equal(string.Empty, variante.CodigoSku);
        Assert.Equal(string.Empty, variante.CodigoBarras);
    }

    [Fact]
    public void Rechaza_identificador_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductoVariante(
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                talla: "M"));
    }

    [Fact]
    public void Rechaza_empresa_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductoVariante(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                talla: "M"));
    }

    [Fact]
    public void Rechaza_producto_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductoVariante(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty,
                talla: "M"));
    }

    [Fact]
    public void Rechaza_variante_sin_datos_distintivos()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductoVariante(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    [Fact]
    public void Actualizar_datos_basicos_normaliza_campos()
    {
        var variante = new ProductoVariante(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            talla: "M");

        variante.ActualizarDatosBasicos(
            " L ",
            " Negro ",
            " SKU-NEG-L ",
            " 7750000000111 ");

        Assert.Equal("L", variante.Talla);
        Assert.Equal("Negro", variante.Color);
        Assert.Equal("SKU-NEG-L", variante.CodigoSku);
        Assert.Equal("7750000000111", variante.CodigoBarras);
    }

    [Fact]
    public void Activar_y_desactivar_cambian_estado()
    {
        var variante = new ProductoVariante(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            talla: "M");

        variante.Desactivar();
        Assert.False(variante.Activo);

        variante.Activar();
        Assert.True(variante.Activo);
    }

    [Fact]
    public void Variante_no_expone_stock_actual_ni_actualizar_stock()
    {
        Assert.Null(typeof(ProductoVariante).GetProperty("StockActual"));
        Assert.Null(typeof(ProductoVariante).GetMethod("ActualizarStock"));
    }
}
