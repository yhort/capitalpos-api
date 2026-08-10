using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Endpoints;
using CapitalPos.Api.Authentication;
using CapitalPos.Api.Development;
using CapitalPos.Api.Middleware;
using CapitalPos.Application.Caja;
using CapitalPos.Application.Catalogo;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.Compras;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Dashboard;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Pedidos;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Reportes;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Series;
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

var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("CapitalPosWeb", policy =>
    {
        if (corsOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
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
builder.Services.AddScoped<CrearCategoriaUseCase>();
builder.Services.AddScoped<ListarCategoriasUseCase>();
builder.Services.AddScoped<CrearMarcaUseCase>();
builder.Services.AddScoped<ListarMarcasUseCase>();
builder.Services.AddScoped<CrearProductoUseCase>();
builder.Services.AddScoped<ListarProductosUseCase>();
builder.Services.AddScoped<ObtenerProductoPorIdUseCase>();
builder.Services.AddScoped<ActivarProductoUseCase>();
builder.Services.AddScoped<DesactivarProductoUseCase>();
builder.Services.AddScoped<ListarUnidadesMedidaUseCase>();
builder.Services.AddScoped<CrearProductoPresentacionUseCase>();
builder.Services.AddScoped<ListarProductoPresentacionesUseCase>();
builder.Services.AddScoped<CrearProductoVarianteUseCase>();
builder.Services.AddScoped<ListarProductoVariantesUseCase>();
builder.Services.AddScoped<ActivarProductoVarianteUseCase>();
builder.Services.AddScoped<DesactivarProductoVarianteUseCase>();
builder.Services.AddScoped<ListarReglasPrecioMayoristaUseCase>();
builder.Services.AddScoped<CrearReglaPrecioMayoristaUseCase>();
builder.Services.AddScoped<ActivarReglaPrecioMayoristaUseCase>();
builder.Services.AddScoped<DesactivarReglaPrecioMayoristaUseCase>();
builder.Services.AddScoped<CrearClienteUseCase>();
builder.Services.AddScoped<ListarClientesUseCase>();
builder.Services.AddScoped<ObtenerClientePorIdUseCase>();
builder.Services.AddScoped<CrearCompraUseCase>();
builder.Services.AddScoped<ListarComprasUseCase>();
builder.Services.AddScoped<ObtenerCompraUseCase>();
builder.Services.AddScoped<CrearVentaUseCase>();
builder.Services.AddScoped<ListarVentasUseCase>();
builder.Services.AddScoped<ObtenerVentaDetalleUseCase>();
builder.Services.AddScoped<AnularVentaUseCase>();
builder.Services.AddScoped<CrearPedidoDigitalUseCase>();
builder.Services.AddScoped<ListarPedidosDigitalesUseCase>();
builder.Services.AddScoped<ObtenerPedidoDigitalUseCase>();
builder.Services.AddScoped<CancelarPedidoDigitalUseCase>();
builder.Services.AddScoped<ActualizarEstadoPedidoDigitalUseCase>();
builder.Services.AddScoped<ConvertirPedidoDigitalAVentaUseCase>();
builder.Services.AddScoped<EmitirCpeDesdeVentaUseCase>();
builder.Services.AddScoped<EmitirNotaCreditoDesdeVentaUseCase>();
builder.Services.AddScoped<RegistrarComprobanteCpeUseCase>();
builder.Services.AddScoped<GuardarConfiguracionFiscalEmpresaUseCase>();
builder.Services.AddScoped<ObtenerConfiguracionFiscalEmpresaUseCase>();
builder.Services.AddScoped<AjustarStockProductoUseCase>();
builder.Services.AddScoped<ListarKardexUseCase>();
builder.Services.AddScoped<ObtenerStockProductoUseCase>();
builder.Services.AddScoped<ListarSedesUseCase>();
builder.Services.AddScoped<ListarPuntosVentaUseCase>();
builder.Services.AddScoped<ListarSeriesComprobanteUseCase>();
builder.Services.AddScoped<ObtenerSerieComprobanteActivaUseCase>();
builder.Services.AddScoped<AbrirSesionCajaUseCase>();
builder.Services.AddScoped<CerrarSesionCajaUseCase>();
builder.Services.AddScoped<ObtenerSesionCajaAbiertaUseCase>();
builder.Services.AddScoped<ObtenerResumenSesionCajaUseCase>();
builder.Services.AddScoped<ReporteVentasPorCanalUseCase>();
builder.Services.AddScoped<DashboardComercialUseCase>();
builder.Services.AddScoped<DashboardReporteCanalesUseCase>();
builder.Services.AddSingleton<IDashboardComercialClock, DashboardComercialClock>();

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

app.UseCors("CapitalPosWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () =>
    Results.Ok(new HealthResponse("ok", "CapitalPos.Api", DateTimeOffset.UtcNow)))
    .WithName("GetHealth");

app.MapAuthEndpoints();
app.MapEmpresaEndpoints();
app.MapUsuarioEndpoints();
app.MapCatalogoEndpoints();
app.MapUnidadMedidaEndpoints();
app.MapProductoEndpoints();
app.MapStockEndpoints();
app.MapSedeEndpoints();
app.MapCajaEndpoints();
app.MapClienteEndpoints();
app.MapCompraEndpoints();
app.MapVentaEndpoints();
app.MapPedidoDigitalEndpoints();
app.MapReporteEndpoints();
app.MapDashboardEndpoints();
app.MapConfiguracionFiscalEndpoints();
app.MapCpeEndpoints();

await app.SeedDemoDataAsync();

await app.RunAsync();

public sealed record HealthResponse(string Status, string Service, DateTimeOffset Timestamp);

public partial class Program;
