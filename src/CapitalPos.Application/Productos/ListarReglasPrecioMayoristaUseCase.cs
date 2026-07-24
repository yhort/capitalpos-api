using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class ListarReglasPrecioMayoristaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IReglaPrecioMayoristaRepository _reglaRepository;

    public ListarReglasPrecioMayoristaUseCase(
        IReglaPrecioMayoristaRepository reglaRepository,
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _reglaRepository = reglaRepository;
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<IReadOnlyCollection<ReglaPrecioMayorista>?> EjecutarAsync(
        Guid productoId,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        if (await ObtenerProductoAsync(productoId, cancellationToken) is null)
        {
            return null;
        }

        return await _reglaRepository.ListarPorProductoAsync(
            _empresaActiva.EmpresaId,
            productoId,
            cancellationToken);
    }

    private Task<Producto?> ObtenerProductoAsync(Guid productoId, CancellationToken cancellationToken)
    {
        return _productoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            productoId,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar reglas de precio mayorista.");
        }
    }
}
