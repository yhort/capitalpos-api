using System.Text.Json;

namespace CapitalPos.Tests;

public class ProductionReadinessDocumentationTests
{
    [Fact]
    public void Documentacion_productiva_define_variable_de_conexion_segura()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("ConnectionStrings__CapitalPos", documentacion);
        Assert.Contains("gestor de secretos", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TLS/SSL", documentacion);
        Assert.Contains("No debe almacenarse en Git", documentacion);
        Assert.Contains("appsettings.json", documentacion);
        Assert.Contains("appsettings.Development.json", documentacion);
    }

    [Fact]
    public void Documentacion_productiva_separa_ambientes_y_privilegios()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Desarrollo", documentacion);
        Assert.Contains("Pruebas", documentacion);
        Assert.Contains("Producción", documentacion);
        Assert.Contains("base exclusiva para producción", documentacion);
        Assert.Contains("usuario exclusivo para la API", documentacion);
        Assert.Contains("privilegios mínimos", documentacion);
        Assert.Contains("Nunca ejecutar pruebas automatizadas contra la base productiva.", documentacion);
    }

    [Fact]
    public void Documentacion_productiva_no_permite_migraciones_automaticas()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("La API no debe ejecutar migraciones automáticamente al iniciar.", documentacion);
        Assert.Contains("dotnet ef database update", documentacion);
        Assert.Contains("backup", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("staging", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Documentacion_productiva_incluye_criterios_de_proveedor()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("región cercana a los usuarios", documentacion);
        Assert.Contains("compatibilidad con PostgreSQL", documentacion);
        Assert.Contains("backups automáticos", documentacion);
        Assert.Contains("restauración", documentacion);
        Assert.Contains("monitoreo", documentacion);
        Assert.Contains("límites de conexiones", documentacion);
        Assert.Contains("costos", documentacion);
    }

    [Fact]
    public void Documentacion_productiva_no_contiene_credenciales_reales()
    {
        var documentacion = LeerDocumento();

        Assert.DoesNotContain("Password=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Username=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capitalpos_prod", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("amazonaws.com", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database.windows.net", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Appsettings_mantienen_connection_string_vacia()
    {
        var root = EncontrarRaizRepo();

        AssertConnectionStringVacia(Path.Combine(root, "src", "CapitalPos.Api", "appsettings.json"));
        AssertConnectionStringVacia(Path.Combine(root, "src", "CapitalPos.Api", "appsettings.Development.json"));
    }

    private static void AssertConnectionStringVacia(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var connectionStrings = document.RootElement.GetProperty("ConnectionStrings");
        var capitalPos = connectionStrings.GetProperty("CapitalPos").GetString();

        Assert.Equal(string.Empty, capitalPos);
    }

    private static string LeerDocumento()
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "Produccion.md"));
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
