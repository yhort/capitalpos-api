using CapitalPos.Application.Empresas;
using CapitalPos.Application.Usuarios;
using CapitalPos.Infrastructure;
using CapitalPos.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace CapitalPos.Tests;

public class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void Add_capital_pos_infrastructure_rechaza_cadena_de_conexion_vacia()
    {
        var services = new ServiceCollection();
        var configuration = CrearConfiguracion(capitalPosConnectionString: string.Empty);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCapitalPosInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:CapitalPos", exception.Message);
    }

    [Fact]
    public void Add_capital_pos_infrastructure_registra_repositorios_ef()
    {
        var services = new ServiceCollection();
        var configuration = CrearConfiguracion("Host=localhost;Database=capitalpos_test");

        services.AddCapitalPosInfrastructure(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IEmpresaRepository) &&
            descriptor.ImplementationType == typeof(EfEmpresaRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IUsuarioRepository) &&
            descriptor.ImplementationType == typeof(EfUsuarioRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IUsuarioEmpresaRepository) &&
            descriptor.ImplementationType == typeof(EfUsuarioEmpresaRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    private static IConfiguration CrearConfiguracion(string capitalPosConnectionString)
    {
        return new TestConfiguration(capitalPosConnectionString);
    }

    private sealed class TestConfiguration : IConfiguration
    {
        private readonly string _capitalPosConnectionString;

        public TestConfiguration(string capitalPosConnectionString)
        {
            _capitalPosConnectionString = capitalPosConnectionString;
        }

        public string? this[string key]
        {
            get => key == "ConnectionStrings:CapitalPos" ? _capitalPosConnectionString : null;
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return [];
        }

        public IChangeToken GetReloadToken()
        {
            return new TestChangeToken();
        }

        public IConfigurationSection GetSection(string key)
        {
            return new TestConfigurationSection(
                key,
                key == "ConnectionStrings" ? _capitalPosConnectionString : null);
        }
    }

    private sealed class TestConfigurationSection : IConfigurationSection
    {
        private readonly string? _capitalPosConnectionString;

        public TestConfigurationSection(string key, string? capitalPosConnectionString)
        {
            Key = key;
            Path = key;
            _capitalPosConnectionString = capitalPosConnectionString;
        }

        public string? this[string key]
        {
            get => key == "CapitalPos" ? _capitalPosConnectionString : null;
            set => throw new NotSupportedException();
        }

        public string Key { get; }

        public string Path { get; }

        public string? Value { get; set; }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return [];
        }

        public IChangeToken GetReloadToken()
        {
            return new TestChangeToken();
        }

        public IConfigurationSection GetSection(string key)
        {
            return new TestConfigurationSection(key, null);
        }
    }

    private sealed class TestChangeToken : IChangeToken
    {
        public bool ActiveChangeCallbacks => false;

        public bool HasChanged => false;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            return new EmptyDisposable();
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
