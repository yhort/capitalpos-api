namespace CapitalPos.Tests;

public class TopologiaDocumentationTests
{
    [Fact]
    public void Documentacion_de_topologia_define_aislamiento_cpe_y_consumo_web()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("capitalpos-web", documentacion);
        Assert.Contains("solo consume `capitalpos-api`", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("capitalpos-cpe-api", documentacion);
        Assert.Contains("red privada", documentacion);
        Assert.Contains("X-API-KEY", documentacion);
        Assert.Contains("no configura CORS publico", documentacion);
        Assert.Contains("nunca llama a `capitalpos-cpe-api`", documentacion);
    }

    [Fact]
    public void Documentacion_de_despliegue_referencia_topologia()
    {
        var documentacion = File.ReadAllText(
            Path.Combine(EncontrarRaizRepo(), "Docs", "Despliegue.md"));

        Assert.Contains("Docs/Topologia.md", documentacion);
        Assert.Contains("Cors__AllowedOrigins__0", documentacion);
    }

    private static string LeerDocumento()
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "Topologia.md"));
    }

    private static string EncontrarRaizRepo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CapitalPos.Api.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo encontrar la raiz del repositorio.");
    }
}
