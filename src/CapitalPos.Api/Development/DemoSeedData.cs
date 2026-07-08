using CapitalPos.Domain;

namespace CapitalPos.Api.Development;

public static class DemoSeedData
{
    public static readonly Guid EmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid UsuarioId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid UsuarioEmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    public const string EmpresaRuc = "20600000001";
    public const string EmpresaRazonSocial = "CapitalPOS Demo S.A.C.";
    public const string EmpresaNombreComercial = "CapitalPOS Demo";
    public const string AdminNombre = "Administrador";
    public const string AdminApellido = "Demo";
    public const string AdminCorreo = "admin@capitalpos.test";
    public const string CredencialAlgoritmo = "ASP.NET Core Identity PasswordHasher";
    public const RolEmpresa AdminRol = RolEmpresa.Administrador;
}
