using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Catalogo;

public sealed class CrearCategoriaUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public CrearCategoriaUseCase(
        ICategoriaRepository categoriaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _categoriaRepository = categoriaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<Categoria> EjecutarAsync(
        CrearCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        if (request.CategoriaPadreId is not null)
        {
            var padre = await _categoriaRepository.ObtenerPorEmpresaAsync(
                _empresaActiva.EmpresaId,
                request.CategoriaPadreId.Value,
                cancellationToken);
            if (padre is null)
            {
                throw new InvalidOperationException("La categoria padre no pertenece a la empresa activa.");
            }

            if (padre.CategoriaPadreId is not null)
            {
                throw new InvalidOperationException("El MVP solo permite un nivel de subcategorias.");
            }
        }

        var categoria = request.CrearCategoria(_empresaActiva.EmpresaId);
        await _categoriaRepository.AgregarAsync(categoria, cancellationToken);

        return categoria;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar categorias.");
        }
    }
}
