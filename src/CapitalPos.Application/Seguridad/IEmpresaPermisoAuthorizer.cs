using CapitalPos.Domain;

namespace CapitalPos.Application.Seguridad;

public interface IEmpresaPermisoAuthorizer
{
    bool TienePermiso(RolEmpresa rol, PermisoEmpresa permiso);
}
