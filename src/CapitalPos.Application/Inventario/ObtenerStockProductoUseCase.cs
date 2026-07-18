using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Sedes;
using CapitalPos.Domain;

namespace CapitalPos.Application.Inventario;

public sealed class ObtenerStockProductoUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly ISedeRepository _sedeRepository;
    private readonly IStockProductoRepository _stockRepository;

    public ObtenerStockProductoUseCase(
        IStockProductoRepository stockRepository,
        ISedeRepository sedeRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _stockRepository = stockRepository;
        _sedeRepository = sedeRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<StockProducto?> EjecutarAsync(
        Guid sedeId,
        Guid productoId,
        Guid? productoVarianteId = null,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        await ValidarSedeAsync(sedeId, cancellationToken);

        return await _stockRepository.ObtenerPorProductoAsync(
            _empresaActiva.EmpresaId,
            sedeId,
            productoId,
            productoVarianteId,
            cancellationToken);
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

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar stock de productos.");
        }
    }
}
