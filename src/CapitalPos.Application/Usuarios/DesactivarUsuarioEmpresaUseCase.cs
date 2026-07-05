using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class DesactivarUsuarioEmpresaUseCase
{
    private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;

    public DesactivarUsuarioEmpresaUseCase(IUsuarioEmpresaRepository usuarioEmpresaRepository)
    {
        _usuarioEmpresaRepository = usuarioEmpresaRepository;
    }

    public async Task<UsuarioEmpresa?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var usuarioEmpresa = await _usuarioEmpresaRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (usuarioEmpresa is null)
        {
            return null;
        }

        usuarioEmpresa.Desactivar();
        await _usuarioEmpresaRepository.ActualizarAsync(usuarioEmpresa, cancellationToken);

        return usuarioEmpresa;
    }
}
