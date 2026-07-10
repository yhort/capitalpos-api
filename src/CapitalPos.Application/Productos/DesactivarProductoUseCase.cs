using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class DesactivarProductoUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;

    public DesactivarProductoUseCase(
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<Producto?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        var producto = await _productoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            id,
            cancellationToken);
        if (producto is null)
        {
            return null;
        }

        producto.Desactivar();
        await _productoRepository.ActualizarAsync(producto, cancellationToken);

        return producto;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar productos.");
        }
    }
}
