using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.ConfiguracionFiscal;

public sealed class GuardarConfiguracionFiscalEmpresaUseCase
{
    private readonly IConfiguracionFiscalEmpresaRepository _repository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public GuardarConfiguracionFiscalEmpresaUseCase(
        IConfiguracionFiscalEmpresaRepository repository,
        IEmpresaActivaContext empresaActiva)
    {
        _repository = repository;
        _empresaActiva = empresaActiva;
    }

    public async Task<ConfiguracionFiscalEmpresa> EjecutarAsync(
        GuardarConfiguracionFiscalEmpresaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var configuracion = await _repository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);

        if (configuracion is null)
        {
            configuracion = new ConfiguracionFiscalEmpresa(
                _empresaActiva.EmpresaId,
                request.Ruc,
                request.RazonSocial,
                request.NombreComercial,
                request.Ubigeo,
                request.Direccion,
                request.Departamento,
                request.Provincia,
                request.Distrito,
                request.Activa);
        }
        else
        {
            configuracion.ActualizarDatosFiscales(
                request.Ruc,
                request.RazonSocial,
                request.NombreComercial,
                request.Ubigeo,
                request.Direccion,
                request.Departamento,
                request.Provincia,
                request.Distrito);

            if (request.Activa)
            {
                configuracion.Activar();
            }
            else
            {
                configuracion.Desactivar();
            }
        }

        await _repository.GuardarAsync(configuracion, cancellationToken);

        return configuracion;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para configurar datos fiscales.");
        }
    }
}
