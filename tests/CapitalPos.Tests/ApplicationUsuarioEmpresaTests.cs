using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationUsuarioEmpresaTests
{
    [Fact]
    public void Crear_usuario_use_case_construye_usuario_valido()
    {
        var useCase = new CrearUsuarioUseCase();
        var request = new CrearUsuarioRequest(
            "Grace",
            "Hopper",
            " GRACE@CAPITALPOS.COM ");

        var usuario = useCase.Ejecutar(request);

        Assert.NotEqual(Guid.Empty, usuario.Id);
        Assert.Equal("Grace", usuario.Nombre);
        Assert.Equal("Hopper", usuario.Apellido);
        Assert.Equal("grace@capitalpos.com", usuario.Correo);
        Assert.True(usuario.Activo);
    }

    [Fact]
    public void Crear_usuario_use_case_propaga_reglas_de_dominio()
    {
        var useCase = new CrearUsuarioUseCase();
        var request = new CrearUsuarioRequest("Grace", "Hopper", "");

        Assert.Throws<ArgumentException>(() => useCase.Ejecutar(request));
    }

    [Fact]
    public void Asignar_usuario_empresa_use_case_construye_relacion_valida()
    {
        var useCase = new AsignarUsuarioEmpresaUseCase();
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var request = new AsignarUsuarioEmpresaRequest(
            usuarioId,
            empresaId,
            RolEmpresa.Cajero);

        var asignacion = useCase.Ejecutar(request);

        Assert.NotEqual(Guid.Empty, asignacion.Id);
        Assert.Equal(usuarioId, asignacion.UsuarioId);
        Assert.Equal(empresaId, asignacion.EmpresaId);
        Assert.Equal(RolEmpresa.Cajero, asignacion.Rol);
        Assert.True(asignacion.Activo);
    }

    [Fact]
    public void Asignar_usuario_empresa_use_case_propaga_reglas_de_dominio()
    {
        var useCase = new AsignarUsuarioEmpresaUseCase();
        var request = new AsignarUsuarioEmpresaRequest(
            Guid.Empty,
            Guid.NewGuid(),
            RolEmpresa.Vendedor);

        Assert.Throws<ArgumentException>(() => useCase.Ejecutar(request));
    }
}
