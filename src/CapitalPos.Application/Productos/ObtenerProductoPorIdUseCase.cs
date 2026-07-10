using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class ObtenerProductoPorIdUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;

    public ObtenerProductoPorIdUseCase(
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<Producto?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        return _productoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            id,
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
