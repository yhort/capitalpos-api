using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Endpoints;
using CapitalPos.Application.Catalogo;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Dashboard;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Inventario;
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
            [(99m, 999m)]));
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
        Assert.DoesNotContain("999", content);
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
            [(productoId, varianteId, 99m, 999m)]));
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
        Assert.DoesNotContain("999", content);
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
        Guid? vendedorId = null)
    {
        return new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(productoId, productoVarianteId, cantidad, 10m, 0m, cantidad * 10m)],
            puntoVentaId ?? PuntoVentaId,
            canalVenta,
            vendedorId);
    }

    private static Venta CrearVentaReporte(
        Guid empresaId,
        CanalVenta canalVenta,
        DateTimeOffset fecha,
        IReadOnlyCollection<(decimal Cantidad, decimal Total)> detalles)
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

        public FakeStockProductoRepository StockRepository { get; } = new();

        public FakeSedeRepository SedeRepository { get; } = new();

        public FakePuntoVentaRepository PuntoVentaRepository { get; } = new();

        public FakeSerieComprobanteRepository SerieComprobanteRepository { get; } = new();

        public FakeVentaRepository VentaRepository { get; } = new();

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
                services.RemoveAll<IClienteRepository>();
                services.RemoveAll<IVentaRepository>();
                services.RemoveAll<IComprobanteRepository>();
                services.RemoveAll<IConfiguracionFiscalEmpresaRepository>();
                services.RemoveAll<IStockProductoRepository>();
                services.RemoveAll<ISedeRepository>();
                services.RemoveAll<IPuntoVentaRepository>();
                services.RemoveAll<ISerieComprobanteRepository>();
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
                services.AddSingleton<IClienteRepository, FakeClienteRepository>();
                services.AddSingleton<IVentaRepository>(VentaRepository);
                services.AddSingleton<IComprobanteRepository, FakeComprobanteRepository>();
                services.AddSingleton<IConfiguracionFiscalEmpresaRepository>(ConfiguracionFiscalRepository);
                services.AddSingleton<IStockProductoRepository>(StockRepository);
                services.AddSingleton<ISedeRepository>(SedeRepository);
                services.AddSingleton<IPuntoVentaRepository>(PuntoVentaRepository);
                services.AddSingleton<ISerieComprobanteRepository>(SerieComprobanteRepository);
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
