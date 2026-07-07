namespace CapitalPos.Infrastructure.Security;

public sealed class JwtTokenOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CapitalPos.Api";

    public string Audience { get; set; } = "CapitalPos.Web";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
}
