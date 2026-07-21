using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class UnidadMedidaTests
{
    [Fact]
    public void Crear_unidad_medida_valida()
    {
        var id = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var unidad = new UnidadMedida(id, " und ", " Unidad ", fechaCreacion: fechaCreacion);

        Assert.Equal(id, unidad.Id);
        Assert.Equal("UND", unidad.Codigo);
        Assert.Equal("Unidad", unidad.Nombre);
        Assert.True(unidad.Activa);
        Assert.Equal(fechaCreacion, unidad.FechaCreacion);
    }

    [Fact]
    public void Rechaza_datos_obligatorios_invalidos()
    {
        Assert.Throws<ArgumentException>(() => new UnidadMedida(Guid.Empty, "UND", "Unidad"));
        Assert.Throws<ArgumentException>(() => new UnidadMedida(Guid.NewGuid(), " ", "Unidad"));
        Assert.Throws<ArgumentException>(() => new UnidadMedida(Guid.NewGuid(), "UND", " "));
    }

    [Fact]
    public void Activar_y_desactivar_cambian_estado()
    {
        var unidad = new UnidadMedida(Guid.NewGuid(), "UND", "Unidad");

        unidad.Desactivar();
        Assert.False(unidad.Activa);

        unidad.Activar();
        Assert.True(unidad.Activa);
    }
}
