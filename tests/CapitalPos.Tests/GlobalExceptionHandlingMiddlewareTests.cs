using System.Text.Json;
using CapitalPos.Api.Endpoints;
using CapitalPos.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace CapitalPos.Tests;

public class GlobalExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Middleware_convierte_excepcion_no_controlada_en_respuesta_500_segura()
    {
        var context = CrearHttpContext();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("detalle interno sensible"),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.NotNull(response);
        Assert.Equal("Ocurrio un error inesperado al procesar la solicitud.", response.Message);
        Assert.DoesNotContain("detalle interno sensible", response.Message);
        Assert.DoesNotContain("InvalidOperationException", response.Message);
    }

    [Fact]
    public async Task Middleware_no_interfiere_si_no_hay_excepcion()
    {
        var context = CrearHttpContext();
        var middleware = new GlobalExceptionHandlingMiddleware(
            async httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                await Task.CompletedTask;
            },
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public void Program_registra_middleware_global_antes_de_autenticacion_y_endpoints()
    {
        var source = File.ReadAllText(ResolverRutaRepo("src/CapitalPos.Api/Program.cs"));
        var middlewareIndex = source.IndexOf(
            "app.UseMiddleware<GlobalExceptionHandlingMiddleware>();",
            StringComparison.Ordinal);
        var authenticationIndex = source.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        var endpointIndex = source.IndexOf("app.MapGet(\"/api/health\"", StringComparison.Ordinal);

        Assert.True(middlewareIndex >= 0);
        Assert.True(authenticationIndex > middlewareIndex);
        Assert.True(endpointIndex > middlewareIndex);
    }

    private static DefaultHttpContext CrearHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/prueba";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static string ResolverRutaRepo(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "CapitalPos.Api.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(directory.FullName, relativePath);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo resolver la raiz del repositorio.");
    }
}
