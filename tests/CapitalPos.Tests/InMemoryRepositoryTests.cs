using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence.InMemory;

namespace CapitalPos.Tests;

public class InMemoryRepositoryTests
{
    [Fact]
    public async Task Empresa_repository_guarda_empresa_en_memoria()
    {
        var repository = new InMemoryEmpresaRepository();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC");

        await repository.AgregarAsync(empresa);

        Assert.Same(empresa, repository.Empresas.Single());
    }

    [Fact]
    public async Task Empresa_repository_lista_empresas_guardadas()
    {
        var repository = new InMemoryEmpresaRepository();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC");
        await repository.AgregarAsync(empresa);

        var empresas = await repository.ListarAsync();

        Assert.Same(empresa, empresas.Single());
    }

    [Fact]
    public async Task Empresa_repository_obtiene_empresa_por_id()
    {
        var repository = new InMemoryEmpresaRepository();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC");
        await repository.AgregarAsync(empresa);

        var empresaEncontrada = await repository.ObtenerPorIdAsync(empresa.Id);

        Assert.Same(empresa, empresaEncontrada);
    }

    [Fact]
    public async Task Empresa_repository_devuelve_null_si_no_existe()
    {
        var repository = new InMemoryEmpresaRepository();

        var empresa = await repository.ObtenerPorIdAsync(Guid.NewGuid());

        Assert.Null(empresa);
    }

    [Fact]
    public async Task Empresa_repository_actualiza_empresa_existente()
    {
        var repository = new InMemoryEmpresaRepository();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC");
        await repository.AgregarAsync(empresa);
        empresa.Desactivar();

        await repository.ActualizarAsync(empresa);

        Assert.False(repository.Empresas.Single().Activa);
    }

    [Fact]
    public async Task Empresa_repository_rechaza_actualizar_empresa_inexistente()
    {
        var repository = new InMemoryEmpresaRepository();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC");

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ActualizarAsync(empresa));
    }

    [Fact]
    public async Task Empresa_repository_indica_si_existe_ruc()
    {
        var repository = new InMemoryEmpresaRepository();
        await repository.AgregarAsync(new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC"));

        var existe = await repository.ExisteRucAsync("20606264004");

        Assert.True(existe);
    }

    [Fact]
    public async Task Usuario_repository_guarda_usuario_en_memoria()
    {
        var repository = new InMemoryUsuarioRepository();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com");

        await repository.AgregarAsync(usuario);

        Assert.Same(usuario, repository.Usuarios.Single());
    }

    [Fact]
    public async Task Usuario_repository_lista_usuarios_guardados()
    {
        var repository = new InMemoryUsuarioRepository();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com");
        await repository.AgregarAsync(usuario);

        var usuarios = await repository.ListarAsync();

        Assert.Same(usuario, usuarios.Single());
    }

    [Fact]
    public async Task Usuario_repository_obtiene_usuario_por_id()
    {
        var repository = new InMemoryUsuarioRepository();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com");
        await repository.AgregarAsync(usuario);

        var usuarioEncontrado = await repository.ObtenerPorIdAsync(usuario.Id);

        Assert.Same(usuario, usuarioEncontrado);
    }

    [Fact]
    public async Task Usuario_repository_devuelve_null_si_no_existe()
    {
        var repository = new InMemoryUsuarioRepository();

        var usuario = await repository.ObtenerPorIdAsync(Guid.NewGuid());

        Assert.Null(usuario);
    }

    [Fact]
    public async Task Usuario_repository_actualiza_usuario_existente()
    {
        var repository = new InMemoryUsuarioRepository();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com");
        await repository.AgregarAsync(usuario);
        usuario.Desactivar();

        await repository.ActualizarAsync(usuario);

        Assert.False(repository.Usuarios.Single().Activo);
    }

    [Fact]
    public async Task Usuario_repository_rechaza_actualizar_usuario_inexistente()
    {
        var repository = new InMemoryUsuarioRepository();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ActualizarAsync(usuario));
    }

    [Fact]
    public async Task Usuario_repository_indica_si_existe_correo()
    {
        var repository = new InMemoryUsuarioRepository();
        await repository.AgregarAsync(new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com"));

        var existe = await repository.ExisteCorreoAsync("grace@capitalpos.com");

        Assert.True(existe);
    }

    [Fact]
    public async Task Usuario_empresa_repository_guarda_asignacion_en_memoria()
    {
        var repository = new InMemoryUsuarioEmpresaRepository();
        var usuarioEmpresa = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);

        await repository.AgregarAsync(usuarioEmpresa);

        Assert.Same(usuarioEmpresa, repository.UsuariosEmpresa.Single());
    }

    [Fact]
    public async Task Usuario_empresa_repository_lista_asignaciones_guardadas()
    {
        var repository = new InMemoryUsuarioEmpresaRepository();
        var usuarioEmpresa = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);
        await repository.AgregarAsync(usuarioEmpresa);

        var usuariosEmpresa = await repository.ListarAsync();

        Assert.Same(usuarioEmpresa, usuariosEmpresa.Single());
    }

    [Fact]
    public async Task Usuario_empresa_repository_obtiene_asignacion_por_id()
    {
        var repository = new InMemoryUsuarioEmpresaRepository();
        var usuarioEmpresa = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);
        await repository.AgregarAsync(usuarioEmpresa);

        var usuarioEmpresaEncontrado = await repository.ObtenerPorIdAsync(usuarioEmpresa.Id);

        Assert.Same(usuarioEmpresa, usuarioEmpresaEncontrado);
    }

    [Fact]
    public async Task Usuario_empresa_repository_devuelve_null_si_no_existe()
    {
        var repository = new InMemoryUsuarioEmpresaRepository();

        var usuarioEmpresa = await repository.ObtenerPorIdAsync(Guid.NewGuid());

        Assert.Null(usuarioEmpresa);
    }

    [Fact]
    public async Task Usuario_empresa_repository_actualiza_asignacion_existente()
    {
        var repository = new InMemoryUsuarioEmpresaRepository();
        var usuarioEmpresa = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);
        await repository.AgregarAsync(usuarioEmpresa);
        usuarioEmpresa.Desactivar();

        await repository.ActualizarAsync(usuarioEmpresa);

        Assert.False(repository.UsuariosEmpresa.Single().Activo);
    }

    [Fact]
    public async Task Usuario_empresa_repository_rechaza_actualizar_asignacion_inexistente()
    {
        var repository = new InMemoryUsuarioEmpresaRepository();
        var usuarioEmpresa = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ActualizarAsync(usuarioEmpresa));
    }

    [Fact]
    public async Task Usuario_empresa_repository_indica_si_existe_asignacion()
    {
        var repository = new InMemoryUsuarioEmpresaRepository();
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        await repository.AgregarAsync(new UsuarioEmpresa(
            Guid.NewGuid(),
            usuarioId,
            empresaId,
            RolEmpresa.Cajero));

        var existe = await repository.ExisteAsignacionAsync(usuarioId, empresaId);

        Assert.True(existe);
    }
}
