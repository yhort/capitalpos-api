using CapitalPos.Application.Usuarios;

namespace CapitalPos.Application.Seguridad;

public sealed class LoginUseCase
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUsuarioCredencialRepository _usuarioCredencialRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public LoginUseCase(
        IUsuarioRepository usuarioRepository,
        IUsuarioCredencialRepository usuarioCredencialRepository,
        IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _usuarioCredencialRepository = usuarioCredencialRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult> EjecutarAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correo = NormalizarCorreo(request.Correo);
        if (string.IsNullOrWhiteSpace(correo))
        {
            return new LoginResult(LoginStatus.UsuarioNoEncontrado);
        }

        var usuario = await _usuarioRepository.ObtenerPorCorreoAsync(correo, cancellationToken);
        if (usuario is null)
        {
            return new LoginResult(LoginStatus.UsuarioNoEncontrado);
        }

        var credencial = await _usuarioCredencialRepository.ObtenerPorUsuarioIdAsync(
            usuario.Id,
            cancellationToken);
        if (credencial is null)
        {
            return new LoginResult(LoginStatus.CredencialNoEncontrada);
        }

        if (!credencial.Activo)
        {
            return new LoginResult(LoginStatus.CredencialInactiva);
        }

        if (credencial.Bloqueado)
        {
            return new LoginResult(LoginStatus.CredencialBloqueada);
        }

        var verificacion = _passwordHasher.Verificar(credencial, request.Password);
        if (!verificacion.EsValida)
        {
            return new LoginResult(LoginStatus.PasswordIncorrecto);
        }

        return new LoginResult(
            LoginStatus.CredencialesValidas,
            usuario.Id,
            usuario.Correo,
            verificacion.RequiereRehash);
    }

    private static string NormalizarCorreo(string? correo)
    {
        return correo?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
