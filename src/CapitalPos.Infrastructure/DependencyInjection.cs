using CapitalPos.Application.Empresas;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Usuarios;
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
        services.AddScoped<IPasswordHasher, AspNetCoreIdentityPasswordHasher>();
        services.Configure<JwtTokenOptions>(configuration.GetSection(JwtTokenOptions.SectionName));
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.Configure<CpeApiOptions>(configuration.GetSection(CpeApiOptions.SectionName));
        services.AddHttpClient<ICpeApiHttpClient, CpeApiHttpClient>((_, httpClient) =>
        {
            var baseUrl = configuration[$"{CpeApiOptions.SectionName}:BaseUrl"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "La configuracion 'CpeApi:BaseUrl' es obligatoria para consumir CapitalPOS CPE API.");
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseAddress) ||
                baseAddress.Scheme is not ("http" or "https"))
            {
                throw new InvalidOperationException(
                    "La configuracion 'CpeApi:BaseUrl' debe ser una URL absoluta http o https valida.");
            }

            httpClient.BaseAddress = baseAddress;
        });

        return services;
    }
}
