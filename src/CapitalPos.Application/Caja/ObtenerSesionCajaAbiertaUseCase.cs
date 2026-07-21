using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Caja;

public sealed class ObtenerSesionCajaAbiertaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly ISesionCajaRepository _sesionCajaRepository;

    public ObtenerSesionCajaAbiertaUseCase(
        ISesionCajaRepository sesionCajaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _sesionCajaRepository = sesionCajaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<SesionCaja?> EjecutarAsync(
        Guid puntoVentaId,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();
        if (puntoVentaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta es obligatorio.", nameof(puntoVentaId));
        }

        return await _sesionCajaRepository.ObtenerAbiertaPorPuntoVentaAsync(
            _empresaActiva.EmpresaId,
            puntoVentaId,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar caja.");
        }
    }
}
