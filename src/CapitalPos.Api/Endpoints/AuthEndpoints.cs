using CapitalPos.Application.Seguridad;

namespace CapitalPos.Api.Endpoints;

public static class AuthEndpoints
{
    private const string ErrorCredencialesInvalidas =
        "Las credenciales son invalidas o el acceso no esta disponible.";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Autenticacion");

        group.MapPost("/login", LoginAsync)
            .WithName("Login");

        return app;
    }

    private static async Task<IResult> LoginAsync(
        AuthLoginRequest request,
        LoginUseCase loginUseCase,
        IAccessTokenIssuer accessTokenIssuer,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(ErrorResponse.From("El correo y la contrasena son obligatorios."));
        }

        var loginResult = await loginUseCase.EjecutarAsync(
            new LoginRequest(request.Correo, request.Password),
            cancellationToken);

        if (!loginResult.EsValido ||
            loginResult.UsuarioId is null ||
            string.IsNullOrWhiteSpace(loginResult.Correo))
        {
            return Results.Json(
                ErrorResponse.From(ErrorCredencialesInvalidas),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var token = accessTokenIssuer.Emitir(new AccessTokenRequest(
            loginResult.UsuarioId.Value,
            loginResult.Correo));
        var expiresIn = Convert.ToInt32(Math.Max(
            0,
            Math.Floor((token.ExpiraEn - DateTimeOffset.UtcNow).TotalSeconds)));

        return Results.Ok(new AuthLoginResponse(
            token.Token,
            "Bearer",
            expiresIn,
            token.ExpiraEn.UtcDateTime,
            new AuthUsuarioResponse(
                loginResult.UsuarioId.Value,
                loginResult.Correo)));
    }
}

public sealed record AuthLoginRequest(string? Correo, string? Password);

public sealed record AuthLoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTime ExpiresAtUtc,
    AuthUsuarioResponse Usuario);

public sealed record AuthUsuarioResponse(
    Guid Id,
    string Correo);
