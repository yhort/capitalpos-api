using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationUsuarioEmpresaTests
{
    [Fact]
    public async Task Crear_usuario_use_case_construye_y_guarda_usuario_valido()
    {
        var repository = new UsuarioRepositoryFake();
        var useCase = new CrearUsuarioUseCase(repository);
        var request = new CrearUsuarioRequest(
            "Grace",
            "Hopper",
            " GRACE@CAPITALPOS.COM ");

        var usuario = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, usuario.Id);
        Assert.Equal("Grace", usuario.Nombre);
        Assert.Equal("Hopper", usuario.Apellido);
        Assert.Equal("grace@capitalpos.com", usuario.Correo);
        Assert.True(usuario.Activo);
        Assert.Same(usuario, repository.Usuarios.Single());
    }

    [Fact]
    public async Task Crear_usuario_use_case_propaga_reglas_de_dominio()
    {
        var repository = new UsuarioRepositoryFake();
        var useCase = new CrearUsuarioUseCase(repository);
        var request = new CrearUsuarioRequest("Grace", "Hopper", "");

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Usuarios);
    }

    [Fact]
    public async Task Asignar_usuario_empresa_use_case_construye_y_guarda_relacion_valida()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var useCase = new AsignarUsuarioEmpresaUseCase(repository);
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var request = new AsignarUsuarioEmpresaRequest(
            usuarioId,
            empresaId,
            RolEmpresa.Cajero);

        var asignacion = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, asignacion.Id);
        Assert.Equal(usuarioId, asignacion.UsuarioId);
        Assert.Equal(empresaId, asignacion.EmpresaId);
        Assert.Equal(RolEmpresa.Cajero, asignacion.Rol);
        Assert.True(asignacion.Activo);
        Assert.Same(asignacion, repository.Asignaciones.Single());
    }

    [Fact]
    public async Task Asignar_usuario_empresa_use_case_propaga_reglas_de_dominio()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var useCase = new AsignarUsuarioEmpresaUseCase(repository);
        var request = new AsignarUsuarioEmpresaRequest(
            Guid.Empty,
            Guid.NewGuid(),
            RolEmpresa.Vendedor);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Asignaciones);
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public List<Usuario> Usuarios { get; } = new();

        public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Usuarios.Add(usuario);

            return Task.CompletedTask;
        }
    }

    private sealed class UsuarioEmpresaRepositoryFake : IUsuarioEmpresaRepository
    {
        public List<UsuarioEmpresa> Asignaciones { get; } = new();

        public Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            Asignaciones.Add(usuarioEmpresa);

            return Task.CompletedTask;
        }
    }
}
