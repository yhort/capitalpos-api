using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Caja;

public sealed class CerrarSesionCajaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly ISesionCajaRepository _sesionCajaRepository;

    public CerrarSesionCajaUseCase(
        ISesionCajaRepository sesionCajaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _sesionCajaRepository = sesionCajaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<SesionCaja> EjecutarAsync(
        CerrarSesionCajaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();
        if (request.SesionCajaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sesion de caja es obligatorio.", nameof(request));
        }

        var sesionCaja = await _sesionCajaRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            request.SesionCajaId,
            cancellationToken);
        if (sesionCaja is null)
        {
            throw new InvalidOperationException("La sesion de caja no pertenece a la empresa activa.");
        }

        sesionCaja.Cerrar(
            request.MontoDeclaradoCierre,
            DateTimeOffset.UtcNow,
            request.UsuarioCierreId ?? _empresaActiva.UsuarioId,
            request.ObservacionCierre);

        await _sesionCajaRepository.GuardarAsync(sesionCaja, cancellationToken);

        return sesionCaja;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar caja.");
        }
    }
}
