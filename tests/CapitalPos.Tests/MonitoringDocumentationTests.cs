namespace CapitalPos.Tests;

public class MonitoringDocumentationTests
{
    [Fact]
    public void Documentacion_de_monitoreo_incluye_senales_minimas()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("GET /api/health", documentacion);
        Assert.Contains("errores HTTP 5xx", documentacion);
        Assert.Contains("respuestas 4xx", documentacion);
        Assert.Contains("latencia por endpoint", documentacion);
        Assert.Contains("CapitalPOS CPE API", documentacion);
        Assert.Contains("PostgreSQL", documentacion);
        Assert.Contains("fallos de autenticación y autorización", documentacion);
        Assert.Contains("excepciones no controladas", documentacion);
        Assert.Contains("volumen de solicitudes", documentacion);
    }

    [Fact]
    public void Documentacion_de_monitoreo_incluye_alertas_minimas()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("health no disponible", documentacion);
        Assert.Contains("aumento sostenido de errores 5xx", documentacion);
        Assert.Contains("latencia elevada", documentacion);
        Assert.Contains("fallos repetidos hacia CapitalPOS CPE API", documentacion);
        Assert.Contains("fallos de conexión a PostgreSQL", documentacion);
        Assert.Contains("ausencia inesperada de tráfico", documentacion);
        Assert.Contains("crecimiento anormal de errores de autenticación", documentacion);
        Assert.Contains("vencimiento próximo de certificados", documentacion);
    }

    [Fact]
    public void Documentacion_de_monitoreo_aprovecha_capacidades_existentes()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("RequestLoggingMiddleware", documentacion);
        Assert.Contains("X-Correlation-Id", documentacion);
        Assert.Contains("GlobalExceptionHandlingMiddleware", documentacion);
        Assert.Contains("logs estructurados", documentacion);
        Assert.Contains("auditoría de operaciones", documentacion);
    }

    [Fact]
    public void Documentacion_de_monitoreo_prohibe_secretos_en_observabilidad()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("No registrar JWT", documentacion);
        Assert.Contains("No registrar API keys", documentacion);
        Assert.Contains("No registrar contraseñas", documentacion);
        Assert.Contains("No registrar cadenas de conexión", documentacion);
        Assert.Contains("No registrar certificados", documentacion);
        Assert.Contains("No registrar request bodies completos", documentacion);
        Assert.Contains("Restringir acceso a logs y métricas", documentacion);
        Assert.Contains("retención", documentacion);
    }

    [Fact]
    public void Documentacion_de_monitoreo_incluye_checklist_y_criterios_de_proveedor()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Health visible desde el monitor", documentacion);
        Assert.Contains("Alerta de prueba funcionando", documentacion);
        Assert.Contains("Latencia visible por endpoint", documentacion);
        Assert.Contains("Integración CPE observable", documentacion);
        Assert.Contains("Errores de PostgreSQL observables", documentacion);
        Assert.Contains("integración con la plataforma de despliegue", documentacion);
        Assert.Contains("soporte para logs estructurados", documentacion);
        Assert.Contains("trazas distribuidas", documentacion);
        Assert.Contains("exportación de datos", documentacion);
        Assert.Contains("correlacionar CapitalPOS API con CapitalPOS CPE API", documentacion);
    }

    [Fact]
    public void Documentacion_de_monitoreo_no_elige_proveedor_ni_incluye_tokens()
    {
        var documentacion = LeerDocumento();

        Assert.DoesNotContain("OpenTelemetry", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Prometheus", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Grafana", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Application Insights", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CloudWatch", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Datadog", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApiKey=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Documentacion_de_monitoreo_deja_integracion_real_pendiente()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("La integración real con una herramienta de monitoreo queda pendiente", documentacion);
        Assert.Contains("elección del proveedor", documentacion);
    }

    private static string LeerDocumento()
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "Monitoreo.md"));
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
