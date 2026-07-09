namespace CapitalPos.Api.Endpoints;

public sealed record CpeEstadoResponse(
    bool Ok,
    string Estado,
    string Mensaje,
    string? Servicio,
    string? Version,
    string? Modo,
    bool? SimularGeneracionXml,
    bool? SimularFirma,
    bool? SimularEnvioSunat,
    IReadOnlyCollection<CpeEstadoErrorResponse> Errores);

public sealed record CpeEstadoErrorResponse(
    string? Codigo,
    string Mensaje);
