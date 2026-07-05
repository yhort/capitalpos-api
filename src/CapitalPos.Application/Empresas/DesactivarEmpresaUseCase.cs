using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public sealed class DesactivarEmpresaUseCase
{
    private readonly IEmpresaRepository _empresaRepository;

    public DesactivarEmpresaUseCase(IEmpresaRepository empresaRepository)
    {
        _empresaRepository = empresaRepository;
    }

    public async Task<Empresa?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var empresa = await _empresaRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (empresa is null)
        {
            return null;
        }

        empresa.Desactivar();
        await _empresaRepository.ActualizarAsync(empresa, cancellationToken);

        return empresa;
    }
}
