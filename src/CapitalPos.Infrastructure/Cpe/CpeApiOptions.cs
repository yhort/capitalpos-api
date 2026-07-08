namespace CapitalPos.Infrastructure.Cpe;

public sealed class CpeApiOptions
{
    public const string SectionName = "CpeApi";

    public string BaseUrl { get; set; } = string.Empty;
}
