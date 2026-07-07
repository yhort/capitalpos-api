using System.IdentityModel.Tokens.Jwt;
using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Usuarios;

namespace CapitalPos.Api.ActiveCompany;

public sealed class EmpresaActivaEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!TryGetEmpresaId(httpContext, out var empresaId, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        if (!TryGetUsuarioId(httpContext, out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var usuarioEmpresaRepository = httpContext.RequestServices
            .GetRequiredService<IUsuarioEmpresaRepository>();
        var usuarioEmpresa = await usuarioEmpresaRepository.ObtenerPorUsuarioYEmpresaAsync(
            usuarioId,
            empresaId,
            httpContext.RequestAborted);

        if (usuarioEmpresa is null || !usuarioEmpresa.Activo)
        {
            return Results.Json(
                ErrorResponse.From("El usuario no pertenece a la empresa activa indicada o la relacion esta inactiva."),
                statusCode: StatusCodes.Status403Forbidden);
        }

        var empresaActivaContext = httpContext.RequestServices
            .GetRequiredService<EmpresaActivaContext>();
        empresaActivaContext.Establecer(usuarioId, empresaId, usuarioEmpresa.Rol);

        return await next(context);
    }

    private static bool TryGetEmpresaId(
        HttpContext httpContext,
        out Guid empresaId,
        out string error)
    {
        empresaId = Guid.Empty;
        error = string.Empty;

        if (!httpContext.Request.Headers.TryGetValue(EmpresaActivaHeaders.HeaderName, out var values) ||
            string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            error = $"El header '{EmpresaActivaHeaders.HeaderName}' es obligatorio.";
            return false;
        }

        if (!Guid.TryParse(values.FirstOrDefault(), out empresaId) || empresaId == Guid.Empty)
        {
            error = $"El header '{EmpresaActivaHeaders.HeaderName}' debe contener un identificador de empresa valido.";
            return false;
        }

        return true;
    }

    private static bool TryGetUsuarioId(HttpContext httpContext, out Guid usuarioId)
    {
        usuarioId = Guid.Empty;
        var sub = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(sub, out usuarioId) && usuarioId != Guid.Empty;
    }
}
