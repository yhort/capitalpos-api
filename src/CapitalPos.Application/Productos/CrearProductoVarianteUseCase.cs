using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class CrearProductoVarianteUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;

    public CrearProductoVarianteUseCase(
        IProductoVarianteRepository productoVarianteRepository,
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _productoVarianteRepository = productoVarianteRepository;
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ProductoVariante> EjecutarAsync(
        CrearProductoVarianteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();
        await ValidarProductoAsync(request.ProductoId, cancellationToken);
        await ValidarUnicidadAsync(request, cancellationToken);

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

    private async Task ValidarProductoAsync(Guid productoId, CancellationToken cancellationToken)
    {
        var producto = await _productoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            productoId,
            cancellationToken);
        if (producto is null)
        {
            throw new InvalidOperationException("El producto no pertenece a la empresa activa.");
        }
    }

    private async Task ValidarUnicidadAsync(
        CrearProductoVarianteRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.CodigoSku) &&
            await _productoVarianteRepository.ExisteSkuAsync(
                _empresaActiva.EmpresaId,
                request.CodigoSku,
                cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una variante con el mismo SKU en la empresa activa.");
        }

        if (!string.IsNullOrWhiteSpace(request.CodigoBarras) &&
            await _productoVarianteRepository.ExisteCodigoBarrasAsync(
                _empresaActiva.EmpresaId,
                request.CodigoBarras,
                cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una variante con el mismo codigo de barras en la empresa activa.");
        }
    }
}
