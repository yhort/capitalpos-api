using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Catalogo;

public sealed class ListarMarcasUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IMarcaRepository _marcaRepository;

    public ListarMarcasUseCase(
        IMarcaRepository marcaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _marcaRepository = marcaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<IReadOnlyCollection<Marca>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        var marcas = await _marcaRepository.ListarPorEmpresaAsync(_empresaActiva.EmpresaId, cancellationToken);

        return marcas
            .Where(marca => marca.Activa)
            .OrderBy(marca => marca.Nombre, StringComparer.Ordinal)
            .ToArray();
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar marcas.");
        }
    }
}
