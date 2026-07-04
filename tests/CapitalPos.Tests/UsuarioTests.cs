using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class UsuarioTests
{
    [Fact]
    public void Crear_usuario_valido()
    {
        var id = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var usuario = new Usuario(
            id,
            " Ada ",
            " Lovelace ",
            " ADA@CAPITALPOS.COM ",
            fechaCreacion: fechaCreacion);

        Assert.Equal(id, usuario.Id);
        Assert.Equal("Ada", usuario.Nombre);
        Assert.Equal("Lovelace", usuario.Apellido);
        Assert.Equal("ada@capitalpos.com", usuario.Correo);
        Assert.True(usuario.Activo);
        Assert.Equal(fechaCreacion, usuario.FechaCreacion);
    }

    [Fact]
    public void Rechaza_correo_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario(Guid.NewGuid(), "Ada", "Lovelace", " "));
    }
}
