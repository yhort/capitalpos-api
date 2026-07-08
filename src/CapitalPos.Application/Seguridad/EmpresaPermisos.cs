using CapitalPos.Domain;

namespace CapitalPos.Application.Seguridad;

public static class EmpresaPermisos
{
    private static readonly IReadOnlyDictionary<RolEmpresa, IReadOnlySet<PermisoEmpresa>> PermisosPorRol =
        new Dictionary<RolEmpresa, IReadOnlySet<PermisoEmpresa>>
        {
            [RolEmpresa.Administrador] = Enum.GetValues<PermisoEmpresa>().ToHashSet(),
            [RolEmpresa.Vendedor] = new HashSet<PermisoEmpresa>
            {
                PermisoEmpresa.ConsultarEmpresa,
                PermisoEmpresa.OperarVentas,
                PermisoEmpresa.EmitirCpe
            },
            [RolEmpresa.Cajero] = new HashSet<PermisoEmpresa>
            {
                PermisoEmpresa.ConsultarEmpresa,
                PermisoEmpresa.OperarVentas,
                PermisoEmpresa.OperarCaja,
                PermisoEmpresa.EmitirCpe
            },
            [RolEmpresa.Almacenero] = new HashSet<PermisoEmpresa>
            {
                PermisoEmpresa.ConsultarEmpresa,
                PermisoEmpresa.OperarAlmacen
            },
            [RolEmpresa.Contador] = new HashSet<PermisoEmpresa>
            {
                PermisoEmpresa.ConsultarEmpresa,
                PermisoEmpresa.ConsultarContabilidad,
                PermisoEmpresa.EmitirCpe
            }
        };

    public static bool TienePermiso(RolEmpresa rol, PermisoEmpresa permiso)
    {
        return PermisosPorRol.TryGetValue(rol, out var permisos) &&
            permisos.Contains(permiso);
    }

    public static IReadOnlyCollection<PermisoEmpresa> ObtenerPermisos(RolEmpresa rol)
    {
        return PermisosPorRol.TryGetValue(rol, out var permisos)
            ? permisos.ToArray()
            : [];
    }
}
