using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class CrearProductoVarianteUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoVarianteRepository _productoVarianteRepository;

    public CrearProductoVarianteUseCase(
        IProductoVarianteRepository productoVarianteRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _productoVarianteRepository = productoVarianteRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ProductoVariante> EjecutarAsync(
        CrearProductoVarianteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var variante = request.CrearProductoVariante(_empresaActiva.EmpresaId);
        await _productoVarianteRepository.AgregarAsync(variante, cancellationToken);

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
