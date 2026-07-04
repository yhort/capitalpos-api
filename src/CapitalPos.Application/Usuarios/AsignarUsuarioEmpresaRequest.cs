using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public sealed record AsignarUsuarioEmpresaRequest(
    Guid UsuarioId,
    Guid EmpresaId,
    RolEmpresa Rol,
    bool Activo = true)
{
    public UsuarioEmpresa CrearAsignacion()
    {
        return new UsuarioEmpresa(
            Guid.NewGuid(),
            UsuarioId,
            EmpresaId,
            Rol,
            Activo);
    }
}
