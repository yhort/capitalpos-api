using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class StockProductoTests
{
    private static readonly Guid SedeIdPrueba = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Crear_stock_producto_valido()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;
        var fechaActualizacion = fechaCreacion.AddMinutes(1);

        var stock = new StockProducto(
            id,
            empresaId,
            SedeIdPrueba,
            productoId,
            varianteId,
            10m,
            2m,
            fechaCreacion,
            fechaActualizacion);

        Assert.Equal(id, stock.Id);
        Assert.Equal(empresaId, stock.EmpresaId);
        Assert.Equal(SedeIdPrueba, stock.SedeId);
        Assert.Equal(productoId, stock.ProductoId);
        Assert.Equal(varianteId, stock.ProductoVarianteId);
        Assert.Equal(10m, stock.CantidadDisponible);
        Assert.Equal(2m, stock.CantidadReservada);
        Assert.Equal(8m, stock.CantidadLibre);
        Assert.Equal(fechaCreacion, stock.FechaCreacion);
        Assert.Equal(fechaActualizacion, stock.FechaActualizacion);
    }

    [Fact]
    public void Rechaza_empresa_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new StockProducto(
                Guid.NewGuid(),
                Guid.Empty,
                SedeIdPrueba,
                Guid.NewGuid(),
                null,
                10m));
    }

    [Fact]
    public void Rechaza_sede_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new StockProducto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                null,
                10m));
    }

    [Fact]
    public void Rechaza_producto_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new StockProducto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                SedeIdPrueba,
                Guid.Empty,
                null,
                10m));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10, -1)]
    public void Rechaza_cantidades_negativas(decimal disponible, decimal reservada)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StockProducto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                disponible,
                reservada));
    }

    [Fact]
    public void Rechaza_reserva_mayor_a_disponible()
    {
        Assert.Throws<ArgumentException>(() =>
            new StockProducto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                5m,
                6m));
    }

    [Fact]
    public void Incrementar_aumenta_stock_disponible()
    {
        var stock = CrearStock(cantidadDisponible: 10m);

        stock.Incrementar(3.5m);

        Assert.Equal(13.5m, stock.CantidadDisponible);
    }

    [Fact]
    public void Descontar_falla_si_no_hay_stock_suficiente()
    {
        var stock = CrearStock(cantidadDisponible: 10m, cantidadReservada: 2m);

        Assert.Throws<InvalidOperationException>(() => stock.Descontar(9m));
    }

    [Fact]
    public void Descontar_reduce_stock_correctamente()
    {
        var stock = CrearStock(cantidadDisponible: 10m, cantidadReservada: 2m);

        stock.Descontar(3m);

        Assert.Equal(7m, stock.CantidadDisponible);
        Assert.Equal(2m, stock.CantidadReservada);
        Assert.Equal(5m, stock.CantidadLibre);
    }

    [Fact]
    public void Reservar_y_liberar_reserva_actualizan_cantidad_reservada()
    {
        var stock = CrearStock(cantidadDisponible: 10m);

        stock.Reservar(4m);
        stock.LiberarReserva(1.5m);

        Assert.Equal(2.5m, stock.CantidadReservada);
        Assert.Equal(7.5m, stock.CantidadLibre);
    }

    private static StockProducto CrearStock(
        decimal cantidadDisponible,
        decimal cantidadReservada = 0)
    {
        return new StockProducto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SedeIdPrueba,
            Guid.NewGuid(),
            null,
            cantidadDisponible,
            cantidadReservada);
    }
}
