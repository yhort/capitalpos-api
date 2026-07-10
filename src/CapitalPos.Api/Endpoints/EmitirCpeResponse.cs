namespace CapitalPos.Api.Endpoints;

public sealed record EmitirCpeResponse(
    bool Ok,
    string Estado,
    string? Mensaje,
    string? Codigo,
    string? Comprobante,
    string? Hash,
    string? NombreXml,
    string? NombreZip,
    string? NombreCdr,
    IReadOnlyCollection<EmitirCpeErrorResponse> Errores);

public sealed record EmitirCpeErrorResponse(
    string? Codigo,
    string? Campo,
    string Mensaje);

public sealed record EmitirCpeApiResponse(
    bool Ok,
    string Mensaje,
    EmitirCpeResponse? Data,
    IReadOnlyCollection<string> Errores)
{
    public static EmitirCpeApiResponse From(EmitirCpeResponse response)
    {
        return new EmitirCpeApiResponse(
            response.Ok,
            response.Mensaje ?? "Emision CPE procesada.",
            response,
            response.Errores.Select(error => error.Mensaje).ToArray());
    }
}
