using CapitalPos.Application.Empresas;
using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class AsignarUsuarioEmpresaUseCase
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public AsignarUsuarioEmpresaUseCase(
        IUsuarioEmpresaRepository usuarioEmpresaRepository,
        IUsuarioRepository usuarioRepository,
        IEmpresaRepository empresaRepository)
    {
        _usuarioEmpresaRepository = usuarioEmpresaRepository;
        _usuarioRepository = usuarioRepository;
        _empresaRepository = empresaRepository;
    }

    public async Task<UsuarioEmpresa> EjecutarAsync(
        AsignarUsuarioEmpresaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuarioEmpresa = request.CrearAsignacion();

        var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioEmpresa.UsuarioId, cancellationToken);
        if (usuario is null)
        {
            throw new InvalidOperationException("El usuario indicado no existe.");
        }

        var empresa = await _empresaRepository.ObtenerPorIdAsync(usuarioEmpresa.EmpresaId, cancellationToken);
        if (empresa is null)
        {
            throw new InvalidOperationException("La empresa indicada no existe.");
        }

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
