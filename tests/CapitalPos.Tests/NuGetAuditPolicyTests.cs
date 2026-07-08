using System.Xml.Linq;

namespace CapitalPos.Tests;

public class NuGetAuditPolicyTests
{
    [Fact]
    public void Proyectos_no_desactivan_nuget_audit()
    {
        foreach (var projectPath in ObtenerProyectos())
        {
            var document = XDocument.Load(projectPath);
            var disabledAudit = document
                .Descendants()
                .Where(element => element.Name.LocalName == "NuGetAudit")
                .Select(element => element.Value.Trim())
                .Any(value => string.Equals(value, "false", StringComparison.OrdinalIgnoreCase));

            Assert.False(disabledAudit);
        }
    }

    [Fact]
    public void Proyectos_no_suprimen_advertencias_nuget_audit()
    {
        foreach (var projectPath in ObtenerProyectos())
        {
            var document = XDocument.Load(projectPath);
            var noWarnValues = document
                .Descendants()
                .Where(element => element.Name.LocalName == "NoWarn")
                .Select(element => element.Value);

            foreach (var noWarn in noWarnValues)
            {
                Assert.DoesNotContain("NU190", noWarn, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Package_references_usan_versiones_explicitas_no_flotantes()
    {
        foreach (var projectPath in ObtenerProyectos())
        {
            var document = XDocument.Load(projectPath);
            var packageReferences = document
                .Descendants()
                .Where(element => element.Name.LocalName == "PackageReference");

            foreach (var packageReference in packageReferences)
            {
                var packageName = packageReference.Attribute("Include")?.Value;
                var version = packageReference.Attribute("Version")?.Value;

                Assert.False(
                    string.IsNullOrWhiteSpace(version),
                    $"El paquete {packageName} en {projectPath} debe declarar Version explicita.");
                Assert.DoesNotContain("*", version, StringComparison.Ordinal);
                Assert.DoesNotContain("-", version, StringComparison.Ordinal);
            }
        }
    }

    private static IReadOnlyCollection<string> ObtenerProyectos()
    {
        var root = EncontrarRaizRepo();

        return Directory
            .EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
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
