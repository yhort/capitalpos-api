using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class CrearProductoUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;

    public CrearProductoUseCase(
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<Producto> EjecutarAsync(
        CrearProductoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var producto = request.CrearProducto(_empresaActiva.EmpresaId);
        await _productoRepository.AgregarAsync(producto, cancellationToken);

        return producto;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar productos.");
        }
    }
}
