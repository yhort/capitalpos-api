using CapitalPos.Domain;

namespace CapitalPos.Api.Development;

public static class DemoSeedData
{
    public static readonly Guid EmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid UsuarioId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid UsuarioEmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid CategoriaId = Guid.Parse("10000000-0000-0000-0000-000000000009");
    public static readonly Guid MarcaId = Guid.Parse("10000000-0000-0000-0000-000000000010");
    public static readonly Guid SedeId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid PuntoVentaId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    public static readonly Guid ProductoId = Guid.Parse("10000000-0000-0000-0000-000000000006");
    public static readonly Guid StockProductoId = Guid.Parse("10000000-0000-0000-0000-000000000007");
    public static readonly Guid SerieComprobanteId = Guid.Parse("10000000-0000-0000-0000-000000000008");
    public static readonly Guid UnidadMedidaUndId = Guid.Parse("10000000-0000-0000-0000-000000000011");
    public static readonly Guid UnidadMedidaCajId = Guid.Parse("10000000-0000-0000-0000-000000000012");
    public static readonly Guid UnidadMedidaPaqId = Guid.Parse("10000000-0000-0000-0000-000000000013");
    public static readonly Guid UnidadMedidaDocId = Guid.Parse("10000000-0000-0000-0000-000000000014");
    public static readonly Guid UnidadMedidaKgId = Guid.Parse("10000000-0000-0000-0000-000000000015");

    public const string EmpresaRuc = "20600000001";
    public const string EmpresaRazonSocial = "CapitalPOS Demo S.A.C.";
    public const string EmpresaNombreComercial = "CapitalPOS Demo";
    public const string AdminNombre = "Administrador";
    public const string AdminApellido = "Demo";
    public const string AdminCorreo = "admin@capitalpos.test";
    public const string CredencialAlgoritmo = "ASP.NET Core Identity PasswordHasher";
    public const RolEmpresa AdminRol = RolEmpresa.Administrador;
    public const string CategoriaNombre = "General";
    public const string MarcaNombre = "Demo";
    public const string SedeNombre = "Tienda Demo";
    public const string SedeCodigoEstablecimiento = "0000";
    public const string SedeDireccion = "Av. Demo 123";
    public const string SedeDistrito = "Lima";
    public const string SedeProvincia = "Lima";
    public const string SedeDepartamento = "Lima";
    public const TipoSede SedeTipo = TipoSede.TIENDA;
    public const string PuntoVentaNombre = "Caja Principal";
    public const string ProductoNombre = "Producto Demo";
    public const string ProductoCodigoSku = "DEMO-001";
    public const decimal ProductoPrecioVenta = 10m;
    public const ModoManejoProducto ProductoModoManejo = ModoManejoProducto.SIMPLE;
    public const decimal StockProductoCantidadDisponible = 20m;
    public const string SerieComprobanteTipo = "03";
    public const string SerieComprobanteSerie = "B001";
    public const int SerieComprobanteCorrelativoActual = 0;

    public static IReadOnlyCollection<(Guid Id, string Codigo, string Nombre)> UnidadesMedidaBasicas { get; } =
    [
        (UnidadMedidaUndId, "UND", "Unidad"),
        (UnidadMedidaCajId, "CAJ", "Caja"),
        (UnidadMedidaPaqId, "PAQ", "Paquete"),
        (UnidadMedidaDocId, "DOC", "Docena"),
        (UnidadMedidaKgId, "KG", "Kilogramo")
    ];
}
