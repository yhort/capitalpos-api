using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public class RegistrarComprobanteCpeUseCase
{
    private readonly IComprobanteRepository _comprobanteRepository;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IVentaRepository _ventaRepository;

    public RegistrarComprobanteCpeUseCase(
        IComprobanteRepository comprobanteRepository,
        IVentaRepository ventaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _comprobanteRepository = comprobanteRepository;
        _ventaRepository = ventaRepository;
        _empresaActiva = empresaActiva;
    }

    public virtual async Task<Comprobante?> EjecutarAsync(
        RegistrarComprobanteCpeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var venta = await _ventaRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            request.VentaId,
            cancellationToken);
        if (venta is null)
        {
            return null;
        }

        var comprobante = new Comprobante(
            Guid.NewGuid(),
            _empresaActiva.EmpresaId,
            request.VentaId,
            request.TipoComprobante,
            request.Serie,
            request.Correlativo,
            request.EstadoCpe,
            request.Mensaje,
            request.Hash,
            request.NombreXml,
            request.NombreZip,
            request.NombreCdr,
            comprobanteAfectadoId: request.ComprobanteAfectadoId,
            tipoComprobanteAfectado: request.TipoComprobanteAfectado,
            serieAfectada: request.SerieAfectada,
            correlativoAfectado: request.CorrelativoAfectado,
            codigoMotivo: request.CodigoMotivo,
            descripcionMotivo: request.DescripcionMotivo);
        await _comprobanteRepository.AgregarAsync(comprobante, cancellationToken);

        return comprobante;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para registrar comprobantes.");
        }
    }
}
