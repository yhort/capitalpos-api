using CapitalPos.Application.Empresas;
using CapitalPos.Application.Usuarios;
using CapitalPos.Infrastructure.Persistence.InMemory;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

builder.Services.AddSingleton<IEmpresaRepository, InMemoryEmpresaRepository>();
builder.Services.AddSingleton<IUsuarioRepository, InMemoryUsuarioRepository>();
builder.Services.AddSingleton<IUsuarioEmpresaRepository, InMemoryUsuarioEmpresaRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/health", () =>
    Results.Ok(new HealthResponse("ok", "CapitalPos.Api", DateTimeOffset.UtcNow)))
    .WithName("GetHealth");

app.Run();

public sealed record HealthResponse(string Status, string Service, DateTimeOffset Timestamp);
