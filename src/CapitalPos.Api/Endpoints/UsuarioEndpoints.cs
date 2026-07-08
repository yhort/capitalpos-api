using CapitalPos.Application.Usuarios;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class UsuarioEndpoints
{
    public static IEndpointRouteBuilder MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var usuarios = app.MapGroup("/api/usuarios")
            .WithTags("Usuarios")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        usuarios.MapGet("/", ListarUsuariosAsync)
            .WithName("ListarUsuarios")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarUsuarios);

        usuarios.MapGet("/{id:guid}", ObtenerUsuarioPorIdAsync)
            .WithName("ObtenerUsuarioPorId")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarUsuarios);

        usuarios.MapPost("/", CrearUsuarioAsync)
            .WithName("CrearUsuario")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarUsuarios);

        usuarios.MapPatch("/{id:guid}/activar", ActivarUsuarioAsync)
            .WithName("ActivarUsuario")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarUsuarios);

        usuarios.MapPatch("/{id:guid}/desactivar", DesactivarUsuarioAsync)
            .WithName("DesactivarUsuario")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarUsuarios);

        var usuariosEmpresas = app.MapGroup("/api/usuarios-empresas")
            .WithTags("UsuariosEmpresas")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        usuariosEmpresas.MapGet("/", ListarUsuariosEmpresaAsync)
            .WithName("ListarUsuariosEmpresa")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarRoles);

        usuariosEmpresas.MapGet("/{id:guid}", ObtenerUsuarioEmpresaPorIdAsync)
            .WithName("ObtenerUsuarioEmpresaPorId")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarRoles);

        usuariosEmpresas.MapPost("/", AsignarUsuarioEmpresaAsync)
            .WithName("AsignarUsuarioEmpresa")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarRoles);

        usuariosEmpresas.MapPatch("/{id:guid}/activar", ActivarUsuarioEmpresaAsync)
            .WithName("ActivarUsuarioEmpresa")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarRoles);

        usuariosEmpresas.MapPatch("/{id:guid}/desactivar", DesactivarUsuarioEmpresaAsync)
            .WithName("DesactivarUsuarioEmpresa")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarRoles);

        usuariosEmpresas.MapPatch("/{id:guid}/rol", CambiarRolUsuarioEmpresaAsync)
            .WithName("CambiarRolUsuarioEmpresa")
            .RequirePermisoEmpresa(PermisoEmpresa.GestionarRoles);

        return app;
    }

    private static async Task<IResult> ListarUsuariosAsync(
        ListarUsuariosUseCase useCase,
        CancellationToken cancellationToken)
    {
        var usuarios = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(usuarios.Select(UsuarioResponse.From));
    }

    private static async Task<IResult> ObtenerUsuarioPorIdAsync(
        Guid id,
        ObtenerUsuarioPorIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var usuario = await useCase.EjecutarAsync(id, cancellationToken);

        return usuario is null
            ? Results.NotFound()
            : Results.Ok(UsuarioResponse.From(usuario));
    }

    private static async Task<IResult> CrearUsuarioAsync(
        CrearUsuarioRequest request,
        CrearUsuarioUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var usuario = await useCase.EjecutarAsync(request, cancellationToken);

            return Results.Created(
                $"/api/usuarios/{usuario.Id}",
                UsuarioResponse.From(usuario));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> ActivarUsuarioAsync(
        Guid id,
        ActivarUsuarioUseCase useCase,
        CancellationToken cancellationToken)
    {
        var usuario = await useCase.EjecutarAsync(id, cancellationToken);

        return usuario is null
            ? Results.NotFound()
            : Results.Ok(UsuarioResponse.From(usuario));
    }

    private static async Task<IResult> DesactivarUsuarioAsync(
        Guid id,
        DesactivarUsuarioUseCase useCase,
        CancellationToken cancellationToken)
    {
        var usuario = await useCase.EjecutarAsync(id, cancellationToken);

        return usuario is null
            ? Results.NotFound()
            : Results.Ok(UsuarioResponse.From(usuario));
    }

    private static async Task<IResult> ListarUsuariosEmpresaAsync(
        ListarUsuariosEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var usuariosEmpresa = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(usuariosEmpresa.Select(UsuarioEmpresaResponse.From));
    }

    private static async Task<IResult> ObtenerUsuarioEmpresaPorIdAsync(
        Guid id,
        ObtenerUsuarioEmpresaPorIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var usuarioEmpresa = await useCase.EjecutarAsync(id, cancellationToken);

        return usuarioEmpresa is null
            ? Results.NotFound()
            : Results.Ok(UsuarioEmpresaResponse.From(usuarioEmpresa));
    }

    private static async Task<IResult> AsignarUsuarioEmpresaAsync(
        AsignarUsuarioEmpresaRequest request,
        AsignarUsuarioEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var usuarioEmpresa = await useCase.EjecutarAsync(request, cancellationToken);

            return Results.Created(
                $"/api/usuarios-empresas/{usuarioEmpresa.Id}",
                UsuarioEmpresaResponse.From(usuarioEmpresa));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> ActivarUsuarioEmpresaAsync(
        Guid id,
        ActivarUsuarioEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var usuarioEmpresa = await useCase.EjecutarAsync(id, cancellationToken);

        return usuarioEmpresa is null
            ? Results.NotFound()
            : Results.Ok(UsuarioEmpresaResponse.From(usuarioEmpresa));
    }

    private static async Task<IResult> DesactivarUsuarioEmpresaAsync(
        Guid id,
        DesactivarUsuarioEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        var usuarioEmpresa = await useCase.EjecutarAsync(id, cancellationToken);

        return usuarioEmpresa is null
            ? Results.NotFound()
            : Results.Ok(UsuarioEmpresaResponse.From(usuarioEmpresa));
    }

    private static async Task<IResult> CambiarRolUsuarioEmpresaAsync(
        Guid id,
        CambiarRolUsuarioEmpresaRequest request,
        CambiarRolUsuarioEmpresaUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var usuarioEmpresa = await useCase.EjecutarAsync(id, request, cancellationToken);

            return usuarioEmpresa is null
                ? Results.NotFound()
                : Results.Ok(UsuarioEmpresaResponse.From(usuarioEmpresa));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }
}

public sealed record UsuarioResponse(
    Guid Id,
    string Nombre,
    string Apellido,
    string Correo,
    bool Activo,
    DateTimeOffset FechaCreacion)
{
    public static UsuarioResponse From(Usuario usuario)
    {
        return new UsuarioResponse(
            usuario.Id,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Correo,
            usuario.Activo,
            usuario.FechaCreacion);
    }
}

public sealed record UsuarioEmpresaResponse(
    Guid Id,
    Guid UsuarioId,
    Guid EmpresaId,
    RolEmpresa Rol,
    bool Activo,
    DateTimeOffset FechaAsignacion)
{
    public static UsuarioEmpresaResponse From(UsuarioEmpresa usuarioEmpresa)
    {
        return new UsuarioEmpresaResponse(
            usuarioEmpresa.Id,
            usuarioEmpresa.UsuarioId,
            usuarioEmpresa.EmpresaId,
            usuarioEmpresa.Rol,
            usuarioEmpresa.Activo,
            usuarioEmpresa.FechaAsignacion);
    }
}
