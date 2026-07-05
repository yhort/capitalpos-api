using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class CrearUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public CrearUsuarioUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<Usuario> EjecutarAsync(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = request.CrearUsuario();

        if (await _usuarioRepository.ExisteCorreoAsync(usuario.Correo, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe un usuario registrado con el mismo correo.");
        }

        await _usuarioRepository.AgregarAsync(usuario, cancellationToken);

        return usuario;
    }
}
