namespace CapitalPos.Tests;

public class CpeContractDocumentationTests
{
    [Fact]
    public void Contrato_cpe_documenta_endpoint_respuesta_estados_y_politica_http()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("POST /api/cpe/emitir", documentacion);
        Assert.Contains("X-CapitalPos-EmpresaId", documentacion);
        Assert.Contains("Permiso de empresa `EmitirCpe`", documentacion);
        Assert.Contains("\"ok\"", documentacion);
        Assert.Contains("\"mensaje\"", documentacion);
        Assert.Contains("\"data\"", documentacion);
        Assert.Contains("\"errores\"", documentacion);
        Assert.Contains("SIMULADO", documentacion);
        Assert.Contains("ACEPTADO", documentacion);
        Assert.Contains("RECHAZADO", documentacion);
        Assert.Contains("ERROR_VALIDACION", documentacion);
        Assert.Contains("ERROR_XML", documentacion);
        Assert.Contains("ERROR_FIRMA", documentacion);
        Assert.Contains("ERROR_SUNAT", documentacion);
        Assert.Contains("ERROR_CDR", documentacion);
        Assert.Contains("ERROR_INTERNO", documentacion);
        Assert.Contains("ERROR_CPE", documentacion);
        Assert.Contains("RESPUESTA_CPE_INVALIDA", documentacion);
        Assert.Contains("200 OK", documentacion);
        Assert.Contains("400 Bad Request", documentacion);
        Assert.Contains("502 Bad Gateway", documentacion);
        Assert.Contains("error inesperado", documentacion);
    }

    [Fact]
    public void Contrato_cpe_documenta_ejemplos_json_obligatorios()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("### SIMULADO", documentacion);
        Assert.Contains("### ACEPTADO", documentacion);
        Assert.Contains("### RECHAZADO", documentacion);
        Assert.Contains("### ERROR_VALIDACION", documentacion);
        Assert.Contains("### ERROR_SUNAT", documentacion);
        Assert.Contains("### ERROR_CPE", documentacion);
        Assert.Contains("### RESPUESTA_CPE_INVALIDA", documentacion);
        Assert.Contains("\"estado\": \"ACEPTADO\"", documentacion);
        Assert.Contains("\"estado\": \"ERROR_VALIDACION\"", documentacion);
        Assert.Contains("\"estado\": \"ERROR_CPE\"", documentacion);
        Assert.Contains("\"estado\": \"RESPUESTA_CPE_INVALIDA\"", documentacion);
    }

    [Fact]
    public void Contrato_cpe_distingue_errores_tecnicos_de_normalizacion()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("### Diferencias entre errores tecnicos", documentacion);
        Assert.Contains("`ERROR_CPE`: `capitalpos-api` no pudo comunicarse correctamente", documentacion);
        Assert.Contains("`RESPUESTA_CPE_INVALIDA`: `capitalpos-api` recibio una respuesta vacia", documentacion);
        Assert.Contains("`ERROR_INTERNO`: `capitalpos-cpe-api` si respondio con JSON interpretable", documentacion);
    }

    [Fact]
    public void Contrato_cpe_documenta_que_no_debe_exponer_datos_sensibles()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("X-API-KEY", documentacion);
        Assert.Contains("rutas internas", documentacion);
        Assert.Contains("certificados", documentacion);
        Assert.Contains("credenciales SUNAT", documentacion);
        Assert.Contains("cuerpo crudo", documentacion);
        Assert.Contains("XML, ZIP o CDR en bruto", documentacion);
    }

    private static string LeerDocumento()
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "ContratoCpeEmision.md"));
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
