using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Inventario;

public sealed class ObtenerStockProductoUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IStockProductoRepository _stockRepository;

    public ObtenerStockProductoUseCase(
        IStockProductoRepository stockRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _stockRepository = stockRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<StockProducto?> EjecutarAsync(
        Guid productoId,
        Guid? productoVarianteId = null,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        return _stockRepository.ObtenerPorProductoAsync(
            _empresaActiva.EmpresaId,
            productoId,
            productoVarianteId,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar stock de productos.");
        }
    }
}
