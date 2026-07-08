using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class EmpresaPermisosTests
{
    [Fact]
    public void Administrador_tiene_todos_los_permisos()
    {
        foreach (var permiso in Enum.GetValues<PermisoEmpresa>())
        {
            Assert.True(EmpresaPermisos.TienePermiso(RolEmpresa.Administrador, permiso));
        }
    }

    [Theory]
    [InlineData(RolEmpresa.Vendedor, PermisoEmpresa.OperarVentas)]
    [InlineData(RolEmpresa.Vendedor, PermisoEmpresa.EmitirCpe)]
    [InlineData(RolEmpresa.Cajero, PermisoEmpresa.OperarCaja)]
    [InlineData(RolEmpresa.Almacenero, PermisoEmpresa.OperarAlmacen)]
    [InlineData(RolEmpresa.Contador, PermisoEmpresa.ConsultarContabilidad)]
    public void Rol_tiene_permiso_permitido(RolEmpresa rol, PermisoEmpresa permiso)
    {
        Assert.True(EmpresaPermisos.TienePermiso(rol, permiso));
    }

    [Theory]
    [InlineData(RolEmpresa.Vendedor, PermisoEmpresa.GestionarUsuarios)]
    [InlineData(RolEmpresa.Cajero, PermisoEmpresa.GestionarRoles)]
    [InlineData(RolEmpresa.Almacenero, PermisoEmpresa.EmitirCpe)]
    [InlineData(RolEmpresa.Contador, PermisoEmpresa.OperarCaja)]
    public void Rol_no_tiene_permiso_no_asignado(RolEmpresa rol, PermisoEmpresa permiso)
    {
        Assert.False(EmpresaPermisos.TienePermiso(rol, permiso));
    }
}
