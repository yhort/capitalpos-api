using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Cpe;

namespace CapitalPos.Tests;

public class EmitirCpeResponseNormalizerTests
{
    [Fact]
    public void Normaliza_emision_exitosa()
    {
        var result = Normalizar("""
            {
                "ok": true,
                "mensaje": "Comprobante emitido correctamente.",
                "data": {
                    "ok": true,
                    "estado": "ACEPTADO",
                    "mensaje": "Aceptado por SUNAT.",
                    "comprobante": "F001-1",
                    "hash": "abc123",
                    "nombreXml": "20123456789-01-F001-1.xml",
                    "nombreZip": "20123456789-01-F001-1.zip",
                    "nombreCdr": "R-20123456789-01-F001-1.zip",
                    "errores": []
                },
                "errores": []
            }
            """);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.Body.Ok);
        Assert.Equal("ACEPTADO", result.Body.Estado);
        Assert.Equal("ACEPTADO", result.Body.Codigo);
        Assert.Equal("Aceptado por SUNAT.", result.Body.Mensaje);
        Assert.Equal("F001-1", result.Body.Comprobante);
        Assert.Equal("abc123", result.Body.Hash);
        Assert.Equal("20123456789-01-F001-1.xml", result.Body.NombreXml);
        Assert.Equal("20123456789-01-F001-1.zip", result.Body.NombreZip);
        Assert.Equal("R-20123456789-01-F001-1.zip", result.Body.NombreCdr);
        Assert.Empty(result.Body.Errores);
    }

    [Fact]
    public void Normaliza_emision_exitosa_con_estado_pascal_case()
    {
        var result = Normalizar("""
            {
                "Ok": true,
                "Mensaje": "Comprobante emitido correctamente.",
                "Data": {
                    "Ok": true,
                    "Estado": "SIMULADO",
                    "Mensaje": "Comprobante aceptado en modo simulación. Modo: BETA. URL SUNAT: https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService",
                    "Comprobante": "F001-1",
                    "Hash": "abc123",
                    "NombreXml": "20123456789-01-F001-1.xml",
                    "NombreZip": "20123456789-01-F001-1.zip",
                    "NombreCdr": "R-20123456789-01-F001-1.zip",
                    "Errores": []
                },
                "Errores": []
            }
            """);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.Body.Ok);
        Assert.Equal("SIMULADO", result.Body.Estado);
        Assert.Equal("Comprobante aceptado en modo simulación. Modo: BETA. URL SUNAT: https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService", result.Body.Mensaje);
        Assert.Equal("F001-1", result.Body.Comprobante);
        Assert.Equal("abc123", result.Body.Hash);
    }

    [Fact]
    public void Normaliza_ok_true_sin_estado_pero_con_mensaje_de_aceptacion_en_simulacion()
    {
        var result = Normalizar("""
            {
                "ok": true,
                "mensaje": "Comprobante emitido correctamente.",
                "data": {
                    "ok": true,
                    "mensaje": "Comprobante aceptado en modo simulación. Modo: BETA. URL SUNAT: https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService",
                    "comprobante": "F001-1",
                    "hash": "abc123"
                },
                "errores": []
            }
            """);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.Body.Ok);
        Assert.Equal("SIMULADO", result.Body.Estado);
        Assert.Equal("SIMULADO", result.Body.Codigo);
        Assert.Contains("Comprobante aceptado en modo simulación", result.Body.Mensaje);
        Assert.Equal("F001-1", result.Body.Comprobante);
    }

    [Fact]
    public void Normaliza_ok_true_sin_data_pero_con_mensaje_de_aceptacion_en_simulacion()
    {
        var result = Normalizar("""
            {
                "ok": true,
                "mensaje": "Comprobante aceptado en modo simulación. Modo: BETA. URL SUNAT: https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService",
                "errores": []
            }
            """);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.Body.Ok);
        Assert.Equal("SIMULADO", result.Body.Estado);
        Assert.Contains("Comprobante aceptado en modo simulación", result.Body.Mensaje);
    }

    [Fact]
    public void Normaliza_rechazo_funcional_de_sunat()
    {
        var result = Normalizar("""
            {
                "ok": false,
                "mensaje": "No se pudo enviar el comprobante a SUNAT.",
                "data": {
                    "ok": false,
                    "estado": "RECHAZADO",
                    "mensaje": "El comprobante fue rechazado.",
                    "errores": ["El RUC del receptor no existe."]
                },
                "errores": []
            }
            """, statusCode: 400);

        Assert.Equal(400, result.StatusCode);
        Assert.False(result.Body.Ok);
        Assert.Equal("RECHAZADO", result.Body.Estado);
        Assert.Equal("RECHAZADO", result.Body.Codigo);
        Assert.Equal("El comprobante fue rechazado.", result.Body.Mensaje);
        var error = Assert.Single(result.Body.Errores);
        Assert.Equal("RECHAZADO", error.Codigo);
        Assert.Null(error.Campo);
        Assert.Equal("El RUC del receptor no existe.", error.Mensaje);
    }

    [Fact]
    public void Normaliza_error_de_validacion()
    {
        var result = Normalizar("""
            {
                "ok": false,
                "mensaje": "El comprobante tiene errores de validación.",
                "data": {
                    "ok": false,
                    "estado": "ERROR_VALIDACION",
                    "mensaje": "El comprobante tiene errores de validación.",
                    "errores": ["Debe indicar cliente."]
                },
                "errores": ["Serie obligatoria."]
            }
            """, statusCode: 400);

        Assert.Equal(400, result.StatusCode);
        Assert.False(result.Body.Ok);
        Assert.Equal("ERROR_VALIDACION", result.Body.Estado);
        Assert.Equal("ERROR_VALIDACION", result.Body.Codigo);
        Assert.Contains(result.Body.Errores, error => error.Mensaje == "Serie obligatoria.");
        Assert.Contains(result.Body.Errores, error => error.Mensaje == "Debe indicar cliente.");
    }

    [Fact]
    public void Normaliza_error_sunat_como_error_funcional()
    {
        var result = Normalizar("""
            {
                "ok": false,
                "mensaje": "SUNAT no pudo procesar el comprobante.",
                "data": {
                    "ok": false,
                    "estado": "ERROR_SUNAT",
                    "mensaje": "SUNAT no pudo procesar el comprobante.",
                    "errores": [
                        {
                            "codigo": "SUNAT_TIMEOUT",
                            "mensaje": "SUNAT no respondio dentro del tiempo esperado."
                        }
                    ]
                },
                "errores": []
            }
            """, statusCode: 400);

        Assert.Equal(400, result.StatusCode);
        Assert.False(result.Body.Ok);
        Assert.Equal("ERROR_SUNAT", result.Body.Estado);
        Assert.Equal("ERROR_SUNAT", result.Body.Codigo);
        Assert.Equal("SUNAT no pudo procesar el comprobante.", result.Body.Mensaje);
        Assert.Contains(result.Body.Errores, error =>
            error.Codigo == "SUNAT_TIMEOUT" &&
            error.Mensaje == "SUNAT no respondio dentro del tiempo esperado.");
    }

    [Fact]
    public void Normaliza_respuesta_vacia_como_error_seguro()
    {
        var result = Normalizar(string.Empty);

        Assert.Equal(502, result.StatusCode);
        Assert.False(result.Body.Ok);
        Assert.Equal("RESPUESTA_CPE_INVALIDA", result.Body.Estado);
        Assert.Equal("CPE_RESPUESTA_INVALIDA", result.Body.Codigo);
        Assert.DoesNotContain(string.Empty, result.Body.Errores.Select(error => error.Mensaje));
    }

    [Fact]
    public void Normaliza_json_invalido_o_inesperado_como_error_seguro()
    {
        var invalidJson = Normalizar("{ esto no es json }");
        var unexpectedJson = Normalizar("""["ok"]""");

        Assert.Equal(502, invalidJson.StatusCode);
        Assert.Equal("RESPUESTA_CPE_INVALIDA", invalidJson.Body.Estado);
        Assert.Equal(502, unexpectedJson.StatusCode);
        Assert.Equal("RESPUESTA_CPE_INVALIDA", unexpectedJson.Body.Estado);
    }

    [Fact]
    public void Normaliza_error_http_del_servicio_cpe()
    {
        var result = Normalizar("""
            {
                "ok": false,
                "mensaje": "Ocurrió un error interno al emitir el comprobante.",
                "data": {
                    "ok": false,
                    "estado": "ERROR_INTERNO",
                    "mensaje": "No se pudo emitir el comprobante.",
                    "errores": ["Servicio SUNAT no disponible."]
                },
                "errores": []
            }
            """, statusCode: 500);

        Assert.Equal(500, result.StatusCode);
        Assert.False(result.Body.Ok);
        Assert.Equal("ERROR_INTERNO", result.Body.Estado);
        Assert.Contains(result.Body.Errores, error => error.Mensaje == "Servicio SUNAT no disponible.");
    }

    [Fact]
    public void Respuesta_publica_no_incluye_datos_sensibles_ni_cuerpo_crudo()
    {
        const string rawBody = """
            {
                "ok": false,
                "mensaje": "Error de validación.",
                "data": {
                    "ok": false,
                    "estado": "ERROR_VALIDACION",
                    "mensaje": "Datos inválidos.",
                    "errores": ["Serie obligatoria."]
                },
                "debug": "X-API-KEY: capitalpos-cpe-test-api-key /var/private/internal"
            }
            """;

        var result = Normalizar(rawBody, statusCode: 400);
        var publicText = string.Join(
            " ",
            result.Body.Mensaje,
            result.Body.Estado,
            result.Body.Codigo,
            string.Join(" ", result.Body.Errores.Select(error => error.Mensaje)));

        Assert.DoesNotContain("capitalpos-cpe-test-api-key", publicText);
        Assert.DoesNotContain("X-API-KEY", publicText);
        Assert.DoesNotContain("/var/private/internal", publicText);
        Assert.DoesNotContain(rawBody, publicText);
    }

    [Fact]
    public void Diagnostico_seguro_solo_indica_presencia_de_campos()
    {
        var response = new CpeGatewayResponse(
            200,
            true,
            """
            {
                "ok": true,
                "mensaje": "Comprobante aceptado en modo simulación. XML <Invoice>...</Invoice> X-API-KEY capitalpos-cpe-test-api-key",
                "data": {
                    "estado": "SIMULADO"
                }
            }
            """,
            "application/json");

        var diagnostics = EmitirCpeResponseNormalizer.ObtenerDiagnosticoSeguro(response);

        Assert.Equal(200, diagnostics.StatusCode);
        Assert.True(diagnostics.TieneOk);
        Assert.True(diagnostics.TieneDataEstado);
        Assert.True(diagnostics.TieneMensaje);
        Assert.DoesNotContain("capitalpos-cpe-test-api-key", diagnostics.ToString());
        Assert.DoesNotContain("<Invoice>", diagnostics.ToString());
    }

    private static EmitirCpeEndpointResponse Normalizar(
        string content,
        int statusCode = 200)
    {
        return EmitirCpeResponseNormalizer.Normalizar(new CpeGatewayResponse(
            statusCode,
            statusCode is >= 200 and <= 299,
            content,
            "application/json"));
    }
}
