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
using CapitalPos.Infrastructure.Auditing;
using CapitalPos.Infrastructure.Cpe;
using CapitalPos.Infrastructure.Persistence;
using CapitalPos.Infrastructure.Persistence.Repositories;
using CapitalPos.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CapitalPos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCapitalPosInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CapitalPos") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexion 'ConnectionStrings:CapitalPos' es obligatoria. Configurela con dotnet user-secrets o variables de entorno.");
        }

        services.AddDbContext<CapitalPosDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEmpresaRepository, EfEmpresaRepository>();
        services.AddScoped<IUsuarioRepository, EfUsuarioRepository>();
        services.AddScoped<IUsuarioEmpresaRepository, EfUsuarioEmpresaRepository>();
        services.AddScoped<IUsuarioCredencialRepository, EfUsuarioCredencialRepository>();
        services.AddScoped<IProductoRepository, EfProductoRepository>();
        services.AddScoped<IProductoVarianteRepository, EfProductoVarianteRepository>();
        services.AddScoped<IClienteRepository, EfClienteRepository>();
        services.AddScoped<IVentaRepository, EfVentaRepository>();
        services.AddScoped<IComprobanteRepository, EfComprobanteRepository>();
        services.AddScoped<IConfiguracionFiscalEmpresaRepository, EfConfiguracionFiscalEmpresaRepository>();
        services.AddScoped<IStockProductoRepository, EfStockProductoRepository>();
        services.AddScoped<ISedeRepository, EfSedeRepository>();
        services.AddScoped<IPuntoVentaRepository, EfPuntoVentaRepository>();
        services.AddScoped<ISerieComprobanteRepository, EfSerieComprobanteRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IAuditoriaOperaciones, LoggerAuditoriaOperaciones>();
        services.AddScoped<IPasswordHasher, AspNetCoreIdentityPasswordHasher>();
        services.Configure<JwtTokenOptions>(configuration.GetSection(JwtTokenOptions.SectionName));
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.Configure<CpeApiOptions>(configuration.GetSection(CpeApiOptions.SectionName));
        services.AddHttpClient<ICpeApiHttpClient, CpeApiHttpClient>((_, httpClient) =>
            ConfigurarCpeHttpClient(configuration, httpClient));
        services.AddHttpClient<ICpeGateway, CpeApiGateway>((_, httpClient) =>
            ConfigurarCpeHttpClient(configuration, httpClient));

        return services;
    }

    private static void ConfigurarCpeHttpClient(IConfiguration configuration, HttpClient httpClient)
    {
        var options = new CpeApiOptions
        {
            BaseUrl = configuration[$"{CpeApiOptions.SectionName}:BaseUrl"] ?? string.Empty,
            ApiKey = configuration[$"{CpeApiOptions.SectionName}:ApiKey"] ?? string.Empty
        };

        httpClient.BaseAddress = options.ObtenerBaseAddress();
        httpClient.DefaultRequestHeaders.Remove(CpeApiOptions.ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(CpeApiOptions.ApiKeyHeaderName, options.ObtenerApiKey());
    }
}
