namespace CapitalPos.Infrastructure.Cpe;

public sealed class CpeApiHttpClient : ICpeApiHttpClient
{
    private readonly HttpClient _httpClient;

    public CpeApiHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Uri? BaseAddress => _httpClient.BaseAddress;
}
