using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class MarcaTests
{
    [Fact]
    public void Crear_marca_valida_normaliza_nombre()
    {
        var empresaId = Guid.NewGuid();
        var fecha = DateTimeOffset.UtcNow;

        var marca = new Marca(
            Guid.NewGuid(),
            empresaId,
            "  Brooklyn  ",
            fechaCreacion: fecha);

        Assert.Equal(empresaId, marca.EmpresaId);
        Assert.Equal("Brooklyn", marca.Nombre);
        Assert.True(marca.Activa);
        Assert.Equal(fecha, marca.FechaCreacion);
    }

    [Fact]
    public void Crear_marca_exige_id()
    {
        Assert.Throws<ArgumentException>(() =>
            new Marca(Guid.Empty, Guid.NewGuid(), "Brooklyn"));
    }

    [Fact]
    public void Crear_marca_exige_empresa()
    {
        Assert.Throws<ArgumentException>(() =>
            new Marca(Guid.NewGuid(), Guid.Empty, "Brooklyn"));
    }

    [Fact]
    public void Crear_marca_exige_nombre()
    {
        Assert.Throws<ArgumentException>(() =>
            new Marca(Guid.NewGuid(), Guid.NewGuid(), " "));
    }

    [Fact]
    public void Activar_y_desactivar_marca_cambia_estado()
    {
        var marca = new Marca(Guid.NewGuid(), Guid.NewGuid(), "Brooklyn");

        marca.Desactivar();
        Assert.False(marca.Activa);

        marca.Activar();
        Assert.True(marca.Activa);
    }
}
