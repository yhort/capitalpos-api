using System.Text.Json;

namespace CapitalPos.Tests;

public class SecretsDocumentationTests
{
    [Fact]
    public void Documentacion_de_secretos_incluye_variables_requeridas()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("ConnectionStrings__CapitalPos", documentacion);
        Assert.Contains("Jwt__SigningKey", documentacion);
        Assert.Contains("CpeApi__BaseUrl", documentacion);
        Assert.Contains("CpeApi__ApiKey", documentacion);
        Assert.Contains("Cors__AllowedOrigins__N", documentacion);
        Assert.Contains("CpeSecuritySettings__ApiKey", documentacion);
        Assert.Contains("CpeSettings__PasswordCertificado", documentacion);
        Assert.Contains("CpeSettings__UsuarioSol", documentacion);
        Assert.Contains("CpeSettings__ClaveSol", documentacion);
        Assert.Contains("CAPITALPOS_TEST_CONNECTION_STRING", documentacion);
    }

    [Fact]
    public void Documentacion_de_secretos_define_gestion_por_ambiente()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("dotnet user-secrets", documentacion);
        Assert.Contains("Pruebas", documentacion);
        Assert.Contains("configuración aislada", documentacion);
        Assert.Contains("Producción", documentacion);
        Assert.Contains("gestor de secretos del proveedor de despliegue", documentacion);
        Assert.Contains("pendiente del despliegue", documentacion);
        Assert.Contains("CpeSettings:PasswordCertificado", documentacion);
        Assert.Contains("No versionar archivos `.pfx`", documentacion);
    }

    [Fact]
    public void Documentacion_de_secretos_declara_reglas_obligatorias()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("No almacenar secretos en `appsettings.json`", documentacion);
        Assert.Contains("No almacenar secretos en Git", documentacion);
        Assert.Contains("No incluir secretos en documentación, ejemplos, logs ni mensajes de error", documentacion);
        Assert.Contains("No compartir secretos entre desarrollo, pruebas y producción", documentacion);
        Assert.Contains("Usar valores distintos por ambiente", documentacion);
        Assert.Contains("privilegios mínimos", documentacion);
        Assert.Contains("Limitar quién puede leer o modificar secretos", documentacion);
        Assert.Contains("Rotar secretos periódicamente", documentacion);
        Assert.Contains("Revocar inmediatamente secretos comprometidos", documentacion);
        Assert.Contains("sin imprimir su valor", documentacion);
    }

    [Fact]
    public void Documentacion_de_secretos_incluye_rotacion_minima()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("JWT SigningKey", documentacion);
        Assert.Contains("API key de CPE", documentacion);
        Assert.Contains("Credenciales PostgreSQL", documentacion);
        Assert.Contains("Revocar", documentacion);
    }

    [Fact]
    public void Documentacion_de_secretos_no_contiene_credenciales_reales()
    {
        var documentacion = LeerDocumento();

        Assert.DoesNotContain("Password=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Username=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres://", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgresql://", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Appsettings_mantienen_valores_sensibles_vacios()
    {
        var root = EncontrarRaizRepo();

        AssertValoresVacios(Path.Combine(root, "src", "CapitalPos.Api", "appsettings.json"));
        AssertValoresVacios(Path.Combine(root, "src", "CapitalPos.Api", "appsettings.Development.json"));
    }

    private static void AssertValoresVacios(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.Equal(string.Empty, root.GetProperty("ConnectionStrings").GetProperty("CapitalPos").GetString());
        Assert.Equal(string.Empty, root.GetProperty("Jwt").GetProperty("SigningKey").GetString());
        Assert.Equal(string.Empty, root.GetProperty("CpeApi").GetProperty("BaseUrl").GetString());
        Assert.Equal(string.Empty, root.GetProperty("CpeApi").GetProperty("ApiKey").GetString());
    }

    private static string LeerDocumento()
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "Secretos.md"));
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
