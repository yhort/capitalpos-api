using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public sealed class CrearEmpresaUseCase
{
    private readonly IEmpresaRepository _empresaRepository;

    public CrearEmpresaUseCase(IEmpresaRepository empresaRepository)
    {
        _empresaRepository = empresaRepository;
    }

    public async Task<Empresa> EjecutarAsync(
        CrearEmpresaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var empresa = request.CrearEmpresa();

        if (await _empresaRepository.ExisteRucAsync(empresa.Ruc, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una empresa registrada con el mismo RUC.");
        }

        await _empresaRepository.AgregarAsync(empresa, cancellationToken);

        return empresa;
    }
}
