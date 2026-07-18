using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Sedes;

public sealed class ListarSedesUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly ISedeRepository _sedeRepository;

    public ListarSedesUseCase(
        ISedeRepository sedeRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _sedeRepository = sedeRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<IReadOnlyCollection<Sede>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        var sedes = await _sedeRepository.ListarPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);

        return sedes
            .Where(sede => sede.Activa)
            .OrderBy(sede => sede.Nombre, StringComparer.Ordinal)
            .ToArray();
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar sedes.");
        }
    }
}
