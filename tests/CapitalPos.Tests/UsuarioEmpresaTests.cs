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
}
