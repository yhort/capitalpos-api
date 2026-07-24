using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ReglaPrecioMayoristaTests
{
    [Fact]
    public void Crear_regla_valida()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var fecha = DateTimeOffset.UtcNow;

        var regla = new ReglaPrecioMayorista(
            id,
            empresaId,
            productoId,
            12,
            35m,
            fechaCreacion: fecha);

        Assert.Equal(id, regla.Id);
        Assert.Equal(empresaId, regla.EmpresaId);
        Assert.Equal(productoId, regla.ProductoId);
        Assert.Equal(12, regla.CantidadMinima);
        Assert.Equal(35m, regla.PrecioUnitarioMayorista);
        Assert.True(regla.Activa);
        Assert.Equal(fecha, regla.FechaCreacion);
    }

    [Fact]
    public void Rechaza_identificadores_obligatorios_vacios()
    {
        Assert.Throws<ArgumentException>(() =>
            new ReglaPrecioMayorista(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 12, 35m));
        Assert.Throws<ArgumentException>(() =>
            new ReglaPrecioMayorista(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 12, 35m));
        Assert.Throws<ArgumentException>(() =>
            new ReglaPrecioMayorista(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 12, 35m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_cantidad_minima_no_positiva(int cantidadMinima)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReglaPrecioMayorista(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cantidadMinima, 35m));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Rechaza_precio_mayorista_no_positivo(string precio)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReglaPrecioMayorista(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 12, decimal.Parse(precio)));
    }

    [Fact]
    public void Permite_activar_y_desactivar()
    {
        var regla = new ReglaPrecioMayorista(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 12, 35m);

        regla.Desactivar();
        Assert.False(regla.Activa);

        regla.Activar();
        Assert.True(regla.Activa);
    }
}
