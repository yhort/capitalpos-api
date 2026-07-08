namespace CapitalPos.Application.Cpe;

public sealed record CpeGatewayResponse(
    int StatusCode,
    bool IsSuccessStatusCode,
    string Content,
    string ContentType);
