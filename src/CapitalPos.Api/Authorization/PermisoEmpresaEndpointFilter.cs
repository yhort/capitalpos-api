using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Seguridad;

namespace CapitalPos.Api.Authorization;

public sealed class PermisoEmpresaEndpointFilter : IEndpointFilter
{
    private readonly PermisoEmpresa _permiso;

    public PermisoEmpresaEndpointFilter(PermisoEmpresa permiso)
    {
        _permiso = permiso;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var empresaActiva = httpContext.RequestServices.GetRequiredService<IEmpresaActivaContext>();
        if (!empresaActiva.TieneEmpresaActiva)
        {
            return Results.Unauthorized();
        }

        var authorizer = httpContext.RequestServices.GetRequiredService<IEmpresaPermisoAuthorizer>();
        if (!authorizer.TienePermiso(empresaActiva.Rol, _permiso))
        {
            return Results.Json(
                ErrorResponse.From("El usuario autenticado no tiene el permiso requerido."),
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}
