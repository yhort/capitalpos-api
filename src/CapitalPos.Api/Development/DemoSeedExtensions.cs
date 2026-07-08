namespace CapitalPos.Api.Development;

public static class DemoSeedExtensions
{
    public static IServiceCollection AddDemoSeed(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DemoSeedOptions>(configuration.GetSection(DemoSeedOptions.SectionName));
        services.AddScoped<IDemoSeedStore>(services =>
            new EfDemoSeedStore(services.GetRequiredService<CapitalPos.Infrastructure.Persistence.CapitalPosDbContext>()));
        services.AddScoped<DemoDataSeeder>();

        return services;
    }

    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var enabled = app.Configuration.GetValue<bool>($"{DemoSeedOptions.SectionName}:Enabled");
        if (!enabled)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.EjecutarAsync(app.Environment);
    }
}
