using System.Security.Claims;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CapitalPos.Tests;

public class PermisoEmpresaEndpointFilterTests
{
    [Fact]
    public async Task Filtro_permiso_permite_administrador()
    {
        var httpContext = CrearHttpContext(RolEmpresa.Administrador, autenticado: true);
        var filter = new PermisoEmpresaEndpointFilter(PermisoEmpresa.GestionarRoles);
        var nextWasCalled = false;

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });

        Assert.True(nextWasCalled);
        Assert.IsAssignableFrom<IResult>(result);
    }

    [Fact]
    public async Task Filtro_permiso_permite_rol_con_permiso()
    {
        var httpContext = CrearHttpContext(RolEmpresa.Cajero, autenticado: true);
        var filter = new PermisoEmpresaEndpointFilter(PermisoEmpresa.OperarCaja);
        var nextWasCalled = false;

        await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });

        Assert.True(nextWasCalled);
    }

    [Fact]
    public async Task Filtro_permiso_rechaza_rol_sin_permiso()
    {
        var httpContext = CrearHttpContext(RolEmpresa.Vendedor, autenticado: true);
        var filter = new PermisoEmpresaEndpointFilter(PermisoEmpresa.GestionarUsuarios);

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.Equal(StatusCodes.Status403Forbidden, await ObtenerStatusCodeAsync(result));
    }

    [Fact]
    public async Task Filtro_permiso_rechaza_usuario_no_autenticado()
    {
        var httpContext = CrearHttpContext(RolEmpresa.Administrador, autenticado: false);
        var filter = new PermisoEmpresaEndpointFilter(PermisoEmpresa.GestionarUsuarios);

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.Equal(StatusCodes.Status401Unauthorized, await ObtenerStatusCodeAsync(result));
    }

    [Fact]
    public async Task Filtro_permiso_rechaza_si_no_hay_empresa_activa_validada()
    {
        var httpContext = CrearHttpContext(rol: null, autenticado: true);
        var filter = new PermisoEmpresaEndpointFilter(PermisoEmpresa.GestionarUsuarios);

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.Equal(StatusCodes.Status401Unauthorized, await ObtenerStatusCodeAsync(result));
    }

    private static DefaultHttpContext CrearHttpContext(RolEmpresa? rol, bool autenticado)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmpresaPermisoAuthorizer, EmpresaPermisoAuthorizer>();
        services.AddScoped<EmpresaActivaContext>();
        services.AddScoped<IEmpresaActivaContext>(provider =>
            provider.GetRequiredService<EmpresaActivaContext>());
        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        if (autenticado)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "Test"));
        }

        if (rol.HasValue)
        {
            var empresaActiva = scope.ServiceProvider.GetRequiredService<EmpresaActivaContext>();
            empresaActiva.Establecer(Guid.NewGuid(), Guid.NewGuid(), rol.Value);
        }

        return httpContext;
    }

    private static async Task<int?> ObtenerStatusCodeAsync(object? result)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
        if (result is IResult httpResult)
        {
            await httpResult.ExecuteAsync(httpContext);
        }

        return httpContext.Response.StatusCode;
    }

    private sealed class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        public TestEndpointFilterInvocationContext(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }
}
