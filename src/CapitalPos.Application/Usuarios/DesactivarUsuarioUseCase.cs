using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class DesactivarUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public DesactivarUsuarioUseCase(IUsuarioRepository usuarioRepository)
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

        usuario.Desactivar();
        await _usuarioRepository.ActualizarAsync(usuario, cancellationToken);

        return usuario;
    }
}
