using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Application.Caja;

public sealed class ObtenerResumenSesionCajaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly ISesionCajaRepository _sesionCajaRepository;
    private readonly IVentaRepository _ventaRepository;

    public ObtenerResumenSesionCajaUseCase(
        ISesionCajaRepository sesionCajaRepository,
        IVentaRepository ventaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _sesionCajaRepository = sesionCajaRepository;
        _ventaRepository = ventaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ResumenSesionCajaResponse?> EjecutarAsync(
        Guid sesionCajaId,
        CancellationToken cancellationToken = default)
    {
        if (sesionCajaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la sesion de caja es obligatorio.",
                nameof(sesionCajaId));
        }

        ValidarEmpresaActiva();
        var empresaId = _empresaActiva.EmpresaId;
        var sesion = await _sesionCajaRepository.ObtenerPorEmpresaAsync(
            empresaId,
            sesionCajaId,
            cancellationToken);
        if (sesion is null)
        {
            return null;
        }

        var ventasEmpresa = await _ventaRepository.ListarPorEmpresaAsync(
            empresaId,
            cancellationToken);
        var ventasSesion = ventasEmpresa
            .Where(venta =>
                venta.Estado == EstadoVenta.Registrada
                && venta.PuntoVentaId == sesion.PuntoVentaId
                && venta.FechaCreacion >= sesion.FechaApertura
                && (!sesion.FechaCierre.HasValue || venta.FechaCreacion <= sesion.FechaCierre.Value))
            .ToArray();
        var pagos = ventasSesion
            .SelectMany(venta => venta.Pagos)
            .ToArray();
        var totalPagado = pagos.Sum(pago => pago.Monto);
        var pagosPorMetodo = Enum.GetValues<MetodoPago>()
            .Select(metodoPago =>
            {
                var pagosMetodo = pagos
                    .Where(pago => pago.MetodoPago == metodoPago)
                    .ToArray();
                return new ResumenPagoMetodoResponse(
                    metodoPago.ToString(),
                    pagosMetodo.Sum(pago => pago.Monto),
                    pagosMetodo.Length);
            })
            .ToArray();
        decimal? diferenciaOperativa = sesion.MontoDeclaradoCierre.HasValue
            ? sesion.MontoDeclaradoCierre.Value - sesion.MontoInicial - totalPagado
            : null;

        return new ResumenSesionCajaResponse(
            sesion.Id,
            sesion.EmpresaId,
            sesion.SedeId,
            sesion.PuntoVentaId,
            sesion.Estado.ToString(),
            sesion.FechaApertura,
            sesion.FechaCierre,
            sesion.MontoInicial,
            sesion.MontoDeclaradoCierre,
            sesion.DiferenciaCierre,
            ventasSesion.Sum(venta => venta.Total),
            ventasSesion.Length,
            totalPagado,
            diferenciaOperativa,
            pagosPorMetodo);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "La empresa activa es obligatoria para consultar el resumen de caja.");
        }
    }
}

public sealed record ResumenSesionCajaResponse(
    Guid SesionCajaId,
    Guid EmpresaId,
    Guid SedeId,
    Guid PuntoVentaId,
    string Estado,
    DateTimeOffset FechaApertura,
    DateTimeOffset? FechaCierre,
    decimal MontoInicial,
    decimal? MontoDeclaradoCierre,
    decimal? DiferenciaCierre,
    decimal TotalVentas,
    int CantidadVentas,
    decimal TotalPagado,
    decimal? DiferenciaOperativa,
    IReadOnlyCollection<ResumenPagoMetodoResponse> PagosPorMetodo);

public sealed record ResumenPagoMetodoResponse(
    string MetodoPago,
    decimal Total,
    int CantidadPagos);
