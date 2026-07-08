using CapitalPos.Domain;

namespace CapitalPos.Application.Seguridad;

public sealed class EmpresaPermisoAuthorizer : IEmpresaPermisoAuthorizer
{
    public bool TienePermiso(RolEmpresa rol, PermisoEmpresa permiso)
    {
        return EmpresaPermisos.TienePermiso(rol, permiso);
    }
}
