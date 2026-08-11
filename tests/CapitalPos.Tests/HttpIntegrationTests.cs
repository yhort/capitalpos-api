using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Caja;
using CapitalPos.Application.Catalogo;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.Compras;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Dashboard;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Pedidos;
using CapitalPos.Application.Persistence;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Reportes;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Series;
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
    private static readonly Guid SedeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PuntoVentaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string SigningKey = "capitalpos-http-integration-tests-signing-key-2026";
    private const string Issuer = "CapitalPos.Api";
    private const string Audience = "CapitalPos.Web";
    private const string ApiKeyFicticia = "capitalpos-cpe-http-tests-api-key";
    // Marcador decimal (con punto) para filtrar fugas multiempresa sin colisionar con Guid hex ("999").
    private const decimal TotalVentaOtraEmpresa = 8888.88m;
    private const string MarcadorVentaOtraEmpresa = "8888.88";

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
    public async Task Categorias_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/categorias");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Categorias_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/categorias");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Categorias_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/categorias");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_categorias_devuelve_solo_activas_de_empresa_activa()
    {
        var categoriaActivaId = Guid.NewGuid();
        var categoriaInactivaId = Guid.NewGuid();
        var categoriaOtraEmpresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.CategoriaRepository.AgregarAsync(new Categoria(categoriaActivaId, EmpresaId, "Polos"));
        await factory.CategoriaRepository.AgregarAsync(new Categoria(categoriaInactivaId, EmpresaId, "Inactiva", activa: false));
        await factory.CategoriaRepository.AgregarAsync(new Categoria(categoriaOtraEmpresaId, otraEmpresaId, "Ajena"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/categorias");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<CategoriaResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var categoria = Assert.Single(body);
        Assert.Equal(categoriaActivaId, categoria.Id);
        Assert.Equal(EmpresaId, categoria.EmpresaId);
        Assert.Null(categoria.CategoriaPadreId);
        Assert.Equal("Polos", categoria.Nombre);
        Assert.True(categoria.Activa);
        Assert.DoesNotContain(categoriaInactivaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(categoriaOtraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_categoria_guarda_en_empresa_activa_e_ignora_empresa_id_libre()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new
        {
            EmpresaId = otraEmpresaId,
            Nombre = " Polos ",
            CategoriaPadreId = (Guid?)null
        };

        var response = await client.PostAsJsonAsync("/api/categorias", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<CategoriaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal("Polos", body.Nombre);
        Assert.DoesNotContain(otraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(factory.CategoriaRepository.Categorias, categoria =>
            categoria.EmpresaId == EmpresaId &&
            categoria.Nombre == "Polos");
        Assert.DoesNotContain(factory.CategoriaRepository.Categorias, categoria =>
            categoria.EmpresaId == otraEmpresaId);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_categoria_con_padre_de_otra_empresa_falla()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var categoriaPadreId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.CategoriaRepository.AgregarAsync(new Categoria(categoriaPadreId, otraEmpresaId, "Ajena"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearCategoriaRequest("Polos", categoriaPadreId);

        var response = await client.PostAsJsonAsync("/api/categorias", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("categoria padre", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(factory.CategoriaRepository.Categorias, categoria =>
            categoria.EmpresaId == EmpresaId);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_categoria_de_segundo_nivel_falla()
    {
        var abueloId = Guid.NewGuid();
        var categoriaPadreId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.CategoriaRepository.AgregarAsync(new Categoria(categoriaPadreId, EmpresaId, "Polos", abueloId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearCategoriaRequest("Manga corta", categoriaPadreId);

        var response = await client.PostAsJsonAsync("/api/categorias", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("un nivel", content, StringComparison.OrdinalIgnoreCase);
        Assert.Single(factory.CategoriaRepository.Categorias);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_categoria_duplicada_por_empresa_falla()
    {
        await using var factory = new CapitalPosHttpFactory();
        await factory.CategoriaRepository.AgregarAsync(new Categoria(Guid.NewGuid(), EmpresaId, "Polos"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearCategoriaRequest(" Polos ");

        var response = await client.PostAsJsonAsync("/api/categorias", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Ya existe", content, StringComparison.OrdinalIgnoreCase);
        Assert.Single(factory.CategoriaRepository.Categorias);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_categoria_valida_nombre_obligatorio()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearCategoriaRequest(" ");

        var response = await client.PostAsJsonAsync("/api/categorias", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("nombre", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.CategoriaRepository.Categorias);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Marcas_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/marcas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Marcas_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/marcas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Marcas_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/marcas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_marcas_devuelve_solo_activas_de_empresa_activa()
    {
        var marcaActivaId = Guid.NewGuid();
        var marcaInactivaId = Guid.NewGuid();
        var marcaOtraEmpresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.MarcaRepository.AgregarAsync(new Marca(marcaActivaId, EmpresaId, "Brooklyn"));
        await factory.MarcaRepository.AgregarAsync(new Marca(marcaInactivaId, EmpresaId, "Inactiva", activa: false));
        await factory.MarcaRepository.AgregarAsync(new Marca(marcaOtraEmpresaId, otraEmpresaId, "Ajena"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/marcas");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<MarcaResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var marca = Assert.Single(body);
        Assert.Equal(marcaActivaId, marca.Id);
        Assert.Equal(EmpresaId, marca.EmpresaId);
        Assert.Equal("Brooklyn", marca.Nombre);
        Assert.True(marca.Activa);
        Assert.DoesNotContain(marcaInactivaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(marcaOtraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_marca_guarda_en_empresa_activa_e_ignora_empresa_id_libre()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new
        {
            EmpresaId = otraEmpresaId,
            Nombre = " Brooklyn "
        };

        var response = await client.PostAsJsonAsync("/api/marcas", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<MarcaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal("Brooklyn", body.Nombre);
        Assert.DoesNotContain(otraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(factory.MarcaRepository.Marcas, marca =>
            marca.EmpresaId == EmpresaId &&
            marca.Nombre == "Brooklyn");
        Assert.DoesNotContain(factory.MarcaRepository.Marcas, marca =>
            marca.EmpresaId == otraEmpresaId);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_marca_duplicada_por_empresa_falla()
    {
        await using var factory = new CapitalPosHttpFactory();
        await factory.MarcaRepository.AgregarAsync(new Marca(Guid.NewGuid(), EmpresaId, "Brooklyn"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearMarcaRequest(" Brooklyn ");

        var response = await client.PostAsJsonAsync("/api/marcas", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Ya existe", content, StringComparison.OrdinalIgnoreCase);
        Assert.Single(factory.MarcaRepository.Marcas);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_marca_valida_nombre_obligatorio()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearMarcaRequest(" ");

        var response = await client.PostAsJsonAsync("/api/marcas", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("nombre", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.MarcaRepository.Marcas);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Reporte_ventas_por_canal_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reportes/ventas-por-canal?desde=2026-05-01&hasta=2026-05-31");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reporte_ventas_por_canal_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/reportes/ventas-por-canal?desde=2026-05-01&hasta=2026-05-31");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Reporte_ventas_por_canal_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Almacenero)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/reportes/ventas-por-canal?desde=2026-05-01&hasta=2026-05-31");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Reporte_ventas_por_canal_rechaza_rango_invalido()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/reportes/ventas-por-canal?desde=2026-06-01&hasta=2026-05-31");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("desde no puede ser mayor", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Reporte_ventas_por_canal_agrupa_filtra_y_calcula_totales()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            [(2m, 100m), (1m, 50m)]));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.PROVINCIA,
            new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero),
            [(3m, 90m)]));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
            [(1m, 20m)]));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 4, 30, 10, 0, 0, TimeSpan.Zero),
            [(9m, 900m)]));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            otraEmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            [(99m, TotalVentaOtraEmpresa)]));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/reportes/ventas-por-canal?desde=2026-05-01&hasta=2026-05-31");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ReporteVentasPorCanalResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(new DateOnly(2026, 5, 1), body.Desde);
        Assert.Equal(new DateOnly(2026, 5, 31), body.Hasta);
        Assert.Equal(Enum.GetValues<CanalVenta>().Length, body.Items.Count);
        var tienda = body.Items.Single(item => item.CanalVenta == "TIENDA");
        Assert.Equal(1, tienda.CantidadVentas);
        Assert.Equal(3m, tienda.Unidades);
        Assert.Equal(150m, tienda.Soles);
        Assert.Equal(50m, tienda.PrecioPromedio);
        var provincia = body.Items.Single(item => item.CanalVenta == "PROVINCIA");
        Assert.Equal(1, provincia.CantidadVentas);
        Assert.Equal(3m, provincia.Unidades);
        Assert.Equal(90m, provincia.Soles);
        Assert.Equal(30m, provincia.PrecioPromedio);
        var marketing = body.Items.Single(item => item.CanalVenta == "MARKETING");
        Assert.Equal(1, marketing.CantidadVentas);
        Assert.Equal(1m, marketing.Unidades);
        Assert.Equal(20m, marketing.Soles);
        Assert.Equal(20m, marketing.PrecioPromedio);
        var mayorista = body.Items.Single(item => item.CanalVenta == "MAYORISTA");
        Assert.Equal(0, mayorista.CantidadVentas);
        Assert.Equal(0m, mayorista.Unidades);
        Assert.Equal(0m, mayorista.Soles);
        Assert.Equal(0m, mayorista.PrecioPromedio);
        Assert.Equal(3, body.TotalGeneral.CantidadVentas);
        Assert.Equal(7m, body.TotalGeneral.Unidades);
        Assert.Equal(260m, body.TotalGeneral.Soles);
        Assert.Equal(37.14m, body.TotalGeneral.PrecioPromedio);
        Assert.DoesNotContain(MarcadorVentaOtraEmpresa, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Reporte_ventas_por_canal_sin_ventas_devuelve_canales_y_total_en_cero()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/reportes/ventas-por-canal?desde=2026-05-01&hasta=2026-05-31");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ReporteVentasPorCanalResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(Enum.GetValues<CanalVenta>().Length, body.Items.Count);
        Assert.All(body.Items, item =>
        {
            Assert.Equal(0, item.CantidadVentas);
            Assert.Equal(0m, item.Unidades);
            Assert.Equal(0m, item.Soles);
            Assert.Equal(0m, item.PrecioPromedio);
        });
        Assert.Equal(0, body.TotalGeneral.CantidadVentas);
        Assert.Equal(0m, body.TotalGeneral.Unidades);
        Assert.Equal(0m, body.TotalGeneral.Soles);
        Assert.Equal(0m, body.TotalGeneral.PrecioPromedio);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Reporte_ventas_por_sede_vendedor_agrupa_filtra_y_calcula_totales()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var sedeB = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var vendedorA = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var vendedorB = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        await using var factory = new CapitalPosHttpFactory();
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            [(2m, 100m), (1m, 50m)],
            sedeId: SedeId,
            vendedorId: vendedorA));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            [(1m, 40m)],
            sedeId: SedeId,
            vendedorId: vendedorA));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.PROVINCIA,
            new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero),
            [(3m, 90m)],
            sedeId: SedeId,
            vendedorId: vendedorB));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
            [(1m, 20m)],
            sedeId: sedeB,
            vendedorId: null));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 4, 30, 10, 0, 0, TimeSpan.Zero),
            [(9m, 900m)],
            sedeId: SedeId,
            vendedorId: vendedorA));
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            otraEmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            [(99m, TotalVentaOtraEmpresa)],
            sedeId: SedeId,
            vendedorId: vendedorA));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/reportes/ventas-por-sede-vendedor?desde=2026-05-01&hasta=2026-05-31");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ReporteVentasPorSedeVendedorResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(new DateOnly(2026, 5, 1), body.Desde);
        Assert.Equal(new DateOnly(2026, 5, 31), body.Hasta);
        Assert.Equal(3, body.Items.Count);
        var sedeAVendedorA = body.Items.Single(item => item.SedeId == SedeId && item.VendedorId == vendedorA);
        Assert.Equal(2, sedeAVendedorA.CantidadVentas);
        Assert.Equal(4m, sedeAVendedorA.Unidades);
        Assert.Equal(190m, sedeAVendedorA.Soles);
        Assert.Equal(47.5m, sedeAVendedorA.PrecioPromedio);
        var sedeAVendedorB = body.Items.Single(item => item.SedeId == SedeId && item.VendedorId == vendedorB);
        Assert.Equal(1, sedeAVendedorB.CantidadVentas);
        Assert.Equal(3m, sedeAVendedorB.Unidades);
        Assert.Equal(90m, sedeAVendedorB.Soles);
        Assert.Equal(30m, sedeAVendedorB.PrecioPromedio);
        var sedeBSinVendedor = body.Items.Single(item => item.SedeId == sedeB && item.VendedorId is null);
        Assert.Equal(1, sedeBSinVendedor.CantidadVentas);
        Assert.Equal(1m, sedeBSinVendedor.Unidades);
        Assert.Equal(20m, sedeBSinVendedor.Soles);
        Assert.Equal(20m, sedeBSinVendedor.PrecioPromedio);
        Assert.Equal(4, body.TotalGeneral.CantidadVentas);
        Assert.Equal(8m, body.TotalGeneral.Unidades);
        Assert.Equal(300m, body.TotalGeneral.Soles);
        Assert.Equal(37.5m, body.TotalGeneral.PrecioPromedio);
        Assert.DoesNotContain(MarcadorVentaOtraEmpresa, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Reporte_ventas_por_sede_vendedor_rechaza_rango_invalido()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/reportes/ventas-por-sede-vendedor?desde=2026-06-01&hasta=2026-05-31");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("desde no puede ser mayor", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Dashboard_comercial_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/comercial");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_comercial_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/dashboard/comercial");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Dashboard_comercial_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Almacenero)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/dashboard/comercial");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Dashboard_comercial_devuelve_resumen_top_y_stock_bajo()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        factory.ProductoRepository.Productos.Add(new Producto(productoId, EmpresaId, "Polo", 50m, codigoSku: "POLO"));
        await factory.ProductoVarianteRepository.AgregarAsync(new ProductoVariante(
            varianteId,
            EmpresaId,
            productoId,
            talla: "M",
            color: "Negro",
            codigoSku: "POLO-M-NEGRO",
            codigoBarras: "7750000000010"));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            varianteId,
            4m,
            1m));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            Guid.NewGuid(),
            null,
            10m));
        await factory.VentaRepository.AgregarAsync(CrearVentaDashboard(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, varianteId, 2m, 100m)]));
        await factory.VentaRepository.AgregarAsync(CrearVentaDashboard(
            EmpresaId,
            CanalVenta.PROVINCIA,
            new DateTimeOffset(2026, 7, 17, 11, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, varianteId, 3m, 90m)]));
        await factory.VentaRepository.AgregarAsync(CrearVentaDashboard(
            otraEmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, varianteId, 99m, TotalVentaOtraEmpresa)]));
        var ventaAnulada = CrearVentaDashboard(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, varianteId, 9m, 900m)]);
        ventaAnulada.Anular();
        await factory.VentaRepository.AgregarAsync(ventaAnulada);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/dashboard/comercial");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<DashboardComercialResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(new DateOnly(2026, 7, 17), body.Fecha);
        Assert.Equal(190m, body.Resumen.ImporteTotalVendido);
        Assert.Equal(2, body.Resumen.CantidadOperaciones);
        Assert.Equal(5m, body.Resumen.UnidadesVendidas);
        Assert.Equal("TIENDA", body.Resumen.CanalLider?.CanalVenta);
        Assert.Single(body.TopProductos);
        Assert.Equal(varianteId, body.TopProductos.Single().ProductoVarianteId);
        Assert.Equal("POLO-M-NEGRO", body.TopProductos.Single().CodigoSku);
        Assert.Single(body.StockBajo);
        Assert.Equal(3m, body.StockBajo.Single().StockLibre);
        Assert.DoesNotContain(MarcadorVentaOtraEmpresa, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Dashboard_reporte_canales_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/reporte-canales");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_reporte_canales_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/dashboard/reporte-canales");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Dashboard_reporte_canales_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Almacenero)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/dashboard/reporte-canales");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Dashboard_reporte_canales_agrupa_por_canal_incluyendo_ceros()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        factory.ProductoRepository.Productos.Add(new Producto(productoId, EmpresaId, "Polo", 50m, codigoSku: "POLO"));
        await factory.VentaRepository.AgregarAsync(CrearVentaDashboard(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 2m, 100m)]));
        await factory.VentaRepository.AgregarAsync(CrearVentaDashboard(
            EmpresaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 7, 17, 11, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 3m, 80m)]));
        await factory.VentaRepository.AgregarAsync(CrearVentaDashboard(
            otraEmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 99m, TotalVentaOtraEmpresa)]));
        var ventaAnulada = CrearVentaDashboard(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 9m, 900m)]);
        ventaAnulada.Anular();
        await factory.VentaRepository.AgregarAsync(ventaAnulada);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/dashboard/reporte-canales");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<DashboardReporteCanalesResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(new DateOnly(2026, 7, 17), body.Fecha);
        Assert.Equal(180m, body.Total.MontoFacturado);
        Assert.Equal(2, body.Total.CantidadTransacciones);
        Assert.Equal(Enum.GetValues<CanalVenta>().Length, body.Canales.Count);
        Assert.Equal(100m, body.Canales.Single(c => c.CanalVenta == "TIENDA").MontoFacturado);
        Assert.Equal(80m, body.Canales.Single(c => c.CanalVenta == "MARKETING").MontoFacturado);
        Assert.Equal(0, body.Canales.Single(c => c.CanalVenta == "PROVINCIA").CantidadTransacciones);
        Assert.DoesNotContain(MarcadorVentaOtraEmpresa, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Stock_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/stock/productos/{Guid.NewGuid()}?sedeId={SedeId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Stock_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync($"/api/stock/productos/{Guid.NewGuid()}?sedeId={SedeId}");
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

        var response = await client.GetAsync($"/api/stock/productos/{Guid.NewGuid()}?sedeId={SedeId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Sedes_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/sedes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sedes_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/sedes");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Sedes_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Almacenero)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/sedes");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_sedes_devuelve_solo_sedes_activas_de_empresa_activa()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var sedeInactivaId = Guid.NewGuid();
        var sedeOtraEmpresaId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        factory.SedeRepository.Sedes.Add(new Sede(
            sedeInactivaId,
            EmpresaId,
            "Sede inactiva",
            TipoSede.ALMACEN,
            activa: false));
        factory.SedeRepository.Sedes.Add(new Sede(
            sedeOtraEmpresaId,
            otraEmpresaId,
            "Sede externa",
            TipoSede.TIENDA));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/sedes");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<SedeResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var sede = Assert.Single(body);
        Assert.Equal(SedeId, sede.Id);
        Assert.Equal(EmpresaId, sede.EmpresaId);
        Assert.Equal("TIENDA", sede.Tipo);
        Assert.True(sede.Activa);
        Assert.DoesNotContain(sedeInactivaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(sedeOtraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_puntos_venta_devuelve_solo_activos_de_sede_y_empresa_activa()
    {
        var otraSedeId = Guid.NewGuid();
        var puntoOtraSedeId = Guid.NewGuid();
        var puntoInactivoId = Guid.NewGuid();
        var puntoOtraEmpresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        factory.SedeRepository.Sedes.Add(new Sede(
            otraSedeId,
            EmpresaId,
            "Otra sede",
            TipoSede.TIENDA));
        factory.PuntoVentaRepository.PuntosVenta.Add(new PuntoVenta(
            puntoOtraSedeId,
            EmpresaId,
            otraSedeId,
            "Caja otra sede"));
        factory.PuntoVentaRepository.PuntosVenta.Add(new PuntoVenta(
            puntoInactivoId,
            EmpresaId,
            SedeId,
            "Caja inactiva",
            activo: false));
        factory.PuntoVentaRepository.PuntosVenta.Add(new PuntoVenta(
            puntoOtraEmpresaId,
            otraEmpresaId,
            SedeId,
            "Caja externa"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/sedes/{SedeId}/puntos-venta");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<PuntoVentaResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var puntoVenta = Assert.Single(body);
        Assert.Equal(PuntoVentaId, puntoVenta.Id);
        Assert.Equal(EmpresaId, puntoVenta.EmpresaId);
        Assert.Equal(SedeId, puntoVenta.SedeId);
        Assert.True(puntoVenta.Activo);
        Assert.DoesNotContain(puntoOtraSedeId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(puntoInactivoId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(puntoOtraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_puntos_venta_de_sede_de_otra_empresa_devuelve_not_found()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var sedeOtraEmpresaId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        factory.SedeRepository.Sedes.Add(new Sede(
            sedeOtraEmpresaId,
            otraEmpresaId,
            "Sede externa",
            TipoSede.TIENDA));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/sedes/{sedeOtraEmpresaId}/puntos-venta");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Caja_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/caja/sesiones/abierta?puntoVentaId={PuntoVentaId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Caja_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync($"/api/caja/sesiones/abierta?puntoVentaId={PuntoVentaId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Caja_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Almacenero)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/caja/sesiones/abierta?puntoVentaId={PuntoVentaId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_caja_abierta_exige_punto_venta_valido()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/caja/sesiones/abierta?puntoVentaId=00000000-0000-0000-0000-000000000000");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("punto de venta", content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_caja_abierta_devuelve_sesion_de_empresa_activa()
    {
        var sesionOtraEmpresaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        factory.SesionCajaRepository.Sesiones.Add(new SesionCaja(
            sesionOtraEmpresaId,
            otraEmpresaId,
            SedeId,
            PuntoVentaId,
            50m));
        factory.SesionCajaRepository.Sesiones.Add(new SesionCaja(
            sesionId,
            EmpresaId,
            SedeId,
            PuntoVentaId,
            100m,
            observacionApertura: "Apertura"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/caja/sesiones/abierta?puntoVentaId={PuntoVentaId}");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<SesionCajaResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(sesionId, body.Id);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(SedeId, body.SedeId);
        Assert.Equal(PuntoVentaId, body.PuntoVentaId);
        Assert.Equal("Abierta", body.Estado);
        Assert.Equal(100m, body.MontoInicial);
        Assert.Null(body.MontoDeclaradoCierre);
        Assert.Null(body.DiferenciaCierre);
        Assert.Equal("Apertura", body.ObservacionApertura);
        Assert.DoesNotContain(sesionOtraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_caja_abierta_sin_sesion_devuelve_not_found()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/caja/sesiones/abierta?puntoVentaId={PuntoVentaId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Abrir_caja_crea_sesion_para_punto_venta_de_empresa_activa_e_ignora_empresa_id_libre()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        using var request = new StringContent(
            $$"""
            {
              "empresaId": "{{otraEmpresaId}}",
              "puntoVentaId": "{{PuntoVentaId}}",
              "montoInicial": 75.50,
              "observacionApertura": " Apertura POS "
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/caja/sesiones/abrir", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<SesionCajaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(SedeId, body.SedeId);
        Assert.Equal(PuntoVentaId, body.PuntoVentaId);
        Assert.Equal("Abierta", body.Estado);
        Assert.Equal(75.50m, body.MontoInicial);
        Assert.Equal("Apertura POS", body.ObservacionApertura);
        var sesion = Assert.Single(factory.SesionCajaRepository.Sesiones);
        Assert.Equal(EmpresaId, sesion.EmpresaId);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Abrir_caja_falla_con_monto_negativo_punto_venta_ajeno_o_sesion_abierta()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var puntoVentaOtraEmpresaId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        factory.PuntoVentaRepository.PuntosVenta.Add(new PuntoVenta(
            puntoVentaOtraEmpresaId,
            otraEmpresaId,
            SedeId,
            "Caja externa"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var montoNegativo = await client.PostAsJsonAsync(
            "/api/caja/sesiones/abrir",
            new AbrirSesionCajaRequest(PuntoVentaId, -1m));
        var puntoAjeno = await client.PostAsJsonAsync(
            "/api/caja/sesiones/abrir",
            new AbrirSesionCajaRequest(puntoVentaOtraEmpresaId, 10m));
        factory.SesionCajaRepository.Sesiones.Add(new SesionCaja(Guid.NewGuid(), EmpresaId, SedeId, PuntoVentaId, 10m));
        var dobleApertura = await client.PostAsJsonAsync(
            "/api/caja/sesiones/abrir",
            new AbrirSesionCajaRequest(PuntoVentaId, 10m));

        Assert.Equal(HttpStatusCode.BadRequest, montoNegativo.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, puntoAjeno.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, dobleApertura.StatusCode);
        Assert.Single(factory.SesionCajaRepository.Sesiones);
    }

    [Fact]
    public async Task Cerrar_caja_cierra_sesion_abierta_y_calcula_diferencia()
    {
        var sesionId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        factory.SesionCajaRepository.Sesiones.Add(new SesionCaja(
            sesionId,
            EmpresaId,
            SedeId,
            PuntoVentaId,
            100m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync(
            $"/api/caja/sesiones/{sesionId}/cerrar",
            new CerrarSesionCajaRequest(Guid.Empty, 130m, " Cierre POS "));
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<SesionCajaResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(sesionId, body.Id);
        Assert.Equal("Cerrada", body.Estado);
        Assert.Equal(130m, body.MontoDeclaradoCierre);
        Assert.Equal(30m, body.DiferenciaCierre);
        Assert.NotNull(body.FechaCierre);
        Assert.Equal("Cierre POS", body.ObservacionCierre);
        Assert.Equal(EstadoSesionCaja.Cerrada, factory.SesionCajaRepository.Sesiones.Single().Estado);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Cerrar_caja_falla_con_monto_negativo_caja_ajena_o_ya_cerrada()
    {
        var sesionId = Guid.NewGuid();
        var sesionOtraEmpresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        var sesionCerrada = new SesionCaja(sesionId, EmpresaId, SedeId, PuntoVentaId, 100m);
        sesionCerrada.Cerrar(100m, sesionCerrada.FechaApertura.AddHours(1));
        factory.SesionCajaRepository.Sesiones.Add(sesionCerrada);
        factory.SesionCajaRepository.Sesiones.Add(new SesionCaja(
            sesionOtraEmpresaId,
            otraEmpresaId,
            SedeId,
            PuntoVentaId,
            100m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var montoNegativo = await client.PostAsJsonAsync(
            $"/api/caja/sesiones/{sesionId}/cerrar",
            new CerrarSesionCajaRequest(Guid.Empty, -1m));
        var cajaAjena = await client.PostAsJsonAsync(
            $"/api/caja/sesiones/{sesionOtraEmpresaId}/cerrar",
            new CerrarSesionCajaRequest(Guid.Empty, 100m));
        var yaCerrada = await client.PostAsJsonAsync(
            $"/api/caja/sesiones/{sesionId}/cerrar",
            new CerrarSesionCajaRequest(Guid.Empty, 100m));

        Assert.Equal(HttpStatusCode.BadRequest, montoNegativo.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, cajaAjena.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, yaCerrada.StatusCode);
        Assert.Equal(EstadoSesionCaja.Cerrada, sesionCerrada.Estado);
    }

    [Fact]
    public async Task Resumen_caja_devuelve_ventas_pagos_y_diferencia_operativa()
    {
        var apertura = new DateTimeOffset(2026, 7, 25, 9, 0, 0, TimeSpan.FromHours(-5));
        var cierre = apertura.AddHours(8);
        var sesionId = Guid.NewGuid();
        var sesion = new SesionCaja(
            sesionId,
            EmpresaId,
            SedeId,
            PuntoVentaId,
            100m,
            fechaApertura: apertura);
        sesion.Cerrar(175m, cierre);
        await using var factory = new CapitalPosHttpFactory();
        factory.SesionCajaRepository.Sesiones.Add(sesion);
        await factory.VentaRepository.AgregarAsync(CrearVentaReporte(
            EmpresaId,
            CanalVenta.TIENDA,
            apertura.AddHours(1),
            [(1m, 50m)],
            MetodoPago.YAPE,
            apertura.AddHours(1)));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/caja/sesiones/{sesionId}/resumen");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ResumenSesionCajaResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(sesionId, body.SesionCajaId);
        Assert.Equal("Cerrada", body.Estado);
        Assert.Equal(50m, body.TotalVentas);
        Assert.Equal(1, body.CantidadVentas);
        Assert.Equal(50m, body.TotalPagado);
        Assert.Equal(25m, body.DiferenciaOperativa);
        var yape = Assert.Single(
            body.PagosPorMetodo,
            item => item.MetodoPago == "YAPE");
        Assert.Equal(50m, yape.Total);
        Assert.Equal(1, yape.CantidadPagos);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Resumen_caja_ajena_devuelve_not_found()
    {
        var sesionAjenaId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        factory.SesionCajaRepository.Sesiones.Add(new SesionCaja(
            sesionAjenaId,
            otraEmpresaId,
            SedeId,
            PuntoVentaId,
            100m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/caja/sesiones/{sesionAjenaId}/resumen");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            SedeId,
            productoId,
            null,
            12m,
            2m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/stock/productos/{productoId}?sedeId={SedeId}");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<StockProductoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(SedeId, body.SedeId);
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
            SedeId,
            productoId,
            varianteId,
            8m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/stock/productos/{productoId}/variantes/{varianteId}?sedeId={SedeId}");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<StockProductoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(SedeId, body.SedeId);
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
        var request = new AjustarStockProductoRequest(SedeId, productoId, null, 15m);

        var response = await client.PutAsJsonAsync("/api/stock/ajustar", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<StockProductoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(SedeId, body.SedeId);
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
            SedeId,
            productoId,
            null,
            99m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/stock/productos/{productoId}?sedeId={SedeId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("99", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Obtener_stock_con_sede_de_otra_empresa_devuelve_bad_request()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var otraSedeId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        factory.SedeRepository.Sedes.Add(new Sede(
            otraSedeId,
            otraEmpresaId,
            "Sede externa",
            TipoSede.TIENDA));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/stock/productos/{productoId}?sedeId={otraSedeId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("sede", content, StringComparison.OrdinalIgnoreCase);
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
        var request = new AjustarStockProductoRequest(SedeId, productoId, null, 15m);

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
        var request = new AjustarStockProductoRequest(SedeId, Guid.NewGuid(), null, -1m);

        var response = await client.PutAsJsonAsync("/api/stock/ajustar", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("cantidad", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.StockRepository.Stocks);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Unidades_medida_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/unidades-medida");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unidades_medida_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync("/api/unidades-medida");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Unidades_medida_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/unidades-medida");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_unidades_medida_devuelve_solo_activas()
    {
        var unidadActivaId = Guid.NewGuid();
        var unidadInactivaId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(unidadActivaId, "UND", "Unidad"));
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(unidadInactivaId, "CAJ", "Caja", activa: false));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync("/api/unidades-medida");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<UnidadMedidaResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var unidad = Assert.Single(body);
        Assert.Equal(unidadActivaId, unidad.Id);
        Assert.Equal("UND", unidad.Codigo);
        Assert.Equal("Unidad", unidad.Nombre);
        Assert.True(unidad.Activa);
        Assert.DoesNotContain(unidadInactivaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Producto_presentaciones_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/presentaciones");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Producto_presentaciones_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CrearAuthorizationHeader(UsuarioId);

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/presentaciones");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(EmpresaActivaHeaders.HeaderName, content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Producto_presentaciones_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/presentaciones");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_presentaciones_devuelve_solo_activas_del_producto_y_empresa_activa()
    {
        var productoId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var presentacionActivaId = Guid.NewGuid();
        var presentacionInactivaId = Guid.NewGuid();
        var presentacionOtroProductoId = Guid.NewGuid();
        var presentacionOtraEmpresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, otroProductoId));
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad"));
        await factory.ProductoPresentacionRepository.AgregarAsync(new ProductoPresentacion(
            presentacionActivaId,
            EmpresaId,
            productoId,
            unidadId,
            1m,
            true,
            10m));
        await factory.ProductoPresentacionRepository.AgregarAsync(new ProductoPresentacion(
            presentacionInactivaId,
            EmpresaId,
            productoId,
            unidadId,
            12m,
            false,
            100m,
            activa: false));
        await factory.ProductoPresentacionRepository.AgregarAsync(new ProductoPresentacion(
            presentacionOtroProductoId,
            EmpresaId,
            otroProductoId,
            unidadId,
            1m,
            true,
            11m));
        await factory.ProductoPresentacionRepository.AgregarAsync(new ProductoPresentacion(
            presentacionOtraEmpresaId,
            otraEmpresaId,
            productoId,
            unidadId,
            1m,
            true,
            99m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/productos/{productoId}/presentaciones");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ProductoPresentacionResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var presentacion = Assert.Single(body);
        Assert.Equal(presentacionActivaId, presentacion.Id);
        Assert.Equal(EmpresaId, presentacion.EmpresaId);
        Assert.Equal(productoId, presentacion.ProductoId);
        Assert.Null(presentacion.ProductoVarianteId);
        Assert.Equal(unidadId, presentacion.UnidadMedidaId);
        Assert.Equal("UND", presentacion.UnidadCodigo);
        Assert.Equal("Unidad", presentacion.UnidadNombre);
        Assert.DoesNotContain(presentacionInactivaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(presentacionOtroProductoId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(presentacionOtraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_presentaciones_de_producto_de_otra_empresa_devuelve_not_found()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/productos/{productoId}/presentaciones");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Crear_presentacion_guarda_en_empresa_activa_e_ignora_empresa_id_libre()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(unidadId, "CAJ", "Caja"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new
        {
            EmpresaId = otraEmpresaId,
            ProductoId = Guid.NewGuid(),
            UnidadMedidaId = unidadId,
            FactorConversion = 12m,
            EsUnidadBase = false,
            PrecioVenta = 100m,
            CodigoBarras = " 7750000000104 "
        };

        var response = await client.PostAsJsonAsync($"/api/productos/{productoId}/presentaciones", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ProductoPresentacionResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(productoId, body.ProductoId);
        Assert.Equal(unidadId, body.UnidadMedidaId);
        Assert.Equal("CAJ", body.UnidadCodigo);
        Assert.Equal("Caja", body.UnidadNombre);
        Assert.Equal(12m, body.FactorConversion);
        Assert.False(body.EsUnidadBase);
        Assert.Equal(100m, body.PrecioVenta);
        Assert.Equal("7750000000104", body.CodigoBarras);
        Assert.DoesNotContain(otraEmpresaId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(factory.ProductoPresentacionRepository.Presentaciones, presentacion =>
            presentacion.EmpresaId == EmpresaId &&
            presentacion.ProductoId == productoId &&
            presentacion.CodigoBarras == "7750000000104");
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_presentacion_falla_si_producto_no_pertenece_a_empresa_activa()
    {
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearProductoPresentacionRequest(productoId, unidadId, 1m, true, 10m);

        var response = await client.PostAsJsonAsync($"/api/productos/{productoId}/presentaciones", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("producto", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.ProductoPresentacionRepository.Presentaciones);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_presentacion_falla_si_unidad_no_existe_o_inactiva()
    {
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad", activa: false));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearProductoPresentacionRequest(productoId, unidadId, 1m, true, 10m);

        var response = await client.PostAsJsonAsync($"/api/productos/{productoId}/presentaciones", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("unidad", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.ProductoPresentacionRepository.Presentaciones);
        AssertSeguro(content);
    }

    [Theory]
    [InlineData(0, 10, "factor")]
    [InlineData(-1, 10, "factor")]
    [InlineData(1, 0, "precio")]
    [InlineData(1, -1, "precio")]
    public async Task Crear_presentacion_valida_factor_y_precio(
        decimal factorConversion,
        decimal precioVenta,
        string errorEsperado)
    {
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearProductoPresentacionRequest(productoId, unidadId, factorConversion, true, precioVenta);

        var response = await client.PostAsJsonAsync($"/api/productos/{productoId}/presentaciones", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(errorEsperado, content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.ProductoPresentacionRepository.Presentaciones);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_presentacion_rechaza_codigo_barras_duplicado_por_empresa()
    {
        var productoId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(unidadId, "UND", "Unidad"));
        await factory.ProductoPresentacionRepository.AgregarAsync(new ProductoPresentacion(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            unidadId,
            1m,
            true,
            10m,
            "7750000000104"));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearProductoPresentacionRequest(
            productoId,
            unidadId,
            2m,
            false,
            20m,
            "7750000000104");

        var response = await client.PostAsJsonAsync($"/api/productos/{productoId}/presentaciones", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("codigo de barras", content, StringComparison.OrdinalIgnoreCase);
        Assert.Single(factory.ProductoPresentacionRepository.Presentaciones);
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
    public async Task Precios_mayoristas_sin_jwt_devuelve_unauthorized()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/precios-mayoristas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Precios_mayoristas_con_jwt_sin_empresa_activa_devuelve_bad_request()
    {
        await using var factory = new CapitalPosHttpFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CrearJwt(UsuarioId));

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/precios-mayoristas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("X-CapitalPos-EmpresaId", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Precios_mayoristas_usuario_sin_permiso_devuelve_forbidden()
    {
        await using var factory = new CapitalPosHttpFactory
        {
            UsuarioEmpresa = CrearUsuarioEmpresa(RolEmpresa.Vendedor)
        };
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/productos/{Guid.NewGuid()}/precios-mayoristas");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permiso requerido", content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Listar_precios_mayoristas_devuelve_solo_producto_y_empresa_activa()
    {
        var productoId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            24,
            30m,
            activa: false));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            12,
            35m));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            Guid.NewGuid(),
            EmpresaId,
            otroProductoId,
            12,
            35m));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            Guid.NewGuid(),
            otraEmpresaId,
            productoId,
            12,
            35m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/productos/{productoId}/precios-mayoristas");
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ReglaPrecioMayoristaResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal([12, 24], body.Select(regla => regla.CantidadMinima));
        Assert.Contains(body, regla => regla.Activa);
        Assert.Contains(body, regla => !regla.Activa);
        Assert.All(body, regla =>
        {
            Assert.Equal(EmpresaId, regla.EmpresaId);
            Assert.Equal(productoId, regla.ProductoId);
        });
        Assert.DoesNotContain(otraEmpresaId.ToString(), content);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_precio_mayorista_funciona_con_empresa_activa_e_ignora_empresa_id_libre()
    {
        var productoId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new
        {
            empresaId = otraEmpresaId,
            cantidadMinima = 12,
            precioUnitarioMayorista = 35m
        };

        var response = await client.PostAsJsonAsync($"/api/productos/{productoId}/precios-mayoristas", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<ReglaPrecioMayoristaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(EmpresaId, body.EmpresaId);
        Assert.Equal(productoId, body.ProductoId);
        Assert.Equal(12, body.CantidadMinima);
        Assert.Equal(35m, body.PrecioUnitarioMayorista);
        Assert.True(body.Activa);
        Assert.DoesNotContain(otraEmpresaId.ToString(), content);
        Assert.Contains(factory.ReglaPrecioMayoristaRepository.Reglas, regla =>
            regla.EmpresaId == EmpresaId &&
            regla.ProductoId == productoId &&
            regla.CantidadMinima == 12);
        AssertSeguro(content);
    }

    [Theory]
    [InlineData(0, 35)]
    [InlineData(12, 0)]
    public async Task Crear_precio_mayorista_valida_cantidad_y_precio(int cantidadMinima, decimal precio)
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync(
            $"/api/productos/{productoId}/precios-mayoristas",
            new CrearReglaPrecioMayoristaRequest(productoId, cantidadMinima, precio));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.ReglaPrecioMayoristaRepository.Reglas);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_precio_mayorista_falla_si_producto_es_de_otra_empresa_o_duplica_activa()
    {
        var productoId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(otraEmpresaId, productoId));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var otraEmpresa = await client.PostAsJsonAsync(
            $"/api/productos/{productoId}/precios-mayoristas",
            new CrearReglaPrecioMayoristaRequest(productoId, 12, 35m));
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            12,
            35m));
        var duplicada = await client.PostAsJsonAsync(
            $"/api/productos/{productoId}/precios-mayoristas",
            new CrearReglaPrecioMayoristaRequest(productoId, 12, 30m));

        Assert.Equal(HttpStatusCode.BadRequest, otraEmpresa.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicada.StatusCode);
        Assert.Single(factory.ReglaPrecioMayoristaRepository.Reglas);
    }

    [Fact]
    public async Task Activar_y_desactivar_precio_mayorista_validan_producto_y_empresa()
    {
        var productoId = Guid.NewGuid();
        var reglaId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        var regla = new ReglaPrecioMayorista(
            reglaId,
            EmpresaId,
            productoId,
            12,
            35m,
            activa: false);
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(regla);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var activar = await client.PatchAsync(
            $"/api/productos/{productoId}/precios-mayoristas/{reglaId}/activar",
            null);
        var activarBody = await activar.Content.ReadFromJsonAsync<ReglaPrecioMayoristaResponse>();
        var desactivar = await client.PatchAsync(
            $"/api/productos/{productoId}/precios-mayoristas/{reglaId}/desactivar",
            null);
        var desactivarBody = await desactivar.Content.ReadFromJsonAsync<ReglaPrecioMayoristaResponse>();

        Assert.Equal(HttpStatusCode.OK, activar.StatusCode);
        Assert.NotNull(activarBody);
        Assert.True(activarBody.Activa);
        Assert.Equal(HttpStatusCode.OK, desactivar.StatusCode);
        Assert.NotNull(desactivarBody);
        Assert.False(desactivarBody.Activa);
    }

    [Fact]
    public async Task Activar_precio_mayorista_falla_si_genera_duplicado_activo()
    {
        var productoId = Guid.NewGuid();
        var reglaInactivaId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            12,
            35m));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            reglaInactivaId,
            EmpresaId,
            productoId,
            12,
            30m,
            activa: false));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PatchAsync(
            $"/api/productos/{productoId}/precios-mayoristas/{reglaInactivaId}/activar",
            null);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("duplicada", content, StringComparison.OrdinalIgnoreCase);
        Assert.False(factory.ReglaPrecioMayoristaRepository.Reglas.Single(regla => regla.Id == reglaInactivaId).Activa);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Patch_precio_mayorista_falla_si_regla_es_de_otra_empresa_u_otro_producto()
    {
        var productoId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var reglaOtroProductoId = Guid.NewGuid();
        var reglaOtraEmpresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            reglaOtroProductoId,
            EmpresaId,
            otroProductoId,
            12,
            35m));
        await factory.ReglaPrecioMayoristaRepository.AgregarAsync(new ReglaPrecioMayorista(
            reglaOtraEmpresaId,
            otraEmpresaId,
            productoId,
            12,
            35m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var otroProducto = await client.PatchAsync(
            $"/api/productos/{productoId}/precios-mayoristas/{reglaOtroProductoId}/desactivar",
            null);
        var otraEmpresa = await client.PatchAsync(
            $"/api/productos/{productoId}/precios-mayoristas/{reglaOtraEmpresaId}/desactivar",
            null);

        Assert.Equal(HttpStatusCode.NotFound, otroProducto.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otraEmpresa.StatusCode);
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
            SedeId,
            productoId,
            null,
            5m));
        AgregarCajaAbierta(factory);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(productoId, null, 2m));
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<VentaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("TIENDA", body.CanalVenta);
        Assert.Equal(SedeId, body.SedeId);
        Assert.Equal(PuntoVentaId, body.PuntoVentaId);
        Assert.Null(body.VendedorId);
        Assert.Single(factory.VentaRepository.Ventas);
        Assert.Equal(3m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        var pago = Assert.Single(body.Pagos);
        Assert.Equal("EFECTIVO", pago.MetodoPago);
        Assert.Equal(body.Total, pago.Monto);
        AssertSeguro(content);
    }

    [Theory]
    [InlineData("EFECTIVO", null)]
    [InlineData("YAPE", "YAPE-001")]
    [InlineData("TARJETA", "VISA-001")]
    public async Task Crear_venta_registra_pago_manual(
        string metodoPago,
        string? codigoOperacion)
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            null,
            5m));
        AgregarCajaAbierta(factory);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = CrearVentaRequest(
            productoId,
            null,
            2m,
            pagos:
            [
                new CrearVentaPagoRequest(
                    metodoPago,
                    20m,
                    codigoOperacion,
                    "Cobro manual")
            ]);

        var response = await client.PostAsJsonAsync("/api/ventas/", request);
        var body = await response.Content.ReadFromJsonAsync<VentaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        var pago = Assert.Single(body.Pagos);
        Assert.Equal(metodoPago, pago.MetodoPago);
        Assert.Equal(20m, pago.Monto);
        Assert.Equal(codigoOperacion, pago.CodigoOperacion);
        Assert.Equal("Cobro manual", pago.Observacion);
        Assert.Equal(3m, factory.StockRepository.Stocks.Single().CantidadDisponible);
    }

    [Fact]
    public async Task Crear_venta_acepta_pago_mixto_efectivo_y_yape()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            null,
            5m));
        AgregarCajaAbierta(factory);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = CrearVentaRequest(
            productoId,
            null,
            2m,
            pagos:
            [
                new CrearVentaPagoRequest("EFECTIVO", 8m),
                new CrearVentaPagoRequest("YAPE", 12m, "YAPE-MIXTO")
            ]);

        var response = await client.PostAsJsonAsync("/api/ventas/", request);
        var body = await response.Content.ReadFromJsonAsync<VentaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Collection(
            body.Pagos.OrderBy(pago => pago.MetodoPago),
            pago =>
            {
                Assert.Equal("EFECTIVO", pago.MetodoPago);
                Assert.Equal(8m, pago.Monto);
            },
            pago =>
            {
                Assert.Equal("YAPE", pago.MetodoPago);
                Assert.Equal(12m, pago.Monto);
            });
    }

    [Theory]
    [InlineData("EFECTIVO", 19)]
    [InlineData("EFECTIVO", 0)]
    [InlineData("YAPE", -1)]
    [InlineData("CRIPTOMONEDA", 20)]
    public async Task Crear_venta_rechaza_pago_invalido_sin_descontar_stock(
        string metodoPago,
        decimal monto)
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            null,
            5m));
        AgregarCajaAbierta(factory);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = CrearVentaRequest(
            productoId,
            null,
            2m,
            pagos: [new CrearVentaPagoRequest(metodoPago, monto)]);

        var response = await client.PostAsJsonAsync("/api/ventas/", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.VentaRepository.Ventas);
        Assert.Equal(5m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        Assert.Contains(
            metodoPago == "CRIPTOMONEDA"
                ? "metodo de pago no es valido"
                : monto <= 0
                    ? "monto del pago debe ser mayor que cero"
                    : "suma de los pagos debe ser igual al total",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Historial_ventas_filtra_y_resume_solo_empresa_activa()
    {
        var ventaTienda = CrearVentaReporte(
            EmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 25, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(2m, 40m), (1m, 20m)],
            MetodoPago.EFECTIVO);
        var ventaWeb = CrearVentaReporte(
            EmpresaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 7, 25, 11, 0, 0, TimeSpan.FromHours(-5)),
            [(4m, 80m)]);
        var ventaOtraEmpresa = CrearVentaReporte(
            Guid.NewGuid(),
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.FromHours(-5)),
            [(9m, 90m)]);
        await using var factory = new CapitalPosHttpFactory();
        await factory.VentaRepository.AgregarAsync(ventaTienda);
        await factory.VentaRepository.AgregarAsync(ventaWeb);
        await factory.VentaRepository.AgregarAsync(ventaOtraEmpresa);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync(
            $"/api/ventas?desde=2026-07-25&hasta=2026-07-25&canalVenta=TIENDA&sedeId={SedeId}&puntoVentaId={PuntoVentaId}");
        var body = await response.Content.ReadFromJsonAsync<VentaResumenResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(Assert.IsType<VentaResumenResponse[]>(body));
        Assert.Equal(ventaTienda.Id, item.Id);
        Assert.Equal(EmpresaId, item.EmpresaId);
        Assert.Equal(2, item.CantidadItems);
        Assert.Equal(3m, item.UnidadesComerciales);
        Assert.Equal(60m, item.Total);
        var pago = Assert.Single(item.Pagos);
        Assert.Equal("EFECTIVO", pago.MetodoPago);
        Assert.Equal(60m, pago.Monto);
    }

    [Fact]
    public async Task Detalle_venta_devuelve_lineas_y_oculta_venta_de_otra_empresa()
    {
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            EmpresaId,
            ventaId,
            productoId,
            3m,
            25m,
            0m,
            75m,
            varianteId,
            factorConversionAplicado: 1m,
            cantidadBaseDescontada: 3m,
            precioMayoristaAplicado: true);
        var venta = new Venta(
            ventaId,
            EmpresaId,
            new DateTimeOffset(2026, 7, 25, 10, 0, 0, TimeSpan.FromHours(-5)),
            75m,
            0m,
            75m,
            [detalle],
            SedeId,
            PuntoVentaId,
            pagos:
            [
                new VentaPago(
                    Guid.NewGuid(),
                    EmpresaId,
                    ventaId,
                    MetodoPago.YAPE,
                    75m,
                    "YAPE-HISTORIAL",
                    "Pago mostrado en detalle")
            ]);
        var otraVenta = CrearVentaReporte(
            Guid.NewGuid(),
            CanalVenta.TIENDA,
            DateTimeOffset.UtcNow,
            [(1m, 10m)]);
        await using var factory = new CapitalPosHttpFactory();
        factory.ProductoRepository.Productos.Add(
            new Producto(productoId, EmpresaId, "Polo Brooklyn", 35m));
        factory.ProductoVarianteRepository.Variantes.Add(
            new ProductoVariante(varianteId, EmpresaId, productoId, "M", "Negro"));
        await factory.VentaRepository.AgregarAsync(venta);
        await factory.VentaRepository.AgregarAsync(otraVenta);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.GetAsync($"/api/ventas/{venta.Id}");
        var body = await response.Content.ReadFromJsonAsync<VentaDetalleCompletoResponse>();
        var responseOtraEmpresa = await client.GetAsync($"/api/ventas/{otraVenta.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var linea = Assert.Single(body.Detalles);
        Assert.Equal(varianteId, linea.ProductoVarianteId);
        Assert.Equal("Polo Brooklyn - M / Negro", linea.Descripcion);
        Assert.True(linea.PrecioMayoristaAplicado);
        Assert.Equal(3m, linea.CantidadBaseDescontada);
        var pago = Assert.Single(body.Pagos);
        Assert.Equal("YAPE", pago.MetodoPago);
        Assert.Equal(75m, pago.Monto);
        Assert.Equal("YAPE-HISTORIAL", pago.CodigoOperacion);
        Assert.Equal("Pago mostrado en detalle", pago.Observacion);
        Assert.Equal(HttpStatusCode.NotFound, responseOtraEmpresa.StatusCode);
    }

    [Fact]
    public async Task Crear_venta_aplica_precio_mayorista_y_devuelve_snapshot()
    {
        var productoId = Guid.NewGuid();
        var varianteSId = Guid.NewGuid();
        var varianteMId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.ProductoVarianteRepository.AgregarAsync(CrearVariante(EmpresaId, productoId, varianteSId));
        await factory.ProductoVarianteRepository.AgregarAsync(new ProductoVariante(
            varianteMId,
            EmpresaId,
            productoId,
            talla: "M",
            color: "Azul"));
        factory.ReglaPrecioMayoristaRepository.Reglas.Add(new ReglaPrecioMayorista(
            Guid.NewGuid(),
            EmpresaId,
            productoId,
            12,
            35m));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            varianteSId,
            10m));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            varianteMId,
            10m));
        AgregarCajaAbierta(factory);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [
                new CrearVentaDetalleRequest(productoId, varianteSId, 7m, 59m, 63m, 413m),
                new CrearVentaDetalleRequest(productoId, varianteMId, 5m, 59m, 45m, 295m)
            ],
            PuntoVentaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", request);
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<VentaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(2, body.Detalles.Count);
        Assert.All(body.Detalles, detalle =>
        {
            Assert.True(detalle.PrecioMayoristaAplicado);
            Assert.Equal(35m, detalle.PrecioUnitario);
        });
        var pago = Assert.Single(body.Pagos);
        Assert.Equal("EFECTIVO", pago.MetodoPago);
        Assert.Equal(body.Total, pago.Monto);
        Assert.Equal(420m, body.Total);
        Assert.Equal(3m, factory.StockRepository.Stocks.Single(stock => stock.ProductoVarianteId == varianteSId).CantidadDisponible);
        Assert.Equal(5m, factory.StockRepository.Stocks.Single(stock => stock.ProductoVarianteId == varianteMId).CantidadDisponible);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_venta_sin_caja_abierta_devuelve_bad_request_y_no_descuenta_stock()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            null,
            5m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(productoId, null, 2m));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("sesion de caja", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.VentaRepository.Ventas);
        Assert.Equal(5m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_venta_con_presentacion_descuenta_stock_por_factor_y_devuelve_presentacion()
    {
        var productoId = Guid.NewGuid();
        var unidadMedidaId = Guid.NewGuid();
        var presentacionId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.UnidadMedidaRepository.AgregarAsync(new UnidadMedida(
            unidadMedidaId,
            "CAJ",
            "Caja"));
        await factory.ProductoPresentacionRepository.AgregarAsync(new ProductoPresentacion(
            presentacionId,
            EmpresaId,
            productoId,
            unidadMedidaId,
            12m,
            false,
            118m));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            null,
            30m));
        AgregarCajaAbierta(factory);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync(
            "/api/ventas/",
            CrearVentaRequest(productoId, null, 2m, productoPresentacionId: presentacionId));
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<VentaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        var detalle = Assert.Single(body.Detalles);
        Assert.Equal(presentacionId, detalle.ProductoPresentacionId);
        Assert.Equal(12m, detalle.FactorConversionAplicado);
        Assert.Equal(24m, detalle.CantidadBaseDescontada);
        Assert.Equal(118m, detalle.PrecioUnitario);
        Assert.Equal(36m, detalle.Igv);
        Assert.Equal(236m, detalle.Total);
        Assert.Equal(200m, body.Subtotal);
        Assert.Single(factory.VentaRepository.Ventas);
        Assert.Equal(6m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        AssertSeguro(content);
    }

    [Theory]
    [InlineData("PROVINCIA")]
    [InlineData("MARKETING")]
    public async Task Crear_venta_devuelve_dimensiones_comerciales(string canalVenta)
    {
        var productoId = Guid.NewGuid();
        var vendedorId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            null,
            5m));
        AgregarCajaAbierta(factory);
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(
            productoId,
            null,
            1m,
            canalVenta,
            PuntoVentaId,
            vendedorId));
        var content = await response.Content.ReadAsStringAsync();
        var body = await response.Content.ReadFromJsonAsync<VentaResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(canalVenta, body.CanalVenta);
        Assert.Equal(SedeId, body.SedeId);
        Assert.Equal(PuntoVentaId, body.PuntoVentaId);
        Assert.Equal(vendedorId, body.VendedorId);
        var venta = Assert.Single(factory.VentaRepository.Ventas);
        Assert.Equal(Enum.Parse<CanalVenta>(canalVenta), venta.CanalVenta);
        Assert.Equal(SedeId, venta.SedeId);
        Assert.Equal(PuntoVentaId, venta.PuntoVentaId);
        Assert.Equal(vendedorId, venta.VendedorId);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_venta_con_canal_invalido_devuelve_bad_request()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            null,
            5m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(
            productoId,
            null,
            1m,
            "ONLINE"));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("canal de venta", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.VentaRepository.Ventas);
        Assert.Equal(5m, factory.StockRepository.Stocks.Single().CantidadDisponible);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_venta_sin_punto_venta_devuelve_bad_request()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(
            productoId,
            null,
            1m,
            puntoVentaId: Guid.Empty));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("punto de venta", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.VentaRepository.Ventas);
        AssertSeguro(content);
    }

    [Fact]
    public async Task Crear_venta_con_punto_venta_inexistente_devuelve_bad_request()
    {
        var productoId = Guid.NewGuid();
        await using var factory = new CapitalPosHttpFactory();
        await factory.ProductoRepository.AgregarAsync(CrearProducto(EmpresaId, productoId));
        await factory.StockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            productoId,
            null,
            5m));
        using var client = CrearClienteAutenticado(factory, UsuarioId, EmpresaId);

        var response = await client.PostAsJsonAsync("/api/ventas/", CrearVentaRequest(
            productoId,
            null,
            1m,
            puntoVentaId: Guid.NewGuid()));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("punto de venta", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.VentaRepository.Ventas);
        Assert.Equal(5m, factory.StockRepository.Stocks.Single().CantidadDisponible);
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
            SedeId,
            productoId,
            null,
            1m));
        AgregarCajaAbierta(factory);
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
            SedeId,
            productoId,
            null,
            5m));
        AgregarCajaAbierta(factory);
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
            SedeId,
            productoId,
            varianteId,
            5m));
        AgregarCajaAbierta(factory);
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
        decimal cantidad,
        string? canalVenta = null,
        Guid? puntoVentaId = null,
        Guid? vendedorId = null,
        Guid? productoPresentacionId = null,
        IReadOnlyCollection<CrearVentaPagoRequest>? pagos = null)
    {
        return new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(
                productoId,
                productoVarianteId,
                cantidad,
                10m,
                0m,
                cantidad * 10m,
                productoPresentacionId)],
            puntoVentaId ?? PuntoVentaId,
            canalVenta,
            vendedorId,
            pagos);
    }

    private static void AgregarCajaAbierta(CapitalPosHttpFactory factory)
    {
        factory.SesionCajaRepository.Sesiones.Add(new SesionCaja(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            PuntoVentaId,
            100m));
    }

    private static Venta CrearVentaReporte(
        Guid empresaId,
        CanalVenta canalVenta,
        DateTimeOffset fecha,
        IReadOnlyCollection<(decimal Cantidad, decimal Total)> detalles,
        MetodoPago? metodoPago = null,
        DateTimeOffset? fechaCreacion = null,
        Guid? puntoVentaId = null,
        EstadoVenta estado = EstadoVenta.Registrada,
        Guid? sedeId = null,
        Guid? vendedorId = null)
    {
        var ventaId = Guid.NewGuid();
        var ventaDetalles = detalles
            .Select(detalle => new VentaDetalle(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                Guid.NewGuid(),
                detalle.Cantidad,
                detalle.Total / detalle.Cantidad,
                0m,
                detalle.Total))
            .ToArray();
        var total = ventaDetalles.Sum(detalle => detalle.Total);
        IReadOnlyCollection<VentaPago>? pagos = metodoPago.HasValue
            ? [new VentaPago(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                metodoPago.Value,
                total)]
            : null;

        return new Venta(
            ventaId,
            empresaId,
            fecha,
            total,
            0m,
            total,
            ventaDetalles,
            sedeId ?? SedeId,
            puntoVentaId ?? PuntoVentaId,
            canalVenta: canalVenta,
            vendedorId: vendedorId,
            estado: estado,
            fechaCreacion: fechaCreacion,
            pagos: pagos);
    }

    private static Venta CrearVentaDashboard(
        Guid empresaId,
        CanalVenta canalVenta,
        DateTimeOffset fecha,
        IReadOnlyCollection<(Guid ProductoId, Guid? ProductoVarianteId, decimal Cantidad, decimal Total)> detalles)
    {
        var ventaId = Guid.NewGuid();
        var ventaDetalles = detalles
            .Select(detalle => new VentaDetalle(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                detalle.ProductoId,
                detalle.Cantidad,
                detalle.Total / detalle.Cantidad,
                0m,
                detalle.Total,
                detalle.ProductoVarianteId))
            .ToArray();
        var total = ventaDetalles.Sum(detalle => detalle.Total);

        return new Venta(
            ventaId,
            empresaId,
            fecha,
            total,
            0m,
            total,
            ventaDetalles,
            SedeId,
            PuntoVentaId,
            canalVenta: canalVenta);
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

        public FakeCategoriaRepository CategoriaRepository { get; } = new();

        public FakeMarcaRepository MarcaRepository { get; } = new();

        public FakeUnidadMedidaRepository UnidadMedidaRepository { get; } = new();

        public FakeProductoRepository ProductoRepository { get; } = new();

        public FakeProductoPresentacionRepository ProductoPresentacionRepository { get; } = new();

        public FakeProductoVarianteRepository ProductoVarianteRepository { get; } = new();

        public FakeReglaPrecioMayoristaRepository ReglaPrecioMayoristaRepository { get; } = new();

        public FakeStockProductoRepository StockRepository { get; } = new();

        public FakeSedeRepository SedeRepository { get; } = new();

        public FakePuntoVentaRepository PuntoVentaRepository { get; } = new();

        public FakeSerieComprobanteRepository SerieComprobanteRepository { get; } = new();

        public FakeSesionCajaRepository SesionCajaRepository { get; } = new();

        public FakeVentaRepository VentaRepository { get; } = new();

        public FakeCompraRepository CompraRepository { get; } = new();

        public IDashboardComercialClock DashboardClock { get; set; } =
            new FakeDashboardComercialClock(new DateTimeOffset(2026, 7, 17, 15, 42, 10, TimeSpan.FromHours(-5)));

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
                services.RemoveAll<ICategoriaRepository>();
                services.RemoveAll<IMarcaRepository>();
                services.RemoveAll<IUnidadMedidaRepository>();
                services.RemoveAll<IProductoRepository>();
                services.RemoveAll<IProductoPresentacionRepository>();
                services.RemoveAll<IProductoVarianteRepository>();
                services.RemoveAll<IReglaPrecioMayoristaRepository>();
                services.RemoveAll<IClienteRepository>();
                services.RemoveAll<ICompraRepository>();
                services.RemoveAll<IVentaRepository>();
                services.RemoveAll<IComprobanteRepository>();
                services.RemoveAll<IConfiguracionFiscalEmpresaRepository>();
                services.RemoveAll<IStockProductoRepository>();
                services.RemoveAll<ISedeRepository>();
                services.RemoveAll<IPuntoVentaRepository>();
                services.RemoveAll<ISerieComprobanteRepository>();
                services.RemoveAll<ISesionCajaRepository>();
                services.RemoveAll<IPedidoDigitalRepository>();
                services.RemoveAll<IMovimientoInventarioRepository>();
                services.RemoveAll<IUnitOfWork>();
                services.RemoveAll<IDashboardComercialClock>();

                services.AddSingleton<IEmpresaRepository>(EmpresaRepository);
                services.AddSingleton<IUsuarioRepository>(UsuarioRepository);
                services.AddSingleton<IUsuarioEmpresaRepository>(UsuarioEmpresaRepository);
                services.AddSingleton<IUsuarioCredencialRepository, FakeUsuarioCredencialRepository>();
                services.AddSingleton<ICategoriaRepository>(CategoriaRepository);
                services.AddSingleton<IMarcaRepository>(MarcaRepository);
                services.AddSingleton<IUnidadMedidaRepository>(UnidadMedidaRepository);
                services.AddSingleton<IProductoRepository>(ProductoRepository);
                services.AddSingleton<IProductoPresentacionRepository>(ProductoPresentacionRepository);
                services.AddSingleton<IProductoVarianteRepository>(ProductoVarianteRepository);
                services.AddSingleton<IReglaPrecioMayoristaRepository>(ReglaPrecioMayoristaRepository);
                services.AddSingleton<IClienteRepository, FakeClienteRepository>();
                services.AddSingleton<ICompraRepository>(CompraRepository);
                services.AddSingleton<IVentaRepository>(VentaRepository);
                services.AddSingleton<IComprobanteRepository, FakeComprobanteRepository>();
                services.AddSingleton<IConfiguracionFiscalEmpresaRepository>(ConfiguracionFiscalRepository);
                services.AddSingleton<IStockProductoRepository>(StockRepository);
                services.AddSingleton<ISedeRepository>(SedeRepository);
                services.AddSingleton<IPuntoVentaRepository>(PuntoVentaRepository);
                services.AddSingleton<ISerieComprobanteRepository>(SerieComprobanteRepository);
                services.AddSingleton<ISesionCajaRepository>(SesionCajaRepository);
                services.AddSingleton<IPedidoDigitalRepository, FakePedidoDigitalRepository>();
                services.AddSingleton<IMovimientoInventarioRepository, FakeMovimientoInventarioRepository>();
                services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
                services.AddSingleton(DashboardClock);
            });
        }
    }

    private sealed class FakeDashboardComercialClock : IDashboardComercialClock
    {
        private readonly DateTimeOffset _ahoraLima;

        public FakeDashboardComercialClock(DateTimeOffset ahoraLima)
        {
            _ahoraLima = ahoraLima;
        }

        public DateTimeOffset AhoraLima() => _ahoraLima;
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

    private sealed class FakeCategoriaRepository : ICategoriaRepository
    {
        public List<Categoria> Categorias { get; } = [];

        public Task AgregarAsync(Categoria categoria, CancellationToken cancellationToken = default)
        {
            Categorias.Add(categoria);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Categoria>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Categoria>>(
                Categorias.Where(categoria => categoria.EmpresaId == empresaId).ToArray());
        }

        public Task<Categoria?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Categorias.FirstOrDefault(categoria =>
                categoria.EmpresaId == empresaId &&
                categoria.Id == id));
        }

        public Task<bool> ExisteNombreAsync(
            Guid empresaId,
            string nombre,
            CancellationToken cancellationToken = default)
        {
            var nombreNormalizado = nombre.Trim();

            return Task.FromResult(Categorias.Any(categoria =>
                categoria.EmpresaId == empresaId &&
                categoria.Nombre == nombreNormalizado));
        }
    }

    private sealed class FakeMarcaRepository : IMarcaRepository
    {
        public List<Marca> Marcas { get; } = [];

        public Task AgregarAsync(Marca marca, CancellationToken cancellationToken = default)
        {
            Marcas.Add(marca);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Marca>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Marca>>(
                Marcas.Where(marca => marca.EmpresaId == empresaId).ToArray());
        }

        public Task<Marca?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Marcas.FirstOrDefault(marca =>
                marca.EmpresaId == empresaId &&
                marca.Id == id));
        }

        public Task<bool> ExisteNombreAsync(
            Guid empresaId,
            string nombre,
            CancellationToken cancellationToken = default)
        {
            var nombreNormalizado = nombre.Trim();

            return Task.FromResult(Marcas.Any(marca =>
                marca.EmpresaId == empresaId &&
                marca.Nombre == nombreNormalizado));
        }
    }

    private sealed class FakeUnidadMedidaRepository : IUnidadMedidaRepository
    {
        public List<UnidadMedida> UnidadesMedida { get; } = [];

        public Task AgregarAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken = default)
        {
            UnidadesMedida.Add(unidadMedida);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<UnidadMedida>> ListarAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<UnidadMedida>>(
                UnidadesMedida.OrderBy(unidadMedida => unidadMedida.Codigo).ToArray());
        }

        public Task<UnidadMedida?> ObtenerPorIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UnidadesMedida.FirstOrDefault(unidadMedida => unidadMedida.Id == id));
        }

        public Task<UnidadMedida?> ObtenerPorCodigoAsync(
            string codigo,
            CancellationToken cancellationToken = default)
        {
            var codigoNormalizado = codigo.Trim().ToUpperInvariant();

            return Task.FromResult(UnidadesMedida.FirstOrDefault(unidadMedida =>
                unidadMedida.Codigo == codigoNormalizado));
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

    private sealed class FakeProductoPresentacionRepository : IProductoPresentacionRepository
    {
        public List<ProductoPresentacion> Presentaciones { get; } = [];

        public Task AgregarAsync(
            ProductoPresentacion presentacion,
            CancellationToken cancellationToken = default)
        {
            Presentaciones.Add(presentacion);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoPresentacion>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoPresentacion>>(
                Presentaciones.Where(presentacion =>
                        presentacion.EmpresaId == empresaId &&
                        presentacion.ProductoId == productoId)
                    .ToArray());
        }

        public Task<ProductoPresentacion?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Presentaciones.FirstOrDefault(presentacion =>
                presentacion.EmpresaId == empresaId &&
                presentacion.Id == id));
        }

        public Task<bool> ExisteCodigoBarrasAsync(
            Guid empresaId,
            string codigoBarras,
            CancellationToken cancellationToken = default)
        {
            var codigoNormalizado = codigoBarras.Trim();

            return Task.FromResult(Presentaciones.Any(presentacion =>
                presentacion.EmpresaId == empresaId &&
                presentacion.CodigoBarras == codigoNormalizado));
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

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>(
                Variantes.Where(variante => variante.EmpresaId == empresaId).ToArray());
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

    private sealed class FakeReglaPrecioMayoristaRepository : IReglaPrecioMayoristaRepository
    {
        public List<ReglaPrecioMayorista> Reglas { get; } = [];

        public Task AgregarAsync(
            ReglaPrecioMayorista regla,
            CancellationToken cancellationToken = default)
        {
            Reglas.Add(regla);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ReglaPrecioMayorista> reglas = Reglas
                .Where(regla =>
                    regla.EmpresaId == empresaId &&
                    regla.ProductoId == productoId)
                .OrderBy(regla => regla.CantidadMinima)
                .ToArray();

            return Task.FromResult(reglas);
        }

        public Task<IReadOnlyCollection<ReglaPrecioMayorista>> ListarActivasPorProductosAsync(
            Guid empresaId,
            IReadOnlyCollection<Guid> productoIds,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ReglaPrecioMayorista> reglas = Reglas
                .Where(regla =>
                    regla.EmpresaId == empresaId &&
                    regla.Activa &&
                    productoIds.Contains(regla.ProductoId))
                .ToArray();

            return Task.FromResult(reglas);
        }

        public Task<ReglaPrecioMayorista?> ObtenerPorEmpresaYProductoAsync(
            Guid empresaId,
            Guid productoId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Reglas.FirstOrDefault(regla =>
                regla.EmpresaId == empresaId &&
                regla.ProductoId == productoId &&
                regla.Id == id));
        }

        public Task<bool> ExisteActivaPorCantidadMinimaAsync(
            Guid empresaId,
            Guid productoId,
            int cantidadMinima,
            Guid? excluirId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Reglas.Any(regla =>
                regla.EmpresaId == empresaId &&
                regla.ProductoId == productoId &&
                regla.CantidadMinima == cantidadMinima &&
                regla.Activa &&
                (!excluirId.HasValue || regla.Id != excluirId.Value)));
        }

        public Task ActualizarAsync(
            ReglaPrecioMayorista regla,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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

    private sealed class FakeSedeRepository : ISedeRepository
    {
        public List<Sede> Sedes { get; } =
        [
            new Sede(SedeId, EmpresaId, "Tienda demo", TipoSede.TIENDA)
        ];

        public Task AgregarAsync(Sede sede, CancellationToken cancellationToken = default)
        {
            Sedes.Add(sede);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Sede>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Sede>>(
                Sedes.Where(sede => sede.EmpresaId == empresaId).ToArray());
        }

        public Task<Sede?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sedes.FirstOrDefault(sede =>
                sede.EmpresaId == empresaId &&
                sede.Id == id));
        }
    }

    private sealed class FakePuntoVentaRepository : IPuntoVentaRepository
    {
        public List<PuntoVenta> PuntosVenta { get; } =
        [
            new PuntoVenta(PuntoVentaId, EmpresaId, SedeId, "Caja principal")
        ];

        public Task AgregarAsync(PuntoVenta puntoVenta, CancellationToken cancellationToken = default)
        {
            PuntosVenta.Add(puntoVenta);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<PuntoVenta>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PuntoVenta>>(
                PuntosVenta.Where(puntoVenta => puntoVenta.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<PuntoVenta>> ListarPorSedeAsync(
            Guid empresaId,
            Guid sedeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PuntoVenta>>(
                PuntosVenta
                    .Where(puntoVenta => puntoVenta.EmpresaId == empresaId && puntoVenta.SedeId == sedeId)
                    .ToArray());
        }

        public Task<PuntoVenta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PuntosVenta.FirstOrDefault(puntoVenta =>
                puntoVenta.EmpresaId == empresaId &&
                puntoVenta.Id == id));
        }
    }

    private sealed class FakeCompraRepository : ICompraRepository
    {
        public List<Compra> Compras { get; } = [];

        public Task AgregarAsync(Compra compra, CancellationToken cancellationToken = default)
        {
            Compras.Add(compra);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Compra>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Compra>>(
                Compras.Where(compra => compra.EmpresaId == empresaId).ToArray());
        }

        public Task<Compra?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Compras.FirstOrDefault(compra =>
                compra.EmpresaId == empresaId &&
                compra.Id == id));
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

        public Task<IReadOnlyCollection<Venta>> ListarRegistradasPorEmpresaYFechaAsync(
            Guid empresaId,
            DateTimeOffset desde,
            DateTimeOffset hastaExclusivo,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Venta>>(
                Ventas.Where(venta =>
                    venta.EmpresaId == empresaId &&
                    venta.Estado == EstadoVenta.Registrada &&
                    venta.Fecha >= desde &&
                    venta.Fecha < hastaExclusivo).ToArray());
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

    private sealed class FakeSesionCajaRepository : ISesionCajaRepository
    {
        public List<SesionCaja> Sesiones { get; } = [];

        public Task AgregarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
        {
            Sesiones.Add(sesionCaja);
            return Task.CompletedTask;
        }

        public Task<SesionCaja?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sesiones.FirstOrDefault(sesion =>
                sesion.EmpresaId == empresaId &&
                sesion.Id == id));
        }

        public Task<SesionCaja?> ObtenerAbiertaPorPuntoVentaAsync(
            Guid empresaId,
            Guid puntoVentaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sesiones.FirstOrDefault(sesion =>
                sesion.EmpresaId == empresaId &&
                sesion.PuntoVentaId == puntoVentaId &&
                sesion.Estado == EstadoSesionCaja.Abierta));
        }

        public Task GuardarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeComprobanteRepository : IComprobanteRepository
    {
        public List<Comprobante> Comprobantes { get; } = [];

        public Task AgregarAsync(Comprobante comprobante, CancellationToken cancellationToken = default)
        {
            Comprobantes.Add(comprobante);
            return Task.CompletedTask;
        }

        public Task<bool> ExistePorVentaAsync(
            Guid empresaId,
            Guid ventaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Comprobantes.Any(
                comprobante => comprobante.EmpresaId == empresaId && comprobante.VentaId == ventaId));
        }

        public Task<Comprobante?> ObtenerEmisionAceptadaPorVentaAsync(
            Guid empresaId,
            Guid ventaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Comprobantes
                .Where(comprobante =>
                    comprobante.EmpresaId == empresaId &&
                    comprobante.VentaId == ventaId &&
                    (comprobante.TipoComprobante == "01" || comprobante.TipoComprobante == "03") &&
                    (comprobante.EstadoCpe == "ACEPTADO" || comprobante.EstadoCpe == "SIMULADO"))
                .OrderByDescending(comprobante => comprobante.FechaCreacion)
                .FirstOrDefault());
        }

        public Task<Comprobante?> ObtenerNotaCreditoAceptadaPorComprobanteAfectadoAsync(
            Guid empresaId,
            Guid comprobanteAfectadoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Comprobantes
                .Where(comprobante =>
                    comprobante.EmpresaId == empresaId &&
                    comprobante.TipoComprobante == "07" &&
                    comprobante.ComprobanteAfectadoId == comprobanteAfectadoId &&
                    (comprobante.EstadoCpe == "ACEPTADO" || comprobante.EstadoCpe == "SIMULADO"))
                .OrderByDescending(comprobante => comprobante.FechaCreacion)
                .FirstOrDefault());
        }
    }

    private sealed class FakePedidoDigitalRepository : IPedidoDigitalRepository
    {
        public Task AgregarAsync(PedidoDigital pedido, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyCollection<PedidoDigital>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PedidoDigital>>([]);

        public Task<PedidoDigital?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PedidoDigital?>(null);
    }

    private sealed class FakeMovimientoInventarioRepository : IMovimientoInventarioRepository
    {
        public Task AgregarAsync(MovimientoInventario movimiento, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyCollection<MovimientoInventario>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<MovimientoInventario>>([]);
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
            Guid sedeId,
            Guid productoId,
            Guid? productoVarianteId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stocks.FirstOrDefault(stock =>
                stock.EmpresaId == empresaId &&
                stock.SedeId == sedeId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId == productoVarianteId));
        }

        public Task<IReadOnlyCollection<StockProducto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<StockProducto>>(
                Stocks.Where(stock => stock.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<StockProducto>> ListarPorSedeAsync(
            Guid empresaId,
            Guid sedeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<StockProducto>>(
                Stocks.Where(stock => stock.EmpresaId == empresaId && stock.SedeId == sedeId).ToArray());
        }

        public Task GuardarAsync(
            StockProducto stock,
            CancellationToken cancellationToken = default)
        {
            var index = Stocks.FindIndex(actual =>
                actual.EmpresaId == stock.EmpresaId &&
                actual.SedeId == stock.SedeId &&
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

    private sealed class FakeSerieComprobanteRepository : ISerieComprobanteRepository
    {
        public List<SerieComprobante> Series { get; } = [];

        public Task AgregarAsync(SerieComprobante serie, CancellationToken cancellationToken = default)
        {
            Series.Add(serie);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<SerieComprobante>> ListarPorSedeAsync(
            Guid empresaId,
            Guid sedeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<SerieComprobante>>(
                Series.Where(serie => serie.EmpresaId == empresaId && serie.SedeId == sedeId).ToArray());
        }

        public Task<SerieComprobante?> ObtenerActivaAsync(
            Guid empresaId,
            Guid sedeId,
            string tipoComprobante,
            string serie,
            CancellationToken cancellationToken = default)
        {
            var tipoNormalizado = tipoComprobante.Trim().ToUpperInvariant();
            var serieNormalizada = serie.Trim().ToUpperInvariant();

            return Task.FromResult(Series.SingleOrDefault(serieComprobante =>
                serieComprobante.EmpresaId == empresaId &&
                serieComprobante.SedeId == sedeId &&
                serieComprobante.TipoComprobante == tipoNormalizado &&
                serieComprobante.Serie == serieNormalizada &&
                serieComprobante.Activa));
        }

        public Task<SerieComprobante?> ObtenerActivaPorSedeYTipoAsync(
            Guid empresaId,
            Guid sedeId,
            string tipoComprobante,
            CancellationToken cancellationToken = default)
        {
            var tipoNormalizado = tipoComprobante.Trim().ToUpperInvariant();

            return Task.FromResult(Series
                .Where(serieComprobante =>
                    serieComprobante.EmpresaId == empresaId &&
                    serieComprobante.SedeId == sedeId &&
                    serieComprobante.TipoComprobante == tipoNormalizado &&
                    serieComprobante.Activa)
                .OrderBy(serieComprobante => serieComprobante.Serie, StringComparer.Ordinal)
                .FirstOrDefault());
        }

        public Task GuardarAsync(SerieComprobante serie, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
