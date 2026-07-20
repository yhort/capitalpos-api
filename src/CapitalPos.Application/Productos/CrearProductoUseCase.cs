using CapitalPos.Application.Catalogo;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class CrearProductoUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IMarcaRepository _marcaRepository;
    private readonly IProductoRepository _productoRepository;

    public CrearProductoUseCase(
        IProductoRepository productoRepository,
        IEmpresaActivaContext empresaActiva,
        ICategoriaRepository categoriaRepository,
        IMarcaRepository marcaRepository)
    {
        _productoRepository = productoRepository;
        _empresaActiva = empresaActiva;
        _categoriaRepository = categoriaRepository;
        _marcaRepository = marcaRepository;
    }

    public async Task<Producto> EjecutarAsync(
        CrearProductoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();
        await ValidarClasificacionAsync(request, cancellationToken);

        var producto = request.CrearProducto(_empresaActiva.EmpresaId);
        await _productoRepository.AgregarAsync(producto, cancellationToken);

        return producto;
    }

    private async Task ValidarClasificacionAsync(
        CrearProductoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CategoriaId is not null)
        {
            var categoria = await _categoriaRepository.ObtenerPorEmpresaAsync(
                _empresaActiva.EmpresaId,
                request.CategoriaId.Value,
                cancellationToken);
            if (categoria is null)
            {
                throw new InvalidOperationException("La categoria del producto no pertenece a la empresa activa.");
            }
        }

        if (request.MarcaId is not null)
        {
            var marca = await _marcaRepository.ObtenerPorEmpresaAsync(
                _empresaActiva.EmpresaId,
                request.MarcaId.Value,
                cancellationToken);
            if (marca is null)
            {
                throw new InvalidOperationException("La marca del producto no pertenece a la empresa activa.");
            }
        }
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar productos.");
        }
    }
}
