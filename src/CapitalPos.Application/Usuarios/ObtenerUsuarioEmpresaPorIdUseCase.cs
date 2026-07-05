using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class ObtenerUsuarioEmpresaPorIdUseCase
{
    private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;

    public ObtenerUsuarioEmpresaPorIdUseCase(IUsuarioEmpresaRepository usuarioEmpresaRepository)
    {
        _usuarioEmpresaRepository = usuarioEmpresaRepository;
    }

    public Task<UsuarioEmpresa?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _usuarioEmpresaRepository.ObtenerPorIdAsync(id, cancellationToken);
    }
}
