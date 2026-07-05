using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class CambiarRolUsuarioEmpresaUseCase
{
    private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;

    public CambiarRolUsuarioEmpresaUseCase(IUsuarioEmpresaRepository usuarioEmpresaRepository)
    {
        _usuarioEmpresaRepository = usuarioEmpresaRepository;
    }

    public async Task<UsuarioEmpresa?> EjecutarAsync(
        Guid id,
        CambiarRolUsuarioEmpresaRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuarioEmpresa = await _usuarioEmpresaRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (usuarioEmpresa is null)
        {
            return null;
        }

        usuarioEmpresa.CambiarRol(request.Rol);
        await _usuarioEmpresaRepository.ActualizarAsync(usuarioEmpresa, cancellationToken);

        return usuarioEmpresa;
    }
}
