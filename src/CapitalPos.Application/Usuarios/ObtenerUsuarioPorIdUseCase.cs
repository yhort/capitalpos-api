using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class ObtenerUsuarioPorIdUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ObtenerUsuarioPorIdUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public Task<Usuario?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _usuarioRepository.ObtenerPorIdAsync(id, cancellationToken);
    }
}
