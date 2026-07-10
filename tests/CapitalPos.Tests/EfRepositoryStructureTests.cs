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

    [Fact]
    public void Ef_usuario_empresa_repository_filtra_por_usuario_y_empresa_en_metodos_de_pertenencia()
    {
        var source = File.ReadAllText(ResolverRutaRepo(
            "src/CapitalPos.Infrastructure/Persistence/Repositories/EfUsuarioEmpresaRepository.cs"));

        Assert.Contains("ObtenerPorUsuarioYEmpresaAsync", source);
        Assert.Contains("usuarioEmpresa.UsuarioId == usuarioId", source);
        Assert.Contains("usuarioEmpresa.EmpresaId == empresaId", source);
        Assert.Contains("ExisteAsignacionAsync", source);
    }

    private static string ResolverRutaRepo(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "CapitalPos.Api.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(directory.FullName, relativePath);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo resolver la raiz del repositorio.");
    }
}
