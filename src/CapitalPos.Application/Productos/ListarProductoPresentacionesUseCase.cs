using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class ListarProductoPresentacionesUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoPresentacionRepository _presentacionRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;

    public ListarProductoPresentacionesUseCase(
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

    public async Task<IReadOnlyCollection<ProductoPresentacionDetalle>?> EjecutarAsync(
        Guid productoId,
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

        var presentaciones = await _presentacionRepository.ListarPorProductoAsync(
            _empresaActiva.EmpresaId,
            productoId,
            cancellationToken);
        var unidades = await _unidadMedidaRepository.ListarAsync(cancellationToken);
        var unidadesPorId = unidades.ToDictionary(unidad => unidad.Id);

        return presentaciones
            .Where(presentacion => presentacion.Activa)
            .Select(presentacion => new ProductoPresentacionDetalle(
                presentacion,
                unidadesPorId[presentacion.UnidadMedidaId]))
            .ToArray();
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar presentaciones.");
        }
    }
}
