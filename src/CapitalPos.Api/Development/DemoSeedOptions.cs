namespace CapitalPos.Api.Development;

public sealed class DemoSeedOptions
{
    public const string SectionName = "DemoSeed";

    public bool Enabled { get; set; }

    public string AdminPassword { get; set; } = string.Empty;
}
