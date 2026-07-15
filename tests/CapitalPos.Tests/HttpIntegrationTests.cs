using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Persistence;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Usuarios;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CapitalPos.Tests;

public class HttpIntegrationTests
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EmpresaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string SigningKey = "capitalpos-http-integration-tests-signing-key-2026";
    private const string Issuer = "CapitalPos.Api";
    private const string Audience = "CapitalPos.Web";
    private const string ApiKeyFicticia = "capitalpos-cpe-http-tests-api-key";

    [Fact]
    public async Task Health_responde_sin_autenticacion()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ok\"", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task OpenApi_permanece_accesible_sin_autenticacion()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/health", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Endpoint_empresarial_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/empresas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_empresarial_con_jwt_sin_header_empresa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Header_empresa_con_formato_invalido_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);
        client.DefaultRequestHeaders.Add(EmpresaActivaHeaders.HeaderName, "empresa-invalida");

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identificador de empresa valido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Usuario_no_asociado_a_empresa_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = null
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("no pertenece", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Usuario_asociado_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/usuarios");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Usuario_autenticado_con_empresa_y_permiso_puede_acceder()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Administrador)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("20601234567", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Configuracion_fiscal_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/configuracion-fiscal");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Configuracion_fiscal_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/configuracion-fiscal");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Configuracion_fiscal_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/configuracion-fiscal");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_configuracion_fiscal_no_expone_configuracion_de_otra_empresa()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        factory.ConfiguracionFiscalRepository.Configuraciones.Add(
            CrearConfiguracionFiscal(otraEmpresaId, "20609999999"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/configuracion-fiscal");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("20609999999", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_configuracion_fiscal_devuelve_configuracion_de_empresa_activa()
    {
        await using var factory = new CapitalPosHttpFactory();
        factory.ConfiguracionFiscalRepository.Configuraciones.Add(
            CrearConfiguracionFiscal(EmpresaId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/configuracion-fiscal");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ConfiguracionFiscalEmpresaResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal("20601234567", body.Ruc);
        Assert.Equal("CapitalPOS SAC", body.RazonSocial);
        Assert.Equal("150101", body.Ubigeo);
        Assert.True(body.Activa);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Guardar_configuracion_fiscal_crea_para_empresa_activa()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = CrearConfiguracionFiscalRequest();

        var response = await client.PutAsJsonAsync("/api/configuracion-fiscal", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ConfiguracionFiscalEmpresaResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal("20601234567", body.Ruc);
        Assert.Contains("20601234567", content);
        Assert.Contains(factory.ConfiguracionFiscalRepository.Configuraciones, configuracion =>
            configuracion.EmpresaId == EmpresaId &&
            configuracion.Ruc == "20601234567");
        AssertSeguro(content);
    }

    [Fact]
    public async Task Guardar_configuracion_fiscal_ignora_empresa_id_libre_del_body()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new
        {
            EmpresaId = otraEmpresaId,
            Ruc = "20601234567",
            RazonSocial = "CapitalPOS SAC",
            NombreComercial = "CapitalPOS",
            Ubigeo = "150101",
            Direccion = "AV. AREQUIPA 123",
            Departamento = "LIMA",
            Provincia = "LIMA",
            Distrito = "LIMA",
            Activa = true
        };

        var response = await client.PutAsJsonAsync("/api/configuracion-fiscal", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ConfiguracionFiscalEmpresaResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.DoesNotContain(otraEmpresaId.ToString(), content);
        Assert.DoesNotContain(factory.ConfiguracionFiscalRepository.Configuraciones, configuracion =>
            configuracion.EmpresaId == otraEmpresaId);
        AssertSeguro(content);
    }

    [Theory]
    [InlineData("123", "150101", "CapitalPOS SAC", "AV. AREQUIPA 123", "RUC")]
    [InlineData("20601234567", "15010A", "CapitalPOS SAC", "AV. AREQUIPA 123", "ubigeo")]
    [InlineData("20601234567", "150101", "", "AV. AREQUIPA 123", "razon social")]
    [InlineData("20601234567", "150101", "CapitalPOS SAC", "", "direccion")]
    public async Task Guardar_configuracion_fiscal_valida_entrada(
        string ruc,
        string ubigeo,
        string razonSocial,
        string direccion,
        string errorEsperado)
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = CrearConfiguracionFiscalRequest(
            ruc: ruc,
            ubigeo: ubigeo,
            razonSocial: razonSocial,
            direccion: direccion);

        var response = await client.PutAsJsonAsync("/api/configuracion-fiscal", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(errorEsperado, content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.ConfiguracionFiscalRepository.Configuraciones);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Stock_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/stock/productos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Stock_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync($"/api/stock/productos/{Guid.NewGuid()}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Stock_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/stock/productos/{Guid.NewGuid()}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_stock_producto_devuelve_stock_de_empresa_activa()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            null,
            12m,
            2m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/stock/productos/{productoId}");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<StockProductoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(productoId, body.ProductoId);
        Assert.Null(body.ProductoVarianteId);
        Assert.Equal(12m, body.CantidadDisponible);
        Assert.Equal(2m, body.CantidadReservada);
        Assert.Equal(10m, body.StockLibre);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_stock_variante_devuelve_stock_de_variante()
    {
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ProductoVarianteRepository.AgregarAsync(CrearVariante(EmpresaId, productoId, varianteId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            varianteId,
            8m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/stock/productos/{productoId}/variantes/{varianteId}");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<StockProductoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(productoId, body.ProductoId);
        Assert.Equal(varianteId, body.ProductoVarianteId);
        Assert.Equal(8m, body.CantidadDisponible);
        Assert.Equal(8m, body.StockLibre);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Ajustar_stock_producto_guarda_para_empresa_activa()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new AjustarStockProductoRequest(productoId, null, 15m);

        var response = await client.PutAsJsonAsync("/api/stock/ajustar", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<StockProductoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(productoId, body.ProductoId);
        Assert.Equal(15m, body.CantidadDisponible);
        Assert.Contains(factory.StockRepository.Stocks, stock =>
            stock.EmpresaId == EmpresaId &&
            stock.ProductoId == productoId &&
            stock.CantidadDisponible == 15m);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_stock_no_expone_stock_de_otra_empresa()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            otraEmpresaId,
            productoId,
            null,
            99m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/stock/productos/{productoId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("99", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Ajustar_stock_producto_de_otra_empresa_devuelve_bad_request()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new AjustarStockProductoRequest(productoId, null, 15m);

        var response = await client.PutAsJsonAsync("/api/stock/ajustar", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("producto", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.StockRepository.Stocks);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Ajustar_stock_valida_entrada()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new AjustarStockProductoRequest(Guid.NewGuid(), null, -1m);

        var response = await client.PutAsJsonAsync("/api/stock/ajustar", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("cantidad", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.StockRepository.Stocks);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Producto_variantes_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/variantes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Producto_variantes_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/variantes");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Producto_variantes_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/variantes");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_variantes_devuelve_solo_producto_y_empresa_activa()
    {
        var productoId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, otroProductoId));
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        await factory.ProductoVarianteRepository.AgregarAsync(CrearVariante(EmpresaId, productoId, Guid.NewGuid()));
        await factory.ProductoVarianteRepository.AgregarAsync(CrearVariante(EmpresaId, otroProductoId, Guid.NewGuid()));
        await factory.ProductoVarianteRepository.AgregarAsync(CrearVariante(otraEmpresaId, productoId, Guid.NewGuid()));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/productos/{productoId}/variantes");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ProductoVarianteResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var variante = Assert.Single(body);
        Assert.Equal(EmpresaId, variante.EmpresaId);
        Assert.Equal(productoId, variante.ProductoId);
        Assert.DoesNotContain(otraEmpresaId.ToString(), content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_variante_funciona_con_talla_color_sku_y_codigo_barras()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearProductoVarianteRequest(
            Guid.NewGuid(),
            " M ",
            " Azul ",
            " SKU-AZ-M ",
            " 7750000000104 ");

        var response = await client.PostAsJsonAsync($"/api/productos/{productoId}/variantes", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ProductoVarianteResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(productoId, body.ProductoId);
        Assert.Equal("M", body.Talla);
        Assert.Equal("Azul", body.Color);
        Assert.Equal("SKU-AZ-M", body.CodigoSku);
        Assert.Equal("7750000000104", body.CodigoBarras);
        Assert.True(body.Activo);
        Assert.DoesNotContain(request.ProductoId.ToString(), content);
        Assert.Contains(factory.ProductoVarianteRepository.Variantes, variante =>
            variante.EmpresaId == EmpresaId &&
            variante.ProductoId == productoId &&
            variante.CodigoSku == "SKU-AZ-M");
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_variante_falla_si_producto_no_pertenece_a_empresa_activa()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearProductoVarianteRequest(productoId, Talla: "M");

        var response = await client.PostAsJsonAsync($"/api/productos/{productoId}/variantes", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("producto", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.ProductoVarianteRepository.Variantes);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_variante_rechaza_sku_y_codigo_barras_duplicados_por_empresa()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ProductoVarianteRepository.AgregarAsync(new ProductoVariante(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            codigoSku: "SKU-001",
            codigoBarras: "7750000000104"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var responseSku = await client.PostAsJsonAsync(
            $"/api/productos/{productoId}/variantes",
            new CrearProductoVarianteRequest(productoId, CodigoSku: "SKU-001"));
        var responseCodigo = await client.PostAsJsonAsync(
            $"/api/productos/{productoId}/variantes",
            new CrearProductoVarianteRequest(productoId, CodigoBarras: "7750000000104"));

        Assert.Equal(HttpStatusCode.BadRequest, responseSku.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, responseCodigo.StatusCode);
        Assert.Single(factory.ProductoVarianteRepository.Variantes);
    }

    [Fact]
    public async Task Activar_y_desactivar_variante_validan_producto_y_empresa()
    {
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ProductoVarianteRepository.AgregarAsync(CrearVariante(EmpresaId, productoId, varianteId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var desactivar = await client.PatchAsync(
            $"/api/productos/{productoId}/variantes/{varianteId}/desactivar",
            null);
        var activar = await client.PatchAsync(
            $"/api/productos/{productoId}/variantes/{varianteId}/activar",
            null);
        var body = await activar.Content.ReadFromJsonAsync<ProductoVarianteResponse>();

        Assert.Equal(HttpStatusCode.OK, desactivar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, activar.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Activo);
    }

    [Fact]
    public async Task Crear_venta_descuenta_stock()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            null,
            5m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(productoId, null, 2m));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(factory.VentaRepository.Ventas);
        Assert.Equal(3m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_venta_con_stock_insuficiente_no_persiste_venta_ni_cambia_stock()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            null,
            1m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(productoId, null, 2m));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Stock insuficiente", content);
        Assert.Empty(factory.VentaRepository.Ventas);
        Assert.Equal(1m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_venta_con_producto_de_otra_empresa_falla()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            otraEmpresaId,
            productoId,
            null,
            5m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(productoId, null, 1m));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("producto", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.VentaRepository.Ventas);
        Assert.Equal(5m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_venta_con_variante_de_otra_empresa_falla()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ProductoVarianteRepository.AgregarAsync(CrearVariante(otraEmpresaId, productoId, varianteId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            otraEmpresaId,
            productoId,
            varianteId,
            5m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(productoId, varianteId, 1m));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("variante", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.VentaRepository.Ventas);
        Assert.Equal(5m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Post_con_entrada_invalida_devuelve_error_de_validacion()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Administrador)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearEmpresaRequest("123", "CapitalPOS SAC");

        var response = await client.PostAsJsonAsync("/api/empresas", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("El RUC debe tener 11 digitos.", content);
        Assert.Empty(factory.EmpresaRepository.EmpresasAgregadas);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Excepcion_no_controlada_devuelve_error_seguro()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Administrador),
            LanzarExcepcionAlListarEmpresas = true
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/empresas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("error inesperado", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Correlation_id_se_propaga_en_la_respuesta()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        const string correlationId = "capitalpos-http-correlation-test";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Equal(correlationId, Assert.Single(values));
    }

    [Fact]
    public async Task X_forwarded_proto_https_permanece_compatible_con_health()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ok\"", content);
        AssertSeguro(content);
    }

    private static HttpClient CrearClienteAutenticado(
        CapitalPosHttpFactory factory,
        Guid usuarioId,
        Guid empresaId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(usuarioId);
        client.DefaultRequestHeaders.Add(EmpresaActivaHeaders.HeaderName, empresaId.ToString());

        return client;
    }

    private static AuthenticationHeaderValue CrearAuthorizationHeader(Guid usuarioId)
    {
        return new AuthenticationHeaderValue("Bearer", CrearJwt(usuarioId));
    }

    private static string CrearJwt(Guid usuarioId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "usuario.http@capitalpos.test"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UsuarioEmpresa CrearUsuarioEmpresa(RolEmpresa rol)
    {
        return new UsuarioEmpresa(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UsuarioId,
            EmpresaId,
            rol);
    }

    private static GuardarConfiguracionFiscalEmpresaRequest CrearConfiguracionFiscalRequest(
        string ruc = "20601234567",
        string razonSocial = "CapitalPOS SAC",
        string? nombreComercial = "CapitalPOS",
        string ubigeo = "150101",
        string direccion = "AV. AREQUIPA 123",
        string departamento = "LIMA",
        string provincia = "LIMA",
        string distrito = "LIMA",
        bool activa = true)
    {
        return new GuardarConfiguracionFiscalEmpresaRequest(
            ruc,
            razonSocial,
            nombreComercial,
            ubigeo,
            direccion,
            departamento,
            provincia,
            distrito,
            activa);
    }

    private static ConfiguracionFiscalEmpresa CrearConfiguracionFiscal(
        Guid empresaId,
        string ruc = "20601234567")
    {
        return new ConfiguracionFiscalEmpresa(
            empresaId,
            ruc,
            "CapitalPOS SAC",
            "CapitalPOS",
            "150101",
            "AV. AREQUIPA 123",
            "LIMA",
            "LIMA",
            "LIMA");
    }

    private static Producto CrearProducto(Guid empresaId, Guid productoId)
    {
        return new Producto(productoId, empresaId, "Producto stock", 10m);
    }

    private static ProductoVariante CrearVariante(Guid empresaId, Guid productoId, Guid varianteId)
    {
        return new ProductoVariante(
            varianteId,
            empresaId,
            productoId,
            talla: "M");
    }

    private static CrearVentaRequest CrearVentaRequest(
        Guid productoId,
        Guid? productoVarianteId,
        decimal cantidad)
    {
        return new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(productoId, productoVarianteId, cantidad, 10m, 0m, cantidad * 10m)]);
    }

    private static void AssertSeguro(string content)
    {
        Assert.DoesNotContain(SigningKey, content, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKeyFicticia, content, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=localhost", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", content, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapitalPosHttpFactory : WebApplicationFactory<Program>
    {
        public CapitalPosHttpFactory()
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__CapitalPos",
                "Host=localhost;Database=capitalpos_http_tests");
            Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
            Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
            Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
            Environment.SetEnvironmentVariable("CpeApi__BaseUrl", "http://localhost/capitalpos-cpe-tests/");
            Environment.SetEnvironmentVariable("CpeApi__ApiKey", ApiKeyFicticia);
            Environment.SetEnvironmentVariable("DemoSeed__Enabled", "false");
        }

        public FakeEmpresaRepository EmpresaRepository { get; } = new();

        public FakeUsuarioRepository UsuarioRepository { get; } = new();

        public FakeUsuarioEmpresaRepository UsuarioEmpresaRepository { get; } = new();

        public FakeConfiguracionFiscalEmpresaRepository ConfiguracionFiscalRepository { get; } = new();

        public FakeProductoRepository ProductoRepository { get; } = new();

        public FakeProductoVarianteRepository ProductoVarianteRepository { get; } = new();

        public FakeStockProductoRepository StockRepository { get; } = new();

        public FakeVentaRepository VentaRepository { get; } = new();

        public UsuarioEmpresa? UsuarioEmpresa
        {
            get => UsuarioEmpresaRepository.UsuarioEmpresa;
            set => UsuarioEmpresaRepository.UsuarioEmpresa = value;
        }

        public bool LanzarExcepcionAlListarEmpresas
        {
            get => EmpresaRepository.LanzarExcepcionAlListar;
            set => EmpresaRepository.LanzarExcepcionAlListar = value;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CapitalPos"] = "Host=localhost;Database=capitalpos_http_tests",
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:SigningKey"] = SigningKey,
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["CpeApi:BaseUrl"] = "http://localhost/capitalpos-cpe-tests/",
                    ["CpeApi:ApiKey"] = ApiKeyFicticia,
                    ["DemoSeed:Enabled"] = "false"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<CapitalPosDbContext>>();
                services.RemoveAll<CapitalPosDbContext>();
                services.RemoveAll<IEmpresaRepository>();
                services.RemoveAll<IUsuarioRepository>();
                services.RemoveAll<IUsuarioEmpresaRepository>();
                services.RemoveAll<IUsuarioCredencialRepository>();
                services.RemoveAll<IProductoRepository>();
                services.RemoveAll<IProductoVarianteRepository>();
                services.RemoveAll<IClienteRepository>();
                services.RemoveAll<IVentaRepository>();
                services.RemoveAll<IComprobanteRepository>();
                services.RemoveAll<IConfiguracionFiscalEmpresaRepository>();
                services.RemoveAll<IStockProductoRepository>();
                services.RemoveAll<IUnitOfWork>();

                services.AddSingleton<IEmpresaRepository>(EmpresaRepository);
                services.AddSingleton<IUsuarioRepository>(UsuarioRepository);
                services.AddSingleton<IUsuarioEmpresaRepository>(UsuarioEmpresaRepository);
                services.AddSingleton<IUsuarioCredencialRepository, FakeUsuarioCredencialRepository>();
                services.AddSingleton<IProductoRepository>(ProductoRepository);
                services.AddSingleton<IProductoVarianteRepository>(ProductoVarianteRepository);
                services.AddSingleton<IClienteRepository, FakeClienteRepository>();
                services.AddSingleton<IVentaRepository>(VentaRepository);
                services.AddSingleton<IComprobanteRepository, FakeComprobanteRepository>();
                services.AddSingleton<IConfiguracionFiscalEmpresaRepository>(ConfiguracionFiscalRepository);
                services.AddSingleton<IStockProductoRepository>(StockRepository);
                services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
            });
        }
    }

    private sealed class FakeEmpresaRepository : IEmpresaRepository
    {
        private readonly List<Empresa> _empresas =
        [
            new Empresa(EmpresaId, "20601234567", "CapitalPOS SAC", "CapitalPOS")
        ];

        public List<Empresa> EmpresasAgregadas { get; } = [];

        public bool LanzarExcepcionAlListar { get; set; }

        public Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            EmpresasAgregadas.Add(empresa);
            _empresas.Add(empresa);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Empresa>> ListarAsync(CancellationToken cancellationToken = default)
        {
            if (LanzarExcepcionAlListar)
            {
                throw new InvalidOperationException("Fallo interno de prueba con secreto simulado");
            }

            return Task.FromResult<IReadOnlyCollection<Empresa>>(_empresas);
        }

        public Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_empresas.FirstOrDefault(empresa => empresa.Id == id));
        }

        public Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteRucAsync(string ruc, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_empresas.Any(empresa => empresa.Ruc == ruc));
        }
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios =
        [
            new Usuario(UsuarioId, "Usuario", "HTTP", "usuario.http@capitalpos.test")
        ];

        public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            _usuarios.Add(usuario);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Usuario>>(_usuarios);
        }

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.Id == id));
        }

        public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuarios.FirstOrDefault(
                usuario => usuario.Correo == correo.Trim().ToLowerInvariant()));
        }

        public Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuarios.Any(usuario => usuario.Correo == correo.Trim().ToLowerInvariant()));
        }
    }

    private sealed class FakeUsuarioEmpresaRepository : IUsuarioEmpresaRepository
    {
        public UsuarioEmpresa? UsuarioEmpresa { get; set; } = CrearUsuarioEmpresa(RolEmpresa.Administrador);

        public Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            UsuarioEmpresa = usuarioEmpresa;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<UsuarioEmpresa>> ListarAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<UsuarioEmpresa> result = UsuarioEmpresa is null
                ? []
                : [UsuarioEmpresa];

            return Task.FromResult(result);
        }

        public Task<UsuarioEmpresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UsuarioEmpresa?.Id == id ? UsuarioEmpresa : null);
        }

        public Task<UsuarioEmpresa?> ObtenerPorUsuarioYEmpresaAsync(
            Guid usuarioId,
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            var result = UsuarioEmpresa is not null &&
                UsuarioEmpresa.UsuarioId == usuarioId &&
                UsuarioEmpresa.EmpresaId == empresaId
                    ? UsuarioEmpresa
                    : null;

            return Task.FromResult(result);
        }

        public Task ActualizarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            UsuarioEmpresa = usuarioEmpresa;

            return Task.CompletedTask;
        }

        public Task<bool> ExisteAsignacionAsync(
            Guid usuarioId,
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UsuarioEmpresa is not null &&
                UsuarioEmpresa.UsuarioId == usuarioId &&
                UsuarioEmpresa.EmpresaId == empresaId);
        }
    }

    private sealed class FakeUsuarioCredencialRepository : IUsuarioCredencialRepository
    {
        public Task<UsuarioCredencial?> ObtenerPorUsuarioIdAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UsuarioCredencial?>(null);
        }
    }

    private sealed class FakeProductoRepository : IProductoRepository
    {
        public List<Producto> Productos { get; } = [];

        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            Productos.Add(producto);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Producto>>(
                Productos.Where(producto => producto.EmpresaId == empresaId).ToArray());
        }

        public Task<Producto?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Productos.FirstOrDefault(producto =>
                producto.EmpresaId == empresaId &&
                producto.Id == id));
        }

        public Task ActualizarAsync(
            Producto producto,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductoVarianteRepository : IProductoVarianteRepository
    {
        public List<ProductoVariante> Variantes { get; } = [];

        public Task AgregarAsync(
            ProductoVariante variante,
            CancellationToken cancellationToken = default)
        {
            Variantes.Add(variante);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>(
                Variantes.Where(variante =>
                    variante.EmpresaId == empresaId &&
                    variante.ProductoId == productoId).ToArray());
        }

        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.FirstOrDefault(variante =>
                variante.EmpresaId == empresaId &&
                variante.Id == id));
        }

        public Task ActualizarAsync(
            ProductoVariante variante,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteSkuAsync(
            Guid empresaId,
            string codigoSku,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.Any(variante =>
                variante.EmpresaId == empresaId &&
                variante.CodigoSku == codigoSku.Trim()));
        }

        public Task<bool> ExisteCodigoBarrasAsync(
            Guid empresaId,
            string codigoBarras,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.Any(variante =>
                variante.EmpresaId == empresaId &&
                variante.CodigoBarras == codigoBarras.Trim()));
        }
    }

    private sealed class FakeClienteRepository : IClienteRepository
    {
        public Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Cliente>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Cliente>>([]);
        }

        public Task<Cliente?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Cliente?>(null);
        }

        public Task ActualizarAsync(
            Cliente cliente,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeVentaRepository : IVentaRepository
    {
        public List<Venta> Ventas { get; } = [];

        public Task AgregarAsync(Venta venta, CancellationToken cancellationToken = default)
        {
            Ventas.Add(venta);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Venta>>(
                Ventas.Where(venta => venta.EmpresaId == empresaId).ToArray());
        }

        public Task<Venta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Ventas.FirstOrDefault(venta =>
                venta.EmpresaId == empresaId &&
                venta.Id == id));
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeComprobanteRepository : IComprobanteRepository
    {
        public Task AgregarAsync(Comprobante comprobante, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeConfiguracionFiscalEmpresaRepository : IConfiguracionFiscalEmpresaRepository
    {
        public List<ConfiguracionFiscalEmpresa> Configuraciones { get; } = [];

        public Task<ConfiguracionFiscalEmpresa?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Configuraciones.FirstOrDefault(
                configuracion => configuracion.EmpresaId == empresaId));
        }

        public Task GuardarAsync(
            ConfiguracionFiscalEmpresa configuracion,
            CancellationToken cancellationToken = default)
        {
            var existente = Configuraciones.FindIndex(
                item => item.EmpresaId == configuracion.EmpresaId);

            if (existente >= 0)
            {
                Configuraciones[existente] = configuracion;
            }
            else
            {
                Configuraciones.Add(configuracion);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeStockProductoRepository : IStockProductoRepository
    {
        public List<StockProducto> Stocks { get; } = [];

        public Task<StockProducto?> ObtenerPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            Guid? productoVarianteId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stocks.FirstOrDefault(stock =>
                stock.EmpresaId == empresaId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId == productoVarianteId));
        }

        public Task GuardarAsync(
            StockProducto stock,
            CancellationToken cancellationToken = default)
        {
            var index = Stocks.FindIndex(actual =>
                actual.EmpresaId == stock.EmpresaId &&
                actual.ProductoId == stock.ProductoId &&
                actual.ProductoVarianteId == stock.ProductoVarianteId);
            if (index >= 0)
            {
                Stocks[index] = stock;
            }
            else
            {
                Stocks.Add(stock);
            }

            return Task.CompletedTask;
        }
    }
}
