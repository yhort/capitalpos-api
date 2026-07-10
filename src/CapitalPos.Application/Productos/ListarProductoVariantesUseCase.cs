using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class ListarProductoVariantesUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoVarianteRepository _productoVarianteRepository;

    public ListarProductoVariantesUseCase(
        IProductoVarianteRepository productoVarianteRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _productoVarianteRepository = productoVarianteRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<IReadOnlyCollection<ProductoVariante>> EjecutarAsync(
        Guid productoId,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        return _productoVarianteRepository.ListarPorProductoAsync(
            _empresaActiva.EmpresaId,
            productoId,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar variantes de producto.");
        }
    }
}
