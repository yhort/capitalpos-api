namespace CapitalPos.Tests;

public class SolutionStructureTests
{
    [Fact]
    public void Application_and_domain_assemblies_have_expected_names()
    {
        var applicationAssemblyName = typeof(Application.AssemblyMarker).Assembly.GetName().Name;
        var domainAssemblyName = typeof(Domain.AssemblyMarker).Assembly.GetName().Name;

        Assert.Equal("CapitalPos.Application", applicationAssemblyName);
        Assert.Equal("CapitalPos.Domain", domainAssemblyName);
    }
}
