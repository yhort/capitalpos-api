namespace CapitalPos.Tests;

public class CpeEndpointStructureTests
{
    [Fact]
    public void Cpe_endpoint_emitir_esta_mapeado_como_post_seguro()
    {
        var source = File.ReadAllText(ResolverRutaRepo("src/CapitalPos.Api/Endpoints/CpeEndpoints.cs"));

        Assert.Contains("app.MapGroup(\"/api/cpe\")", source);
        Assert.Contains(".RequireAuthorization()", source);
        Assert.Contains(".AddEndpointFilter<EmpresaActivaEndpointFilter>()", source);
        Assert.Contains("group.MapPost(\"/emitir\", EmitirAsync)", source);
        Assert.Contains(".RequirePermisoEmpresa(PermisoEmpresa.EmitirCpe)", source);
    }

    [Fact]
    public void Program_mapea_endpoints_cpe_sin_modificar_health_ni_openapi()
    {
        var source = File.ReadAllText(ResolverRutaRepo("src/CapitalPos.Api/Program.cs"));
        var healthStart = source.IndexOf("app.MapGet(\"/api/health\"", StringComparison.Ordinal);
        var openApiStart = source.IndexOf("app.MapOpenApi()", StringComparison.Ordinal);

        Assert.Contains("app.MapCpeEndpoints();", source);
        Assert.True(healthStart >= 0);
        Assert.True(openApiStart >= 0);

        var healthBlock = source[healthStart..source.IndexOf(";", healthStart, StringComparison.Ordinal)];
        var openApiBlock = source[openApiStart..source.IndexOf(";", openApiStart, StringComparison.Ordinal)];
        Assert.DoesNotContain("MapCpeEndpoints", healthBlock);
        Assert.DoesNotContain("RequireAuthorization", healthBlock);
        Assert.DoesNotContain("MapCpeEndpoints", openApiBlock);
        Assert.DoesNotContain("RequireAuthorization", openApiBlock);
    }

    [Fact]
    public void Endpoint_emitir_llama_gateway_y_no_devuelve_cuerpo_crudo()
    {
        var source = File.ReadAllText(ResolverRutaRepo("src/CapitalPos.Api/Endpoints/CpeEndpoints.cs"));

        Assert.Contains("ICpeGateway gateway", source);
        Assert.Contains("gateway.EmitirAsync(request, cancellationToken)", source);
        Assert.Contains("EmitirCpeResponseNormalizer.Normalizar(response)", source);
        Assert.Contains("Results.Json(", source);
        Assert.DoesNotContain("Results.Content(", source);
        Assert.DoesNotContain("ApiResponse", source);
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
