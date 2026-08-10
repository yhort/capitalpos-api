namespace CapitalPos.Tests;

public class CorsConfigurationStructureTests
{
    [Fact]
    public void Program_configura_cors_con_origenes_explicito_sin_allow_any_origin()
    {
        var source = File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "src", "CapitalPos.Api", "Program.cs"));

        Assert.Contains("Cors:AllowedOrigins", source);
        Assert.Contains("AddPolicy(\"CapitalPosWeb\"", source);
        Assert.Contains("WithOrigins(corsOrigins)", source);
        Assert.Contains("SetIsOriginAllowed(_ => false)", source);
        Assert.Contains("UseCors(\"CapitalPosWeb\")", source);
        Assert.DoesNotContain("AllowAnyOrigin", source);
    }

    [Fact]
    public void Appsettings_definen_cors_allowed_origins()
    {
        var root = EncontrarRaizRepo();
        using var baseSettings = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "src", "CapitalPos.Api", "appsettings.json")));
        using var developmentSettings = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "src", "CapitalPos.Api", "appsettings.Development.json")));

        Assert.Equal(
            System.Text.Json.JsonValueKind.Array,
            baseSettings.RootElement.GetProperty("Cors").GetProperty("AllowedOrigins").ValueKind);
        Assert.Empty(
            baseSettings.RootElement.GetProperty("Cors").GetProperty("AllowedOrigins").EnumerateArray());

        var developmentOrigins = developmentSettings.RootElement
            .GetProperty("Cors")
            .GetProperty("AllowedOrigins")
            .EnumerateArray()
            .Select(origin => origin.GetString())
            .ToArray();
        Assert.Contains("http://localhost:4200", developmentOrigins);
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
