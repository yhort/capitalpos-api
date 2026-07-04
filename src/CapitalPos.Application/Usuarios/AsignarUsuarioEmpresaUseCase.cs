using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class AsignarUsuarioEmpresaUseCase
{
    public UsuarioEmpresa Ejecutar(AsignarUsuarioEmpresaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.CrearAsignacion();
    }
}
