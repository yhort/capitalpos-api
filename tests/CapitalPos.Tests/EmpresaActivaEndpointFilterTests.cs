using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CapitalPos.Tests;

public class EmpresaActivaEndpointFilterTests
{
    [Fact]
    public async Task Filtro_establece_empresa_activa_con_header_usuario_y_asignacion_valida()
    {
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            usuarioId,
            empresaId,
            RolEmpresa.Administrador);
        var httpContext = CrearHttpContext(usuarioId, empresaId, [asignacion]);
        var filter = new EmpresaActivaEndpointFilter();
        var nextWasCalled = false;

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });

        var empresaActiva = httpContext.RequestServices.GetRequiredService<EmpresaActivaContext>();
        Assert.True(nextWasCalled);
        Assert.True(empresaActiva.TieneEmpresaActiva);
        Assert.Equal(usuarioId, empresaActiva.UsuarioId);
        Assert.Equal(empresaId, empresaActiva.EmpresaId);
        Assert.Equal(RolEmpresa.Administrador, empresaActiva.Rol);
        Assert.IsAssignableFrom<IResult>(result);
    }

    [Fact]
    public async Task Filtro_devuelve_bad_request_si_falta_header()
    {
        var httpContext = CrearHttpContext(Guid.NewGuid(), empresaId: null, []);
        var filter = new EmpresaActivaEndpointFilter();

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.Equal(StatusCodes.Status400BadRequest, await ObtenerStatusCodeAsync(result));
    }

    [Fact]
    public async Task Filtro_devuelve_bad_request_si_header_tiene_formato_invalido()
    {
        var httpContext = CrearHttpContext(Guid.NewGuid(), empresaId: null, []);
        httpContext.Request.Headers[EmpresaActivaHeaders.HeaderName] = "no-es-guid";
        var filter = new EmpresaActivaEndpointFilter();

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.Equal(StatusCodes.Status400BadRequest, await ObtenerStatusCodeAsync(result));
    }

    [Fact]
    public async Task Filtro_devuelve_unauthorized_si_no_hay_usuario_autenticado()
    {
        var empresaId = Guid.NewGuid();
        var httpContext = CrearHttpContext(usuarioId: null, empresaId, []);
        var filter = new EmpresaActivaEndpointFilter();

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.Equal(StatusCodes.Status401Unauthorized, await ObtenerStatusCodeAsync(result));
    }

    [Fact]
    public async Task Filtro_devuelve_forbidden_si_usuario_no_pertenece_a_empresa()
    {
        var httpContext = CrearHttpContext(Guid.NewGuid(), Guid.NewGuid(), []);
        var filter = new EmpresaActivaEndpointFilter();

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.Equal(StatusCodes.Status403Forbidden, await ObtenerStatusCodeAsync(result));
    }

    [Fact]
    public async Task Filtro_devuelve_forbidden_si_asignacion_esta_inactiva()
    {
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            usuarioId,
            empresaId,
            RolEmpresa.Cajero,
            activo: false);
        var httpContext = CrearHttpContext(usuarioId, empresaId, [asignacion]);
        var filter = new EmpresaActivaEndpointFilter();

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.Equal(StatusCodes.Status403Forbidden, await ObtenerStatusCodeAsync(result));
    }

    private static DefaultHttpContext CrearHttpContext(
        Guid? usuarioId,
        Guid? empresaId,
        IReadOnlyCollection<UsuarioEmpresa> asignaciones)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUsuarioEmpresaRepository>(
            new UsuarioEmpresaRepositoryFake(asignaciones));
        services.AddScoped<EmpresaActivaContext>();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider.CreateScope().ServiceProvider
        };

        if (usuarioId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(JwtRegisteredClaimNames.Sub, usuarioId.Value.ToString())],
                "Test"));
        }

        if (empresaId.HasValue)
        {
            httpContext.Request.Headers[EmpresaActivaHeaders.HeaderName] = empresaId.Value.ToString();
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

    private sealed class UsuarioEmpresaRepositoryFake : IUsuarioEmpresaRepository
    {
        private readonly IReadOnlyCollection<UsuarioEmpresa> _asignaciones;

        public UsuarioEmpresaRepositoryFake(IReadOnlyCollection<UsuarioEmpresa> asignaciones)
        {
            _asignaciones = asignaciones;
        }

        public Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<UsuarioEmpresa>> ListarAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<UsuarioEmpresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<UsuarioEmpresa?> ObtenerPorUsuarioYEmpresaAsync(
            Guid usuarioId,
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            var asignacion = _asignaciones.SingleOrDefault(asignacion =>
                asignacion.UsuarioId == usuarioId &&
                asignacion.EmpresaId == empresaId);

            return Task.FromResult(asignacion);
        }

        public Task ActualizarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExisteAsignacionAsync(
            Guid usuarioId,
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
