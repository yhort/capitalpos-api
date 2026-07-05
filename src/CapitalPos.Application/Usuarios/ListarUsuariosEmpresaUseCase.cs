using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class ListarUsuariosEmpresaUseCase
{
    private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;

    public ListarUsuariosEmpresaUseCase(IUsuarioEmpresaRepository usuarioEmpresaRepository)
    {
        _usuarioEmpresaRepository = usuarioEmpresaRepository;
    }

    public Task<IReadOnlyCollection<UsuarioEmpresa>> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        return _usuarioEmpresaRepository.ListarAsync(cancellationToken);
    }
}
