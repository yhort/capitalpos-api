using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class CrearReglaPrecioMayoristaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IReglaPrecioMayoristaRepository _reglaRepository;

    public CrearReglaPrecioMayoristaUseCase(
        IReglaPrecioMayoristaRepository reglaRepository,
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _reglaRepository = reglaRepository;
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ReglaPrecioMayorista> EjecutarAsync(
        CrearReglaPrecioMayoristaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();
        await ValidarProductoAsync(request.ProductoId, cancellationToken);
        await ValidarUnicidadActivaAsync(request.ProductoId, request.CantidadMinima, null, cancellationToken);

        var regla = request.CrearRegla(_empresaActiva.EmpresaId);
        await _reglaRepository.AgregarAsync(regla, cancellationToken);

        return regla;
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

    private async Task ValidarUnicidadActivaAsync(
        Guid productoId,
        int cantidadMinima,
        Guid? excluirId,
        CancellationToken cancellationToken)
    {
        if (await _reglaRepository.ExisteActivaPorCantidadMinimaAsync(
            _empresaActiva.EmpresaId,
            productoId,
            cantidadMinima,
            excluirId,
            cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una regla activa con la misma cantidad minima para el producto.");
        }
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar reglas de precio mayorista.");
        }
    }
}
