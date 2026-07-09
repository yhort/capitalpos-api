using System.Text.Json;
using CapitalPos.Application.Cpe;

namespace CapitalPos.Api.Endpoints;

public static class CpeEstadoResponseNormalizer
{
    public static CpeEstadoEndpointResponse Normalizar(CpeGatewayResponse gatewayResponse)
    {
        if (gatewayResponse.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            return CrearNoAutorizado();
        }

        if (!gatewayResponse.IsSuccessStatusCode)
        {
            return CrearNoDisponible(gatewayResponse.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(gatewayResponse.Content))
        {
            return CrearRespuestaInvalida();
        }

        try
        {
            using var document = JsonDocument.Parse(gatewayResponse.Content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetProperty(root, "data", out var data) ||
                data.ValueKind != JsonValueKind.Object)
            {
                return CrearRespuestaInvalida();
            }

            var estado = TryGetString(data, "status");
            if (string.IsNullOrWhiteSpace(estado))
            {
                return CrearRespuestaInvalida();
            }

            var ok = TryGetBoolean(root, "ok") ?? gatewayResponse.IsSuccessStatusCode;
            var response = new CpeEstadoResponse(
                ok,
                estado,
                TryGetString(root, "mensaje") ?? "Estado de API CPE obtenido correctamente.",
                TryGetString(data, "service"),
                TryGetString(data, "version"),
                TryGetString(data, "modo"),
                TryGetBoolean(data, "simularGeneracionXml"),
                TryGetBoolean(data, "simularFirma"),
                TryGetBoolean(data, "simularEnvioSunat"),
                LeerErrores(root));

            return new CpeEstadoEndpointResponse(StatusCodes.Status200OK, response);
        }
        catch (JsonException)
        {
            return CrearRespuestaInvalida();
        }
    }

    public static CpeEstadoEndpointResponse CrearNoDisponible(Exception? exception = null)
    {
        return CrearNoDisponible((int?)null);
    }

    private static CpeEstadoEndpointResponse CrearNoDisponible(int? statusCode)
    {
        var mensaje = statusCode.HasValue
            ? "La API CPE no esta disponible o no pudo responder correctamente."
            : "La API CPE no esta disponible.";

        return new CpeEstadoEndpointResponse(
            StatusCodes.Status503ServiceUnavailable,
            new CpeEstadoResponse(
                false,
                "NO_DISPONIBLE",
                mensaje,
                null,
                null,
                null,
                null,
                null,
                null,
                [
                    new CpeEstadoErrorResponse(
                        statusCode?.ToString(),
                        "No se pudo obtener el estado de la API CPE.")
                ]));
    }

    private static CpeEstadoEndpointResponse CrearNoAutorizado()
    {
        return new CpeEstadoEndpointResponse(
            StatusCodes.Status503ServiceUnavailable,
            new CpeEstadoResponse(
                false,
                "NO_AUTORIZADO",
                "No se pudo consultar la API CPE por un problema de configuracion interna.",
                null,
                null,
                null,
                null,
                null,
                null,
                [
                    new CpeEstadoErrorResponse(
                        "CPE_NO_AUTORIZADO",
                        "La API CPE rechazo la autenticacion configurada.")
                ]));
    }

    private static CpeEstadoEndpointResponse CrearRespuestaInvalida()
    {
        return new CpeEstadoEndpointResponse(
            StatusCodes.Status502BadGateway,
            new CpeEstadoResponse(
                false,
                "RESPUESTA_INVALIDA",
                "La API CPE no devolvió datos de estado válidos.",
                null,
                null,
                null,
                null,
                null,
                null,
                [
                    new CpeEstadoErrorResponse(
                        "CPE_RESPUESTA_INVALIDA",
                        "La respuesta de estado de la API CPE no tiene el formato esperado.")
                ]));
    }

    private static IReadOnlyCollection<CpeEstadoErrorResponse> LeerErrores(JsonElement root)
    {
        if (!TryGetProperty(root, "errores", out var errores) ||
            errores.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<CpeEstadoErrorResponse>();
        foreach (var error in errores.EnumerateArray())
        {
            if (error.ValueKind == JsonValueKind.String)
            {
                var mensaje = error.GetString();
                if (!string.IsNullOrWhiteSpace(mensaje))
                {
                    result.Add(new CpeEstadoErrorResponse(null, mensaje));
                }
            }
            else if (error.ValueKind == JsonValueKind.Object)
            {
                result.Add(new CpeEstadoErrorResponse(
                    TryGetString(error, "codigo"),
                    TryGetString(error, "mensaje") ?? "Error informado por la API CPE."));
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

public sealed record CpeEstadoEndpointResponse(
    int StatusCode,
    CpeEstadoResponse Body);
