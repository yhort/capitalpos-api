namespace CapitalPos.Tests;

public class EndpointProtectionStructureTests
{
    [Theory]
    [InlineData("src/CapitalPos.Api/Endpoints/EmpresaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UsuarioEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ProductoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/StockEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ClienteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/VentaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ReporteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ConfiguracionFiscalEndpoints.cs")]
    public void Grupos_de_endpoints_de_negocio_requieren_autorizacion(string relativePath)
    {
        var source = File.ReadAllText(ResolverRutaRepo(relativePath));

        Assert.Contains(".RequireAuthorization()", source);
    }

    [Theory]
    [InlineData("src/CapitalPos.Api/Endpoints/EmpresaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UsuarioEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ProductoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/StockEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ClienteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/VentaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ReporteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ConfiguracionFiscalEndpoints.cs")]
    public void Grupos_de_endpoints_de_negocio_requieren_empresa_activa(string relativePath)
    {
        var source = File.ReadAllText(ResolverRutaRepo(relativePath));

        Assert.Contains(".AddEndpointFilter<EmpresaActivaEndpointFilter>()", source);
    }

    [Theory]
    [InlineData("src/CapitalPos.Api/Endpoints/EmpresaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UsuarioEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ProductoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/StockEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ClienteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/VentaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ReporteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ConfiguracionFiscalEndpoints.cs")]
    public void Endpoints_de_negocio_requieren_permisos_empresariales(string relativePath)
    {
        var source = File.ReadAllText(ResolverRutaRepo(relativePath));
        var endpointCount =
            Contar(source, ".MapGet(") +
            Contar(source, ".MapPost(") +
            Contar(source, ".MapPut(") +
            Contar(source, ".MapPatch(") +
            Contar(source, ".MapDelete(");
        var permissionCount = Contar(source, ".RequirePermisoEmpresa(PermisoEmpresa.");

        Assert.True(endpointCount > 0);
        Assert.Equal(endpointCount, permissionCount);
    }

    [Fact]
    public void Health_no_requiere_header_de_empresa_activa_ni_permiso_empresarial()
    {
        var source = File.ReadAllText(ResolverRutaRepo("src/CapitalPos.Api/Program.cs"));
        var healthStart = source.IndexOf("app.MapGet(\"/api/health\"", StringComparison.Ordinal);

        Assert.True(healthStart >= 0);
        var healthBlock = source[healthStart..source.IndexOf(";", healthStart, StringComparison.Ordinal)];
        Assert.DoesNotContain("EmpresaActivaEndpointFilter", healthBlock);
        Assert.DoesNotContain("RequirePermisoEmpresa", healthBlock);
        Assert.DoesNotContain("RequireAuthorization", healthBlock);
    }

    [Fact]
    public void OpenApi_no_requiere_header_de_empresa_activa_ni_permiso_empresarial()
    {
        var source = File.ReadAllText(ResolverRutaRepo("src/CapitalPos.Api/Program.cs"));
        var openApiStart = source.IndexOf("app.MapOpenApi()", StringComparison.Ordinal);

        Assert.True(openApiStart >= 0);
        var openApiBlock = source[openApiStart..source.IndexOf(";", openApiStart, StringComparison.Ordinal)];
        Assert.DoesNotContain("EmpresaActivaEndpointFilter", openApiBlock);
        Assert.DoesNotContain("RequirePermisoEmpresa", openApiBlock);
        Assert.DoesNotContain("RequireAuthorization", openApiBlock);
    }

    private static int Contar(string source, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
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
