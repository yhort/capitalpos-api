namespace CapitalPos.Api.Endpoints;

public sealed record EmitirCpeResponse(
    bool Ok,
    string Estado,
    string? Mensaje,
    string? Codigo,
    EmitirCpeComprobanteResponse? Comprobante,
    string? Hash,
    string? NombreXml,
    string? NombreZip,
    string? NombreCdr,
    IReadOnlyCollection<EmitirCpeErrorResponse> Errores);

public sealed record EmitirCpeComprobanteResponse(string Numero);

public sealed record EmitirCpeErrorResponse(
    string? Codigo,
    string? Campo,
    string Mensaje);
