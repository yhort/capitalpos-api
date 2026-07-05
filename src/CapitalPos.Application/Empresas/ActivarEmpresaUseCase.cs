using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public sealed class ActivarEmpresaUseCase
{
    private readonly IEmpresaRepository _empresaRepository;

    public ActivarEmpresaUseCase(IEmpresaRepository empresaRepository)
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

        empresa.Activar();
        await _empresaRepository.ActualizarAsync(empresa, cancellationToken);

        return empresa;
    }
}
