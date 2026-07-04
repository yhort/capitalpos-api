using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public sealed class CrearEmpresaUseCase
{
    public Empresa Ejecutar(CrearEmpresaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.CrearEmpresa();
    }
}
