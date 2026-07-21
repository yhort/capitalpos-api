namespace CapitalPos.Tests;

public class EndpointPermissionStructureTests
{
    [Theory]
    [InlineData("src/CapitalPos.Api/Endpoints/EmpresaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UsuarioEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/CatalogoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UnidadMedidaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ProductoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/StockEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/SedeEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ClienteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/VentaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ReporteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/DashboardEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ConfiguracionFiscalEndpoints.cs")]
    public void Endpoints_piden_permisos_explicitamente(string relativePath)
    {
        var source = File.ReadAllText(ResolverRutaRepo(relativePath));

        Assert.Contains("RequirePermisoEmpresa(PermisoEmpresa.", source);
    }

    [Theory]
    [InlineData("src/CapitalPos.Api/Endpoints/EmpresaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UsuarioEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/CatalogoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UnidadMedidaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ProductoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/StockEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/SedeEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ClienteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/VentaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ReporteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/DashboardEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ConfiguracionFiscalEndpoints.cs")]
    public void Endpoints_no_comparan_roles_directamente_para_autorizar(string relativePath)
    {
        var source = File.ReadAllText(ResolverRutaRepo(relativePath));

        Assert.DoesNotContain(".Rol ==", source);
        Assert.DoesNotContain(".Rol !=", source);
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
