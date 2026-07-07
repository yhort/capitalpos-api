using CapitalPos.Application.Empresas;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Usuarios;
using CapitalPos.Infrastructure.Persistence.Repositories;

namespace CapitalPos.Tests;

public class EfRepositoryStructureTests
{
    [Fact]
    public void Ef_empresa_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IEmpresaRepository).IsAssignableFrom(typeof(EfEmpresaRepository)));
    }

    [Fact]
    public void Ef_usuario_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IUsuarioRepository).IsAssignableFrom(typeof(EfUsuarioRepository)));
    }

    [Fact]
    public void Ef_usuario_empresa_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IUsuarioEmpresaRepository).IsAssignableFrom(typeof(EfUsuarioEmpresaRepository)));
    }

    [Fact]
    public void Ef_usuario_credencial_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IUsuarioCredencialRepository).IsAssignableFrom(typeof(EfUsuarioCredencialRepository)));
    }
}
