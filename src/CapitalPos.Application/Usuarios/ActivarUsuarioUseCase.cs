using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class ActivarUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ActivarUsuarioUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<Usuario?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        usuario.Activar();
        await _usuarioRepository.ActualizarAsync(usuario, cancellationToken);

        return usuario;
    }
}
