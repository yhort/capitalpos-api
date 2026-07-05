using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public sealed class ListarEmpresasUseCase
{
    private readonly IEmpresaRepository _empresaRepository;

    public ListarEmpresasUseCase(IEmpresaRepository empresaRepository)
    {
        _empresaRepository = empresaRepository;
    }

    public Task<IReadOnlyCollection<Empresa>> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        return _empresaRepository.ListarAsync(cancellationToken);
    }
}
