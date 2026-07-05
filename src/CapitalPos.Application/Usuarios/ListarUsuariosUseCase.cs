using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class ListarUsuariosUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ListarUsuariosUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public Task<IReadOnlyCollection<Usuario>> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        return _usuarioRepository.ListarAsync(cancellationToken);
    }
}
