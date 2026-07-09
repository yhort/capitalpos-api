using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Cpe;
using Microsoft.AspNetCore.Http;

namespace CapitalPos.Tests;

public class CpeEstadoResponseNormalizerTests
{
    [Fact]
    public void Normaliza_respuesta_exitosa_para_angular()
    {
        var gatewayResponse = new CpeGatewayResponse(
            200,
            true,
            """
            {
              "ok": true,
              "mensaje": "API CPE funcionando correctamente.",
              "data": {
                "status": "OK",
                "service": "CapitalPOS CPE API",
                "version": "1.0.0",
                "modo": "BETA",
                "simularGeneracionXml": false,
                "simularFirma": false,
                "simularEnvioSunat": true
              },
              "errores": []
            }
            """,
            "application/json");

        var response = CpeEstadoResponseNormalizer.Normalizar(gatewayResponse);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.True(response.Body.Ok);
        Assert.Equal("OK", response.Body.Estado);
        Assert.Equal("API CPE funcionando correctamente.", response.Body.Mensaje);
        Assert.Equal("CapitalPOS CPE API", response.Body.Servicio);
        Assert.Equal("1.0.0", response.Body.Version);
        Assert.Equal("BETA", response.Body.Modo);
        Assert.False(response.Body.SimularGeneracionXml);
        Assert.False(response.Body.SimularFirma);
        Assert.True(response.Body.SimularEnvioSunat);
        Assert.Empty(response.Body.Errores);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public void Normaliza_no_autorizado_sin_exponer_api_key(int statusCode)
    {
        var gatewayResponse = new CpeGatewayResponse(
            statusCode,
            false,
            """{"mensaje":"api-key-super-secreta"}""",
            "application/json");

        var response = CpeEstadoResponseNormalizer.Normalizar(gatewayResponse);
        var serialized = System.Text.Json.JsonSerializer.Serialize(response.Body);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        Assert.False(response.Body.Ok);
        Assert.Equal("NO_AUTORIZADO", response.Body.Estado);
        Assert.Contains("configuracion interna", response.Body.Mensaje);
        Assert.DoesNotContain("api-key-super-secreta", serialized);
        Assert.DoesNotContain("X-API-KEY", serialized);
    }

    [Fact]
    public void Normaliza_no_disponible_para_error_http()
    {
        var gatewayResponse = new CpeGatewayResponse(
            500,
            false,
            """{"detalle":"ruta interna /var/app/cpe"}""",
            "application/json");

        var response = CpeEstadoResponseNormalizer.Normalizar(gatewayResponse);
        var serialized = System.Text.Json.JsonSerializer.Serialize(response.Body);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        Assert.False(response.Body.Ok);
        Assert.Equal("NO_DISPONIBLE", response.Body.Estado);
        Assert.DoesNotContain("/var/app/cpe", serialized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ json-invalido }")]
    [InlineData("""{"ok":true,"data":{}}""")]
    public void Normaliza_respuesta_invalida_con_error_seguro(string content)
    {
        var gatewayResponse = new CpeGatewayResponse(
            200,
            true,
            content,
            "application/json");

        var response = CpeEstadoResponseNormalizer.Normalizar(gatewayResponse);

        Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
        Assert.False(response.Body.Ok);
        Assert.Equal("RESPUESTA_INVALIDA", response.Body.Estado);
        Assert.Equal("La API CPE no devolvió datos de estado válidos.", response.Body.Mensaje);
        Assert.Contains(response.Body.Errores, error => error.Codigo == "CPE_RESPUESTA_INVALIDA");
    }

    [Fact]
    public void Crea_no_disponible_para_excepcion_sin_detalles_internos()
    {
        var response = CpeEstadoResponseNormalizer.CrearNoDisponible(
            new HttpRequestException("api-key-super-secreta /ruta/interna"));
        var serialized = System.Text.Json.JsonSerializer.Serialize(response.Body);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        Assert.Equal("NO_DISPONIBLE", response.Body.Estado);
        Assert.DoesNotContain("api-key-super-secreta", serialized);
        Assert.DoesNotContain("/ruta/interna", serialized);
    }
}
