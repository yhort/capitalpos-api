using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class AsignarUsuarioEmpresaUseCase
{
    private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;

    public AsignarUsuarioEmpresaUseCase(IUsuarioEmpresaRepository usuarioEmpresaRepository)
    {
        _usuarioEmpresaRepository = usuarioEmpresaRepository;
    }

    public async Task<UsuarioEmpresa> EjecutarAsync(
        AsignarUsuarioEmpresaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuarioEmpresa = request.CrearAsignacion();

        var existeAsignacion = await _usuarioEmpresaRepository.ExisteAsignacionAsync(
            usuarioEmpresa.UsuarioId,
            usuarioEmpresa.EmpresaId,
            cancellationToken);
        if (existeAsignacion)
        {
            throw new InvalidOperationException("El usuario ya pertenece a la empresa indicada.");
        }

        await _usuarioEmpresaRepository.AgregarAsync(usuarioEmpresa, cancellationToken);

        return usuarioEmpresa;
    }
}
