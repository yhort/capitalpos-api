using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed class CrearUsuarioUseCase
{
    public Usuario Ejecutar(CrearUsuarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.CrearUsuario();
    }
}
