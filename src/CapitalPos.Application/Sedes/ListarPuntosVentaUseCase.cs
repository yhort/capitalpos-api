using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Sedes;

public sealed class ListarPuntosVentaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IPuntoVentaRepository _puntoVentaRepository;
    private readonly ISedeRepository _sedeRepository;

    public ListarPuntosVentaUseCase(
        ISedeRepository sedeRepository,
        IPuntoVentaRepository puntoVentaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _sedeRepository = sedeRepository;
        _puntoVentaRepository = puntoVentaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<IReadOnlyCollection<PuntoVenta>?> EjecutarAsync(
        Guid sedeId,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        var sede = await _sedeRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            sedeId,
            cancellationToken);
        if (sede is null || !sede.Activa)
        {
            return null;
        }

        var puntosVenta = await _puntoVentaRepository.ListarPorSedeAsync(
            _empresaActiva.EmpresaId,
            sedeId,
            cancellationToken);

        return puntosVenta
            .Where(puntoVenta => puntoVenta.Activo)
            .OrderBy(puntoVenta => puntoVenta.Nombre, StringComparer.Ordinal)
            .ToArray();
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar puntos de venta.");
        }
    }
}
