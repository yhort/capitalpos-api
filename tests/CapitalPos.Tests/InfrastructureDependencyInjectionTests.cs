using CapitalPos.Application.Auditoria;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Persistence;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Series;
using CapitalPos.Application.Usuarios;
using CapitalPos.Application.Ventas;
using CapitalPos.Infrastructure;
using CapitalPos.Infrastructure.Auditing;
using CapitalPos.Infrastructure.Cpe;
using CapitalPos.Infrastructure.Persistence.Repositories;
using CapitalPos.Infrastructure.Persistence;
using CapitalPos.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IUsuarioCredencialRepository) &&
            descriptor.ImplementationType == typeof(EfUsuarioCredencialRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IProductoRepository) &&
            descriptor.ImplementationType == typeof(EfProductoRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IProductoVarianteRepository) &&
            descriptor.ImplementationType == typeof(EfProductoVarianteRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IClienteRepository) &&
            descriptor.ImplementationType == typeof(EfClienteRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IVentaRepository) &&
            descriptor.ImplementationType == typeof(EfVentaRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IComprobanteRepository) &&
            descriptor.ImplementationType == typeof(EfComprobanteRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IConfiguracionFiscalEmpresaRepository) &&
            descriptor.ImplementationType == typeof(EfConfiguracionFiscalEmpresaRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IStockProductoRepository) &&
            descriptor.ImplementationType == typeof(EfStockProductoRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ISedeRepository) &&
            descriptor.ImplementationType == typeof(EfSedeRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IPuntoVentaRepository) &&
            descriptor.ImplementationType == typeof(EfPuntoVentaRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ISerieComprobanteRepository) &&
            descriptor.ImplementationType == typeof(EfSerieComprobanteRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IUnitOfWork) &&
            descriptor.ImplementationType == typeof(EfUnitOfWork) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAuditoriaOperaciones) &&
            descriptor.ImplementationType == typeof(LoggerAuditoriaOperaciones) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IPasswordHasher) &&
            descriptor.ImplementationType == typeof(AspNetCoreIdentityPasswordHasher) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAccessTokenIssuer) &&
            descriptor.ImplementationType == typeof(JwtAccessTokenIssuer) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ICpeApiHttpClient) &&
            descriptor.Lifetime == ServiceLifetime.Transient);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ICpeGateway) &&
            descriptor.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Cliente_http_cpe_usa_base_url_configurada()
    {
        var services = new ServiceCollection();
        var configuration = CrearConfiguracion(
            "Host=localhost;Database=capitalpos_test",
            cpeApiBaseUrl: "https://cpe.capitalpos.test/");

        services.AddCapitalPosInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ICpeApiHttpClient>();

        Assert.Equal(new Uri("https://cpe.capitalpos.test/"), client.BaseAddress);
    }

    [Fact]
    public void Cliente_http_cpe_envia_header_api_key_configurada()
    {
        const string apiKey = "capitalpos-cpe-test-api-key";
        var services = new ServiceCollection();
        var configuration = CrearConfiguracion(
            "Host=localhost;Database=capitalpos_test",
            cpeApiApiKey: apiKey);

        services.AddCapitalPosInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ICpeApiHttpClient>();
        var httpClient = ObtenerHttpClient(client);
        var headerValues = httpClient.DefaultRequestHeaders.GetValues(CpeApiOptions.ApiKeyHeaderName);

        Assert.Equal([apiKey], headerValues);
    }

    [Fact]
    public void Gateway_cpe_usa_base_url_y_api_key_configuradas()
    {
        const string apiKey = "capitalpos-cpe-test-api-key";
        var services = new ServiceCollection();
        var configuration = CrearConfiguracion(
            "Host=localhost;Database=capitalpos_test",
            cpeApiBaseUrl: "https://cpe.capitalpos.test/",
            cpeApiApiKey: apiKey);

        services.AddCapitalPosInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var gateway = provider.GetRequiredService<ICpeGateway>();
        var httpClient = ObtenerHttpClient(gateway);
        var headerValues = httpClient.DefaultRequestHeaders.GetValues(CpeApiOptions.ApiKeyHeaderName);

        Assert.Equal(new Uri("https://cpe.capitalpos.test/"), httpClient.BaseAddress);
        Assert.Equal([apiKey], headerValues);
    }

    [Fact]
    public void Cliente_http_cpe_rechaza_base_url_vacia_al_resolverse()
    {
        var services = new ServiceCollection();
        var configuration = CrearConfiguracion(
            "Host=localhost;Database=capitalpos_test",
            cpeApiBaseUrl: string.Empty);

        services.AddCapitalPosInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ICpeApiHttpClient>());

        Assert.Contains("CpeApi:BaseUrl", exception.Message);
    }

    [Fact]
    public void Cliente_http_cpe_rechaza_api_key_vacia_al_resolverse()
    {
        var services = new ServiceCollection();
        var configuration = CrearConfiguracion(
            "Host=localhost;Database=capitalpos_test",
            cpeApiApiKey: string.Empty);

        services.AddCapitalPosInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ICpeApiHttpClient>());

        Assert.Contains("CpeApi:ApiKey", exception.Message);
    }

    [Fact]
    public void Cliente_http_cpe_rechaza_base_url_relativa_al_resolverse()
    {
        var services = new ServiceCollection();
        var configuration = CrearConfiguracion(
            "Host=localhost;Database=capitalpos_test",
            cpeApiBaseUrl: "/api/cpe");

        services.AddCapitalPosInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ICpeApiHttpClient>());

        Assert.Contains("CpeApi:BaseUrl", exception.Message);
    }

    [Fact]
    public void Opciones_cpe_normalizan_base_url_desde_configuracion()
    {
        var options = new CpeApiOptions
        {
            BaseUrl = " https://cpe.capitalpos.test/api/ "
        };

        var baseAddress = options.ObtenerBaseAddress();

        Assert.Equal(new Uri("https://cpe.capitalpos.test/api/"), baseAddress);
    }

    [Fact]
    public void Opciones_cpe_normalizan_api_key_desde_configuracion()
    {
        var options = new CpeApiOptions
        {
            ApiKey = " capitalpos-cpe-test-api-key "
        };

        var apiKey = options.ObtenerApiKey();

        Assert.Equal("capitalpos-cpe-test-api-key", apiKey);
    }

    [Fact]
    public void Opciones_cpe_rechazan_api_key_vacia_sin_exponer_valores()
    {
        const string apiKey = "capitalpos-cpe-test-api-key";
        var options = new CpeApiOptions
        {
            ApiKey = " "
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            options.ObtenerApiKey());

        Assert.Contains("CpeApi:ApiKey", exception.Message);
        Assert.DoesNotContain(apiKey, exception.Message);
        Assert.DoesNotContain("X-API-KEY", exception.Message);
    }

    [Fact]
    public void Opciones_cpe_rechazan_esquemas_no_http()
    {
        var options = new CpeApiOptions
        {
            BaseUrl = "ftp://cpe.capitalpos.test"
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            options.ObtenerBaseAddress());

        Assert.Contains("CpeApi:BaseUrl", exception.Message);
    }

    private static IConfiguration CrearConfiguracion(
        string capitalPosConnectionString,
        string cpeApiBaseUrl = "https://cpe.capitalpos.test/",
        string cpeApiApiKey = "capitalpos-cpe-test-api-key")
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:CapitalPos"] = capitalPosConnectionString,
            ["CpeApi:BaseUrl"] = cpeApiBaseUrl,
            ["CpeApi:ApiKey"] = cpeApiApiKey
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static HttpClient ObtenerHttpClient(ICpeApiHttpClient client)
    {
        var field = typeof(CpeApiHttpClient).GetField(
            "_httpClient",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(field);
        return Assert.IsType<HttpClient>(field.GetValue(client));
    }

    private static HttpClient ObtenerHttpClient(ICpeGateway gateway)
    {
        var field = typeof(CpeApiGateway).GetField(
            "_httpClient",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(field);
        return Assert.IsType<HttpClient>(field.GetValue(gateway));
    }
}
