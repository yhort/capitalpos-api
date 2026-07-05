using CapitalPos.Api.Endpoints;
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
builder.Services.AddScoped<CrearEmpresaUseCase>();
builder.Services.AddScoped<CrearUsuarioUseCase>();
builder.Services.AddScoped<AsignarUsuarioEmpresaUseCase>();
builder.Services.AddScoped<ListarEmpresasUseCase>();
builder.Services.AddScoped<ListarUsuariosUseCase>();
builder.Services.AddScoped<ListarUsuariosEmpresaUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/health", () =>
    Results.Ok(new HealthResponse("ok", "CapitalPos.Api", DateTimeOffset.UtcNow)))
    .WithName("GetHealth");

app.MapEmpresaEndpoints();
app.MapUsuarioEndpoints();

app.Run();

public sealed record HealthResponse(string Status, string Service, DateTimeOffset Timestamp);
