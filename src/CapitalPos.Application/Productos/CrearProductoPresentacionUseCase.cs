using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class CrearProductoPresentacionUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoPresentacionRepository _presentacionRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;

    public CrearProductoPresentacionUseCase(
        IProductoPresentacionRepository presentacionRepository,
        IProductoRepository productoRepository,
        IUnidadMedidaRepository unidadMedidaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _presentacionRepository = presentacionRepository;
        _productoRepository = productoRepository;
        _unidadMedidaRepository = unidadMedidaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ProductoPresentacionDetalle> EjecutarAsync(
        CrearProductoPresentacionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var producto = await _productoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            request.ProductoId,
            cancellationToken);
        if (producto is null)
        {
            throw new InvalidOperationException("El producto no pertenece a la empresa activa.");
        }

        var unidad = await _unidadMedidaRepository.ObtenerPorIdAsync(
            request.UnidadMedidaId,
            cancellationToken);
        if (unidad is null || !unidad.Activa)
        {
            throw new InvalidOperationException("La unidad de medida no existe o no esta activa.");
        }

        if (!string.IsNullOrWhiteSpace(request.CodigoBarras) &&
            await _presentacionRepository.ExisteCodigoBarrasAsync(
                _empresaActiva.EmpresaId,
                request.CodigoBarras,
                cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una presentacion con el mismo codigo de barras en la empresa activa.");
        }

        var presentacion = request.CrearPresentacion(_empresaActiva.EmpresaId);
        await _presentacionRepository.AgregarAsync(presentacion, cancellationToken);

        return new ProductoPresentacionDetalle(presentacion, unidad);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar presentaciones.");
        }
    }
}
