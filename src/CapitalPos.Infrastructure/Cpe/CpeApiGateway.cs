using System.Text;
using System.Text.Json;
using CapitalPos.Application.Cpe;

namespace CapitalPos.Infrastructure.Cpe;

public sealed class CpeApiGateway : ICpeGateway
{
    private const string EmitirPath = "api/cpe/emitir";
    private readonly HttpClient _httpClient;

    public CpeApiGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CpeGatewayResponse> EmitirAsync(
        JsonElement request,
        CancellationToken cancellationToken = default)
    {
        using var content = new StringContent(
            request.GetRawText(),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(
            EmitirPath,
            content,
            cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;

        return new CpeGatewayResponse(
            (int)response.StatusCode,
            response.IsSuccessStatusCode,
            responseContent,
            contentType);
    }
}
