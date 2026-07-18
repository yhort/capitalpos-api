using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class PuntoVentaTests
{
    [Fact]
    public void Crear_punto_venta_valido_asigna_empresa_sede_y_datos()
    {
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var fecha = new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

        var puntoVenta = new PuntoVenta(
            Guid.NewGuid(),
            empresaId,
            sedeId,
            " Caja Principal ",
            fechaCreacion: fecha);

        Assert.Equal(empresaId, puntoVenta.EmpresaId);
        Assert.Equal(sedeId, puntoVenta.SedeId);
        Assert.Equal("Caja Principal", puntoVenta.Nombre);
        Assert.True(puntoVenta.Activo);
        Assert.Equal(fecha, puntoVenta.FechaCreacion);
    }

    [Fact]
    public void Crear_punto_venta_exige_empresa()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new PuntoVenta(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "Caja"));

        Assert.Contains("empresa", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_punto_venta_exige_sede()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new PuntoVenta(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Caja"));

        Assert.Contains("sede", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_punto_venta_exige_nombre()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new PuntoVenta(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " "));

        Assert.Contains("nombre", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Activar_y_desactivar_punto_venta_cambia_estado()
    {
        var puntoVenta = new PuntoVenta(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Caja");

        puntoVenta.Desactivar();
        Assert.False(puntoVenta.Activo);

        puntoVenta.Activar();
        Assert.True(puntoVenta.Activo);
    }
}
