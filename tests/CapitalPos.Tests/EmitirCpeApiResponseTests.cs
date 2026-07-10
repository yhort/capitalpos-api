using System.Text.Json;
using CapitalPos.Api.Endpoints;

namespace CapitalPos.Tests;

public class EmitirCpeApiResponseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Dto_publico_serializa_solo_los_campos_del_contrato()
    {
        var response = EmitirCpeApiResponse.From(new EmitirCpeResponse(
            false,
            "ERROR_VALIDACION",
            "El comprobante tiene errores de validacion.",
            "ERROR_VALIDACION",
            null,
            null,
            null,
            null,
            null,
            [
                new EmitirCpeErrorResponse(
                    "CPE_SERIE_OBLIGATORIA",
                    "serie",
                    "Debe indicar la serie del comprobante.")
            ]));

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, JsonOptions));
        var root = document.RootElement;
        var data = root.GetProperty("data");
        var error = data.GetProperty("errores").EnumerateArray().Single();
        var rootErrors = root.GetProperty("errores");

        AssertJsonProperties(root, "ok", "mensaje", "data", "errores");
        AssertJsonProperties(
            data,
            "ok",
            "estado",
            "mensaje",
            "codigo",
            "comprobante",
            "hash",
            "nombreXml",
            "nombreZip",
            "nombreCdr",
            "errores");
        AssertJsonProperties(error, "codigo", "campo", "mensaje");
        Assert.Equal(JsonValueKind.Array, rootErrors.ValueKind);
        Assert.All(rootErrors.EnumerateArray(), item => Assert.Equal(JsonValueKind.String, item.ValueKind));
    }

    [Theory]
    [InlineData("SIMULADO")]
    [InlineData("ACEPTADO")]
    [InlineData("RECHAZADO")]
    [InlineData("ERROR_VALIDACION")]
    [InlineData("ERROR_XML")]
    [InlineData("ERROR_FIRMA")]
    [InlineData("ERROR_SUNAT")]
    [InlineData("ERROR_CDR")]
    [InlineData("ERROR_INTERNO")]
    [InlineData("ERROR_CPE")]
    [InlineData("RESPUESTA_CPE_INVALIDA")]
    public void Dto_publico_representa_estados_canonicos_documentados(string estado)
    {
        var response = EmitirCpeApiResponse.From(new EmitirCpeResponse(
            false,
            estado,
            "Mensaje seguro.",
            estado,
            null,
            null,
            null,
            null,
            null,
            [new EmitirCpeErrorResponse(estado, null, "Mensaje seguro.")]));

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, JsonOptions));
        var data = document.RootElement.GetProperty("data");

        Assert.Equal(estado, data.GetProperty("estado").GetString());
        Assert.Equal(estado, data.GetProperty("codigo").GetString());
        Assert.Equal("Mensaje seguro.", data.GetProperty("errores")[0].GetProperty("mensaje").GetString());
    }

    [Fact]
    public void Dto_publico_no_serializa_campos_sensibles_o_internos()
    {
        var response = EmitirCpeApiResponse.From(new EmitirCpeResponse(
            false,
            "ERROR_CPE",
            "Servicio CPE no disponible.",
            "ERROR_CPE",
            null,
            null,
            null,
            null,
            null,
            [new EmitirCpeErrorResponse("ERROR_CPE", null, "Servicio CPE no disponible.")]));

        var json = JsonSerializer.Serialize(response, JsonOptions);

        Assert.DoesNotContain("xmlCrudo", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ruta", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apiKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-API-KEY", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certificado", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credencial", json, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertJsonProperties(JsonElement element, params string[] expectedProperties)
    {
        var actualProperties = element
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = expectedProperties
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actualProperties);
    }
}
