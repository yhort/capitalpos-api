using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Catalogo;

public sealed class ListarCategoriasUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public ListarCategoriasUseCase(
        ICategoriaRepository categoriaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _categoriaRepository = categoriaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<IReadOnlyCollection<Categoria>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        var categorias = await _categoriaRepository.ListarPorEmpresaAsync(_empresaActiva.EmpresaId, cancellationToken);

        return categorias
            .Where(categoria => categoria.Activa)
            .OrderBy(categoria => categoria.Nombre, StringComparer.Ordinal)
            .ToArray();
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar categorias.");
        }
    }
}
