using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class ListarProductosUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;

    public ListarProductosUseCase(
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<IReadOnlyCollection<Producto>> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        return _productoRepository.ListarPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar productos.");
        }
    }
}
