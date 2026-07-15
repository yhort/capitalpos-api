using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class DesactivarProductoVarianteUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;

    public DesactivarProductoVarianteUseCase(
        IProductoVarianteRepository productoVarianteRepository,
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _productoVarianteRepository = productoVarianteRepository;
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ProductoVariante?> EjecutarAsync(
        Guid productoId,
        Guid varianteId,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        var producto = await _productoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            productoId,
            cancellationToken);
        if (producto is null)
        {
            return null;
        }

        var variante = await _productoVarianteRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            varianteId,
            cancellationToken);
        if (variante is null || variante.ProductoId != productoId)
        {
            return null;
        }

        variante.Desactivar();
        await _productoVarianteRepository.ActualizarAsync(variante, cancellationToken);

        return variante;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar variantes de producto.");
        }
    }
}
