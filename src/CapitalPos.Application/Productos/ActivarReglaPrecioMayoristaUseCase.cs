using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class ActivarReglaPrecioMayoristaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IReglaPrecioMayoristaRepository _reglaRepository;

    public ActivarReglaPrecioMayoristaUseCase(
        IReglaPrecioMayoristaRepository reglaRepository,
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _reglaRepository = reglaRepository;
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ReglaPrecioMayorista?> EjecutarAsync(
        Guid productoId,
        Guid reglaId,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        if (!await ExisteProductoAsync(productoId, cancellationToken))
        {
            return null;
        }

        var regla = await _reglaRepository.ObtenerPorEmpresaYProductoAsync(
            _empresaActiva.EmpresaId,
            productoId,
            reglaId,
            cancellationToken);
        if (regla is null)
        {
            return null;
        }

        if (await _reglaRepository.ExisteActivaPorCantidadMinimaAsync(
            _empresaActiva.EmpresaId,
            productoId,
            regla.CantidadMinima,
            regla.Id,
            cancellationToken))
        {
            throw new InvalidOperationException("Activar la regla generaria una cantidad minima duplicada para el producto.");
        }

        regla.Activar();
        await _reglaRepository.ActualizarAsync(regla, cancellationToken);

        return regla;
    }

    private async Task<bool> ExisteProductoAsync(Guid productoId, CancellationToken cancellationToken)
    {
        return await _productoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            productoId,
            cancellationToken) is not null;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar reglas de precio mayorista.");
        }
    }
}
