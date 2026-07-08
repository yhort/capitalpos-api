namespace CapitalPos.Infrastructure.Cpe;

public sealed class CpeApiOptions
{
    public const string SectionName = "CpeApi";

    public string BaseUrl { get; set; } = string.Empty;

    public Uri ObtenerBaseAddress()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException(
                "La configuracion 'CpeApi:BaseUrl' es obligatoria para consumir CapitalPOS CPE API.");
        }

        if (!Uri.TryCreate(BaseUrl.Trim(), UriKind.Absolute, out var baseAddress) ||
            baseAddress.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "La configuracion 'CpeApi:BaseUrl' debe ser una URL absoluta http o https valida.");
        }

        return baseAddress;
    }
}
