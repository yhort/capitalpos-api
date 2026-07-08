namespace CapitalPos.Tests;

public class ExecutionDocumentationTests
{
    [Fact]
    public void Documentacion_de_ejecucion_incluye_comandos_esenciales()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("dotnet restore CapitalPos.Api.sln", documentacion);
        Assert.Contains("dotnet tool restore", documentacion);
        Assert.Contains("dotnet run --project src/CapitalPos.Api", documentacion);
        Assert.Contains("dotnet ef database update", documentacion);
        Assert.Contains("dotnet list CapitalPos.Api.sln package --vulnerable --include-transitive", documentacion);
        Assert.Contains("dotnet build CapitalPos.Api.sln -m:1 -nr:false", documentacion);
        Assert.Contains("dotnet test CapitalPos.Api.sln -m:1 -nr:false", documentacion);
    }

    [Fact]
    public void Documentacion_de_ejecucion_mantiene_secretos_fuera_del_repositorio()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("dotnet user-secrets set", documentacion);
        Assert.Contains("ConnectionStrings:CapitalPos", documentacion);
        Assert.Contains("Jwt:SigningKey", documentacion);
        Assert.Contains("CpeApi:BaseUrl", documentacion);
        Assert.Contains("CpeApi:ApiKey", documentacion);
        Assert.Contains("No imprimir ni registrar esos valores.", documentacion);
        Assert.DoesNotContain("Password=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=localhost;", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Documentacion_de_ejecucion_indica_endpoints_publicos_y_empresa_activa()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("http://localhost:5198/api/health", documentacion);
        Assert.Contains("http://localhost:5198/openapi/v1.json", documentacion);
        Assert.Contains("X-CapitalPos-EmpresaId", documentacion);
        Assert.Contains("Angular nunca debe conocer", documentacion);
        Assert.Contains("X-API-KEY", documentacion);
    }

    private static string LeerDocumento()
    {
        var root = EncontrarRaizRepo();
        return File.ReadAllText(Path.Combine(root, "Docs", "Ejecucion.md"));
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
