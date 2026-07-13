using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.ConfiguracionFiscal;

public sealed class ObtenerConfiguracionFiscalEmpresaUseCase
{
    private readonly IConfiguracionFiscalEmpresaRepository _repository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public ObtenerConfiguracionFiscalEmpresaUseCase(
        IConfiguracionFiscalEmpresaRepository repository,
        IEmpresaActivaContext empresaActiva)
    {
        _repository = repository;
        _empresaActiva = empresaActiva;
    }

    public Task<ConfiguracionFiscalEmpresa?> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        return _repository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para obtener datos fiscales.");
        }
    }
}
