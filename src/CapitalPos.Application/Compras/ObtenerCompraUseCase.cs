using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Compras;

public sealed class ObtenerCompraUseCase
{
    private readonly ICompraRepository _compraRepository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public ObtenerCompraUseCase(
        ICompraRepository compraRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _compraRepository = compraRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<Compra?> EjecutarAsync(Guid compraId, CancellationToken cancellationToken = default)
    {
        if (compraId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la compra es obligatorio.", nameof(compraId));
        }

        ValidarEmpresaActiva();
        return _compraRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            compraId,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar compras.");
        }
    }
}
