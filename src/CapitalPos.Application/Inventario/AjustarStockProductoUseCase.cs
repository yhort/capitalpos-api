using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Persistence;
using CapitalPos.Application.Sedes;
using CapitalPos.Domain;

namespace CapitalPos.Application.Inventario;

public sealed class AjustarStockProductoUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly ISedeRepository _sedeRepository;
    private readonly IStockProductoRepository _stockRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AjustarStockProductoUseCase(
        IStockProductoRepository stockRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        ISedeRepository sedeRepository,
        IUnitOfWork unitOfWork,
        IEmpresaActivaContext empresaActiva)
    {
        _stockRepository = stockRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _sedeRepository = sedeRepository;
        _unitOfWork = unitOfWork;
        _empresaActiva = empresaActiva;
    }

    public async Task<StockProducto> EjecutarAsync(
        AjustarStockProductoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();
        await ValidarSedeAsync(request.SedeId, cancellationToken);
        await ValidarProductoAsync(request, cancellationToken);

        var stock = await _stockRepository.ObtenerPorProductoAsync(
            _empresaActiva.EmpresaId,
            request.SedeId,
            request.ProductoId,
            request.ProductoVarianteId,
            cancellationToken);

        if (stock is null)
        {
            stock = new StockProducto(
                Guid.NewGuid(),
                _empresaActiva.EmpresaId,
                request.SedeId,
                request.ProductoId,
                request.ProductoVarianteId,
                request.CantidadDisponible);
        }
        else
        {
            stock.AjustarCantidadDisponible(request.CantidadDisponible);
        }

        await _stockRepository.GuardarAsync(stock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stock;
    }

    private async Task ValidarSedeAsync(Guid sedeId, CancellationToken cancellationToken)
    {
        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        var sede = await _sedeRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            sedeId,
            cancellationToken);
        if (sede is null)
        {
            throw new InvalidOperationException("La sede no pertenece a la empresa activa.");
        }

        if (!sede.Activa)
        {
            throw new InvalidOperationException("La sede no esta activa.");
        }
    }

    private async Task ValidarProductoAsync(
        AjustarStockProductoRequest request,
        CancellationToken cancellationToken)
    {
        var producto = await _productoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            request.ProductoId,
            cancellationToken);
        if (producto is null)
        {
            throw new InvalidOperationException("El producto no pertenece a la empresa activa.");
        }

        if (request.ProductoVarianteId is null)
        {
            return;
        }

        var variante = await _productoVarianteRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            request.ProductoVarianteId.Value,
            cancellationToken);
        if (variante is null || variante.ProductoId != request.ProductoId)
        {
            throw new InvalidOperationException("La variante no pertenece al producto y empresa activos.");
        }
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar stock de productos.");
        }
    }
}
