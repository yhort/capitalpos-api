using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public sealed class ObtenerEmpresaPorIdUseCase
{
    private readonly IEmpresaRepository _empresaRepository;

    public ObtenerEmpresaPorIdUseCase(IEmpresaRepository empresaRepository)
    {
        _empresaRepository = empresaRepository;
    }

    public Task<Empresa?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _empresaRepository.ObtenerPorIdAsync(id, cancellationToken);
    }
}
