using CapitalPos.Application.Empresas;
using CapitalPos.Application.Usuarios;
using CapitalPos.Infrastructure.Persistence;
using CapitalPos.Infrastructure.Persistence.Repositories;
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

        return services;
    }
}
