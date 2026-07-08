using CapitalPos.Application.Seguridad;

namespace CapitalPos.Api.Authorization;

public static class PermisoEmpresaEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermisoEmpresa(
        this RouteHandlerBuilder builder,
        PermisoEmpresa permiso)
    {
        return builder
            .WithMetadata(new PermisoEmpresaEndpointMetadata(permiso))
            .AddEndpointFilter(new PermisoEmpresaEndpointFilter(permiso));
    }
}
