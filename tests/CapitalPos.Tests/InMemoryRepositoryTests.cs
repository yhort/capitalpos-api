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
}
