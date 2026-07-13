using CapitalPos.Domain;

namespace CapitalPos.Application.ConfiguracionFiscal;

public interface IConfiguracionFiscalEmpresaRepository
{
    Task<ConfiguracionFiscalEmpresa?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task GuardarAsync(
        ConfiguracionFiscalEmpresa configuracion,
        CancellationToken cancellationToken = default);
}
