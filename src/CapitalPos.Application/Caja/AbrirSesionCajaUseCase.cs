using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Caja;

public sealed class AbrirSesionCajaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IPuntoVentaRepository _puntoVentaRepository;
    private readonly ISesionCajaRepository _sesionCajaRepository;

    public AbrirSesionCajaUseCase(
        ISesionCajaRepository sesionCajaRepository,
        IPuntoVentaRepository puntoVentaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _sesionCajaRepository = sesionCajaRepository;
        _puntoVentaRepository = puntoVentaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<SesionCaja> EjecutarAsync(
        AbrirSesionCajaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();
        if (request.PuntoVentaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta es obligatorio.", nameof(request));
        }

        var puntoVenta = await _puntoVentaRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            request.PuntoVentaId,
            cancellationToken);
        if (puntoVenta is null)
        {
            throw new InvalidOperationException("El punto de venta no pertenece a la empresa activa.");
        }

        if (!puntoVenta.Activo)
        {
            throw new InvalidOperationException("El punto de venta no esta activo.");
        }

        var sesionAbierta = await _sesionCajaRepository.ObtenerAbiertaPorPuntoVentaAsync(
            _empresaActiva.EmpresaId,
            request.PuntoVentaId,
            cancellationToken);
        if (sesionAbierta is not null)
        {
            throw new InvalidOperationException("Ya existe una sesion de caja abierta para el punto de venta.");
        }

        var usuarioAperturaId = request.UsuarioAperturaId ?? _empresaActiva.UsuarioId;
        var sesionCaja = new SesionCaja(
            Guid.NewGuid(),
            _empresaActiva.EmpresaId,
            puntoVenta.SedeId,
            puntoVenta.Id,
            request.MontoInicial,
            usuarioAperturaId,
            request.ObservacionApertura);

        await _sesionCajaRepository.AgregarAsync(sesionCaja, cancellationToken);

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
