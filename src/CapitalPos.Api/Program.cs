using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Usuarios;
using CapitalPos.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

builder.Services.AddCapitalPosInfrastructure(builder.Configuration);
builder.Services.AddScoped<CrearEmpresaUseCase>();
builder.Services.AddScoped<CrearUsuarioUseCase>();
builder.Services.AddScoped<AsignarUsuarioEmpresaUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<ListarEmpresasUseCase>();
builder.Services.AddScoped<ListarUsuariosUseCase>();
builder.Services.AddScoped<ListarUsuariosEmpresaUseCase>();
builder.Services.AddScoped<ObtenerEmpresaPorIdUseCase>();
builder.Services.AddScoped<ObtenerUsuarioPorIdUseCase>();
builder.Services.AddScoped<ObtenerUsuarioEmpresaPorIdUseCase>();
builder.Services.AddScoped<ActivarEmpresaUseCase>();
builder.Services.AddScoped<DesactivarEmpresaUseCase>();
builder.Services.AddScoped<ActivarUsuarioUseCase>();
builder.Services.AddScoped<DesactivarUsuarioUseCase>();
builder.Services.AddScoped<ActivarUsuarioEmpresaUseCase>();
builder.Services.AddScoped<DesactivarUsuarioEmpresaUseCase>();
builder.Services.AddScoped<CambiarRolUsuarioEmpresaUseCase>();

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
