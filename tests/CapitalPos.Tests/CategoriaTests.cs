using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class CategoriaTests
{
    [Fact]
    public void Crear_categoria_valida_normaliza_nombre()
    {
        var empresaId = Guid.NewGuid();
        var padreId = Guid.NewGuid();
        var fecha = DateTimeOffset.UtcNow;

        var categoria = new Categoria(
            Guid.NewGuid(),
            empresaId,
            "  Polos  ",
            padreId,
            fechaCreacion: fecha);

        Assert.Equal(empresaId, categoria.EmpresaId);
        Assert.Equal(padreId, categoria.CategoriaPadreId);
        Assert.Equal("Polos", categoria.Nombre);
        Assert.True(categoria.Activa);
        Assert.Equal(fecha, categoria.FechaCreacion);
    }

    [Fact]
    public void Crear_categoria_exige_id()
    {
        Assert.Throws<ArgumentException>(() =>
            new Categoria(Guid.Empty, Guid.NewGuid(), "Polos"));
    }

    [Fact]
    public void Crear_categoria_exige_empresa()
    {
        Assert.Throws<ArgumentException>(() =>
            new Categoria(Guid.NewGuid(), Guid.Empty, "Polos"));
    }

    [Fact]
    public void Crear_categoria_exige_nombre()
    {
        Assert.Throws<ArgumentException>(() =>
            new Categoria(Guid.NewGuid(), Guid.NewGuid(), " "));
    }

    [Fact]
    public void Crear_categoria_rechaza_categoria_padre_vacia()
    {
        Assert.Throws<ArgumentException>(() =>
            new Categoria(Guid.NewGuid(), Guid.NewGuid(), "Polos", Guid.Empty));
    }

    [Fact]
    public void Activar_y_desactivar_categoria_cambia_estado()
    {
        var categoria = new Categoria(Guid.NewGuid(), Guid.NewGuid(), "Polos");

        categoria.Desactivar();
        Assert.False(categoria.Activa);

        categoria.Activar();
        Assert.True(categoria.Activa);
    }
}
