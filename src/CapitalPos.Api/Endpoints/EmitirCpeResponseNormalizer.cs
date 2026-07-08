using System.Text.Json;
using CapitalPos.Application.Cpe;

namespace CapitalPos.Api.Endpoints;

public static class EmitirCpeResponseNormalizer
{
    private const string EstadoRespuestaInvalida = "RESPUESTA_CPE_INVALIDA";
    private const string CodigoRespuestaInvalida = "CPE_RESPUESTA_INVALIDA";
    private const string MensajeRespuestaInvalida = "No se pudo interpretar la respuesta del servicio CPE.";

    public static EmitirCpeEndpointResponse Normalizar(CpeGatewayResponse gatewayResponse)
    {
        if (string.IsNullOrWhiteSpace(gatewayResponse.Content))
        {
            return CrearRespuestaInvalida();
        }

        try
        {
            using var document = JsonDocument.Parse(gatewayResponse.Content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return CrearRespuestaInvalida();
            }

            return NormalizarRespuestaApi(root, gatewayResponse.StatusCode);
        }
        catch (JsonException)
        {
            return CrearRespuestaInvalida();
        }
    }

    private static EmitirCpeEndpointResponse NormalizarRespuestaApi(
        JsonElement root,
        int statusCode)
    {
        var rootOk = TryGetBoolean(root, "ok");
        var rootMensaje = TryGetString(root, "mensaje");
        var rootErrores = LeerErrores(root, "errores", null);
        var data = TryGetProperty(root, "data", out var dataElement) &&
            dataElement.ValueKind == JsonValueKind.Object
                ? dataElement
                : default;
        var tieneData = data.ValueKind == JsonValueKind.Object;

        if (!rootOk.HasValue && !tieneData)
        {
            return CrearRespuestaInvalida();
        }

        var dataOk = tieneData ? TryGetBoolean(data, "ok") : null;
        var ok = dataOk ?? rootOk ?? false;
        var estado = tieneData
            ? TryGetString(data, "estado")
            : null;
        estado = string.IsNullOrWhiteSpace(estado)
            ? ok ? "ACEPTADO" : "ERROR_CPE"
            : estado;

        var mensaje = tieneData
            ? TryGetString(data, "mensaje") ?? rootMensaje
            : rootMensaje;
        var codigo = estado;
        var errores = new List<EmitirCpeErrorResponse>();
        errores.AddRange(rootErrores);

        if (tieneData)
        {
            errores.AddRange(LeerErrores(data, "errores", codigo));
        }

        if (!ok && errores.Count == 0)
        {
            errores.Add(new EmitirCpeErrorResponse(
                codigo,
                null,
                mensaje ?? "El servicio CPE no pudo procesar la emision."));
        }

        var response = new EmitirCpeResponse(
            ok,
            estado,
            mensaje,
            codigo,
            CrearComprobante(tieneData ? data : default),
            tieneData ? TryGetString(data, "hash") : null,
            tieneData ? TryGetString(data, "nombreXml") : null,
            tieneData ? TryGetString(data, "nombreZip") : null,
            tieneData ? TryGetString(data, "nombreCdr") : null,
            errores);

        return new EmitirCpeEndpointResponse(statusCode, response);
    }

    private static EmitirCpeEndpointResponse CrearRespuestaInvalida()
    {
        var response = new EmitirCpeResponse(
            false,
            EstadoRespuestaInvalida,
            MensajeRespuestaInvalida,
            CodigoRespuestaInvalida,
            null,
            null,
            null,
            null,
            null,
            [
                new EmitirCpeErrorResponse(
                    CodigoRespuestaInvalida,
                    null,
                    MensajeRespuestaInvalida)
            ]);

        return new EmitirCpeEndpointResponse(StatusCodes.Status502BadGateway, response);
    }

    private static EmitirCpeComprobanteResponse? CrearComprobante(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var comprobante = TryGetString(data, "comprobante");
        return string.IsNullOrWhiteSpace(comprobante)
            ? null
            : new EmitirCpeComprobanteResponse(comprobante);
    }

    private static IReadOnlyCollection<EmitirCpeErrorResponse> LeerErrores(
        JsonElement source,
        string propertyName,
        string? codigo)
    {
        if (!TryGetProperty(source, propertyName, out var errores) ||
            errores.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<EmitirCpeErrorResponse>();
        foreach (var error in errores.EnumerateArray())
        {
            if (error.ValueKind == JsonValueKind.String)
            {
                var mensaje = error.GetString();
                if (!string.IsNullOrWhiteSpace(mensaje))
                {
                    result.Add(new EmitirCpeErrorResponse(codigo, null, mensaje));
                }
            }
            else if (error.ValueKind == JsonValueKind.Object)
            {
                var mensaje = TryGetString(error, "mensaje") ??
                    TryGetString(error, "message") ??
                    "Error informado por el servicio CPE.";
                result.Add(new EmitirCpeErrorResponse(
                    TryGetString(error, "codigo") ?? codigo,
                    TryGetString(error, "campo"),
                    mensaje));
            }
        }

        return result;
    }

    private static string? TryGetString(JsonElement source, string propertyName)
    {
        if (!TryGetProperty(source, propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    private static bool? TryGetBoolean(JsonElement source, string propertyName)
    {
        if (!TryGetProperty(source, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static bool TryGetProperty(
        JsonElement source,
        string propertyName,
        out JsonElement value)
    {
        foreach (var property in source.EnumerateObject())
        {
            if (property.NameEquals(propertyName))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

public sealed record EmitirCpeEndpointResponse(
    int StatusCode,
    EmitirCpeResponse Body);
