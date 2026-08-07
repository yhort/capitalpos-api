using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Compras;

public sealed class ListarComprasUseCase
{
    private readonly ICompraRepository _compraRepository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public ListarComprasUseCase(
        ICompraRepository compraRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _compraRepository = compraRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<IReadOnlyCollection<Compra>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        return _compraRepository.ListarPorEmpresaAsync(_empresaActiva.EmpresaId, cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para listar compras.");
        }
    }
}
