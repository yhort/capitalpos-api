namespace CapitalPos.Tests;

public class EndpointPermissionStructureTests
{
    [Theory]
    [InlineData("src/CapitalPos.Api/Endpoints/EmpresaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UsuarioEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/CatalogoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UnidadMedidaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ProductoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/StockEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/SedeEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/CajaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ClienteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/CompraEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/VentaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/PedidoDigitalEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ReporteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/DashboardEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ConfiguracionFiscalEndpoints.cs")]
    public void Endpoints_piden_permisos_explicitamente(string relativePath)
    {
        var source = File.ReadAllText(ResolverRutaRepo(relativePath));

        Assert.Contains("RequirePermisoEmpresa(PermisoEmpresa.", source);
    }

    [Theory]
    [InlineData("src/CapitalPos.Api/Endpoints/EmpresaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UsuarioEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/CatalogoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/UnidadMedidaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ProductoEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/StockEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/SedeEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/CajaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ClienteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/CompraEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/VentaEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/PedidoDigitalEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ReporteEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/DashboardEndpoints.cs")]
    [InlineData("src/CapitalPos.Api/Endpoints/ConfiguracionFiscalEndpoints.cs")]
    public void Endpoints_no_comparan_roles_directamente_para_autorizar(string relativePath)
    {
        var source = File.ReadAllText(ResolverRutaRepo(relativePath));

        Assert.DoesNotContain(".Rol ==", source);
        Assert.DoesNotContain(".Rol !=", source);
    }

    [Fact]
    public void Historial_ventas_declara_proteccion_empresa_y_permiso_operar_ventas()
    {
        var source = File.ReadAllText(
            ResolverRutaRepo("src/CapitalPos.Api/Endpoints/VentaEndpoints.cs"));

        Assert.Contains("MapGet(\"/\", ListarVentasAsync)", source);
        Assert.Contains("MapGet(\"/{id:guid}\", ObtenerVentaDetalleAsync)", source);
        Assert.Contains(".RequireAuthorization()", source);
        Assert.Contains(".AddEndpointFilter<EmpresaActivaEndpointFilter>()", source);
        Assert.True(
            source.Split("RequirePermisoEmpresa(PermisoEmpresa.OperarVentas)").Length - 1 >= 3);
    }

    [Fact]
    public void Resumen_caja_declara_ruta_y_permiso_operar_ventas()
    {
        var source = File.ReadAllText(
            ResolverRutaRepo("src/CapitalPos.Api/Endpoints/CajaEndpoints.cs"));

        Assert.Contains(
            "MapGet(\"/{sesionCajaId:guid}/resumen\", ObtenerResumenAsync)",
            source);
        Assert.Contains(".RequireAuthorization()", source);
        Assert.Contains(".AddEndpointFilter<EmpresaActivaEndpointFilter>()", source);
        Assert.Contains(
            "RequirePermisoEmpresa(PermisoEmpresa.OperarVentas)",
            source);
    }

    [Fact]
    public void Pedidos_digitales_declaran_proteccion_empresa_y_permiso_operar_ventas()
    {
        var source = File.ReadAllText(
            ResolverRutaRepo("src/CapitalPos.Api/Endpoints/PedidoDigitalEndpoints.cs"));

        Assert.Contains("MapGet(\"/\", ListarPedidosDigitalesAsync)", source);
        Assert.Contains("MapPost(\"/\", CrearPedidoDigitalAsync)", source);
        Assert.Contains("MapGet(\"/{id:guid}\", ObtenerPedidoDigitalAsync)", source);
        Assert.Contains("MapPost(\"/{id:guid}/cancelar\", CancelarPedidoDigitalAsync)", source);
        Assert.Contains(
            "MapPost(\"/{id:guid}/convertir-venta\", ConvertirPedidoDigitalAVentaAsync)",
            source);
        Assert.Contains(".RequireAuthorization()", source);
        Assert.Contains(".AddEndpointFilter<EmpresaActivaEndpointFilter>()", source);
        Assert.True(
            source.Split("RequirePermisoEmpresa(PermisoEmpresa.OperarVentas)").Length - 1 >= 5);
    }

    [Fact]
    public void Compras_declaran_proteccion_empresa_y_permiso_operar_almacen()
    {
        var source = File.ReadAllText(
            ResolverRutaRepo("src/CapitalPos.Api/Endpoints/CompraEndpoints.cs"));

        Assert.Contains("MapGet(\"/\", ListarComprasAsync)", source);
        Assert.Contains("MapGet(\"/{id:guid}\", ObtenerCompraAsync)", source);
        Assert.Contains("MapPost(\"/\", CrearCompraAsync)", source);
        Assert.Contains(".RequireAuthorization()", source);
        Assert.Contains(".AddEndpointFilter<EmpresaActivaEndpointFilter>()", source);
        Assert.True(
            source.Split("RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen)").Length - 1 >= 3);
    }

    [Fact]
    public void Kardex_declara_ruta_y_permiso_operar_almacen()
    {
        var source = File.ReadAllText(
            ResolverRutaRepo("src/CapitalPos.Api/Endpoints/StockEndpoints.cs"));

        Assert.Contains("MapGet(\"/kardex\", ListarKardexAsync)", source);
        Assert.Contains(".RequireAuthorization()", source);
        Assert.Contains(".AddEndpointFilter<EmpresaActivaEndpointFilter>()", source);
        Assert.Contains(
            "RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen)",
            source);
    }

    private static string ResolverRutaRepo(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "CapitalPos.Api.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(directory.FullName, relativePath);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo resolver la raiz del repositorio.");
    }
}
