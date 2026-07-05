using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class UsuarioEmpresaTests
{
    [Fact]
    public void Crear_relacion_usuario_empresa_valida()
    {
        var id = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var fechaAsignacion = DateTimeOffset.UtcNow;

        var usuarioEmpresa = new UsuarioEmpresa(
            id,
            usuarioId,
            empresaId,
            RolEmpresa.Administrador,
            fechaAsignacion: fechaAsignacion);

        Assert.Equal(id, usuarioEmpresa.Id);
        Assert.Equal(usuarioId, usuarioEmpresa.UsuarioId);
        Assert.Equal(empresaId, usuarioEmpresa.EmpresaId);
        Assert.Equal(RolEmpresa.Administrador, usuarioEmpresa.Rol);
        Assert.True(usuarioEmpresa.Activo);
        Assert.Equal(fechaAsignacion, usuarioEmpresa.FechaAsignacion);
    }

    [Fact]
    public void Rechaza_identificador_de_usuario_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new UsuarioEmpresa(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                RolEmpresa.Cajero));
    }

    [Fact]
    public void Rechaza_identificador_de_empresa_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new UsuarioEmpresa(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty,
                RolEmpresa.Cajero));
    }

    [Fact]
    public void Cambiar_rol_actualiza_rol_de_la_relacion()
    {
        var usuarioEmpresa = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);

        usuarioEmpresa.CambiarRol(RolEmpresa.Almacenero);

        Assert.Equal(RolEmpresa.Almacenero, usuarioEmpresa.Rol);
    }

    [Fact]
    public void Cambiar_rol_rechaza_rol_invalido()
    {
        var usuarioEmpresa = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            usuarioEmpresa.CambiarRol((RolEmpresa)999));
    }
}
