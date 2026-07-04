using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed record CrearUsuarioRequest(
    string Nombre,
    string Apellido,
    string Correo,
    bool Activo = true)
{
    public Usuario CrearUsuario()
    {
        return new Usuario(
            Guid.NewGuid(),
            Nombre,
            Apellido,
            Correo,
            Activo);
    }
}
