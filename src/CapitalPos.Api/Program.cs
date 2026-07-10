using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Endpoints;
using CapitalPos.Api.Authentication;
using CapitalPos.Api.Development;
using CapitalPos.Api.Middleware;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Usuarios;
using CapitalPos.Application.Ventas;
using CapitalPos.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddCapitalPosInfrastructure(builder.Configuration);
builder.Services.AddCapitalPosJwtAuthentication(builder.Configuration);
builder.Services.AddDemoSeed(builder.Configuration);
builder.Services.AddScoped<EmpresaActivaContext>();
builder.Services.AddScoped<IEmpresaActivaContext>(services =>
    services.GetRequiredService<EmpresaActivaContext>());
builder.Services.AddSingleton<IEmpresaPermisoAuthorizer, EmpresaPermisoAuthorizer>();
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
builder.Services.AddScoped<CrearProductoUseCase>();
builder.Services.AddScoped<ListarProductosUseCase>();
builder.Services.AddScoped<ObtenerProductoPorIdUseCase>();
builder.Services.AddScoped<ActivarProductoUseCase>();
builder.Services.AddScoped<DesactivarProductoUseCase>();
builder.Services.AddScoped<CrearClienteUseCase>();
builder.Services.AddScoped<ListarClientesUseCase>();
builder.Services.AddScoped<ObtenerClientePorIdUseCase>();
builder.Services.AddScoped<CrearVentaUseCase>();
builder.Services.AddScoped<EmitirCpeDesdeVentaUseCase>();
builder.Services.AddScoped<RegistrarComprobanteCpeUseCase>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () =>
    Results.Ok(new HealthResponse("ok", "CapitalPos.Api", DateTimeOffset.UtcNow)))
    .WithName("GetHealth");

app.MapAuthEndpoints();
app.MapEmpresaEndpoints();
app.MapUsuarioEndpoints();
app.MapProductoEndpoints();
app.MapClienteEndpoints();
app.MapVentaEndpoints();
app.MapCpeEndpoints();

await app.SeedDemoDataAsync();

await app.RunAsync();

public sealed record HealthResponse(string Status, string Service, DateTimeOffset Timestamp);

public partial class Program;
