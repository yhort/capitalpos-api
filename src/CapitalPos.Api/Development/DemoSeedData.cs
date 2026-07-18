using CapitalPos.Domain;

namespace CapitalPos.Api.Development;

public static class DemoSeedData
{
    public static readonly Guid EmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid UsuarioId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid UsuarioEmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid SedeId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid PuntoVentaId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    public const string EmpresaRuc = "20600000001";
    public const string EmpresaRazonSocial = "CapitalPOS Demo S.A.C.";
    public const string EmpresaNombreComercial = "CapitalPOS Demo";
    public const string AdminNombre = "Administrador";
    public const string AdminApellido = "Demo";
    public const string AdminCorreo = "admin@capitalpos.test";
    public const string CredencialAlgoritmo = "ASP.NET Core Identity PasswordHasher";
    public const RolEmpresa AdminRol = RolEmpresa.Administrador;
    public const string SedeNombre = "Tienda Demo";
    public const string SedeCodigoEstablecimiento = "0000";
    public const string SedeDireccion = "Av. Demo 123";
    public const string SedeDistrito = "Lima";
    public const string SedeProvincia = "Lima";
    public const string SedeDepartamento = "Lima";
    public const TipoSede SedeTipo = TipoSede.TIENDA;
    public const string PuntoVentaNombre = "Caja Principal";
}
