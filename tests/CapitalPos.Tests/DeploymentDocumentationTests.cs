namespace CapitalPos.Tests;

public class DeploymentDocumentationTests
{
    [Fact]
    public void Documentacion_de_despliegue_incluye_prerrequisitos()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("PostgreSQL productivo administrado", documentacion);
        Assert.Contains("Secretos configurados fuera del repositorio", documentacion);
        Assert.Contains("HTTPS terminado", documentacion);
        Assert.Contains("Dominio pendiente", documentacion);
        Assert.Contains("Migraciones productivas manuales", documentacion);
        Assert.Contains("GET /api/health", documentacion);
        Assert.Contains("Logs estructurados", documentacion);
        Assert.Contains("Backups", documentacion);
        Assert.Contains("Monitoreo", documentacion);
        Assert.Contains("Región y costos", documentacion);
    }

    [Fact]
    public void Documentacion_de_despliegue_incluye_variables_requeridas()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("ConnectionStrings__CapitalPos", documentacion);
        Assert.Contains("Jwt__SigningKey", documentacion);
        Assert.Contains("CpeApi__BaseUrl", documentacion);
        Assert.Contains("CpeApi__ApiKey", documentacion);
        Assert.Contains("ASPNETCORE_ENVIRONMENT=\"Production\"", documentacion);
    }

    [Fact]
    public void Documentacion_de_despliegue_incluye_checklist_y_procedimiento()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Build Release correcto", documentacion);
        Assert.Contains("dotnet publish", documentacion);
        Assert.Contains("Auditoría NuGet", documentacion);
        Assert.Contains("dotnet ef database update", documentacion);
        Assert.Contains("TLS/HTTPS", documentacion);
        Assert.Contains("Health check", documentacion);
        Assert.Contains("OpenAPI", documentacion);
        Assert.Contains("prueba funcional mínima", documentacion);
        Assert.Contains("X-Correlation-Id", documentacion);
        Assert.Contains("rollback", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Documentacion_de_despliegue_incluye_criterios_de_proveedor()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("región cercana a Perú", documentacion);
        Assert.Contains("soporte para .NET 10", documentacion);
        Assert.Contains("variables y secretos administrados", documentacion);
        Assert.Contains("HTTPS automático", documentacion);
        Assert.Contains("health checks", documentacion);
        Assert.Contains("escalado", documentacion);
        Assert.Contains("logs", documentacion);
        Assert.Contains("límites de memoria y CPU", documentacion);
        Assert.Contains("integración con PostgreSQL", documentacion);
        Assert.Contains("costos", documentacion);
        Assert.Contains("backups y restauración", documentacion);
        Assert.Contains("facilidad de rollback", documentacion);
    }

    [Fact]
    public void Documentacion_de_despliegue_no_elige_proveedor_ni_incluye_credenciales()
    {
        var documentacion = LeerDocumento();

        Assert.DoesNotContain("Azure", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AWS", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Render", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Railway", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Fly.io", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres://", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgresql://", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Documentacion_de_despliegue_mantiene_despliegue_real_pendiente()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Queda pendiente", documentacion);
        Assert.Contains("elegir proveedor", documentacion);
        Assert.Contains("crear Dockerfile si la plataforma lo requiere", documentacion);
        Assert.Contains("crear pipeline CI/CD", documentacion);
        Assert.Contains("configurar dominio y DNS", documentacion);
        Assert.Contains("desplegar la API", documentacion);
        Assert.Contains("configurar secretos reales", documentacion);
    }

    private static string LeerDocumento()
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "Despliegue.md"));
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
