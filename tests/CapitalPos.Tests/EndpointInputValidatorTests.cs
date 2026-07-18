using CapitalPos.Api.Endpoints;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class EndpointInputValidatorTests
{
    [Fact]
    public void CrearEmpresa_acepta_request_valido()
    {
        var request = new CrearEmpresaRequest("20601234567", "CapitalPOS SAC", "CapitalPOS");

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.True(esValido);
        Assert.Equal(string.Empty, error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("2060123456A")]
    public void CrearEmpresa_rechaza_ruc_invalido(string ruc)
    {
        var request = new CrearEmpresaRequest(ruc, "CapitalPOS SAC");

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("RUC", error);
    }

    [Fact]
    public void CrearEmpresa_rechaza_razon_social_vacia()
    {
        var request = new CrearEmpresaRequest("20601234567", "");

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("razon social", error);
    }

    [Fact]
    public void CrearUsuario_acepta_request_valido()
    {
        var request = new CrearUsuarioRequest("Ada", "Lovelace", "ada@example.com");

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.True(esValido);
        Assert.Equal(string.Empty, error);
    }

    [Theory]
    [InlineData("", "Lovelace", "ada@example.com", "nombre")]
    [InlineData("Ada", "", "ada@example.com", "apellido")]
    [InlineData("Ada", "Lovelace", "", "correo")]
    [InlineData("Ada", "Lovelace", "correo-invalido", "correo")]
    public void CrearUsuario_rechaza_campos_invalidos(
        string nombre,
        string apellido,
        string correo,
        string campoEsperado)
    {
        var request = new CrearUsuarioRequest(nombre, apellido, correo);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains(campoEsperado, error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsignarUsuarioEmpresa_acepta_request_valido()
    {
        var request = new AsignarUsuarioEmpresaRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Administrador);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.True(esValido);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void AsignarUsuarioEmpresa_rechaza_usuario_vacio()
    {
        var request = new AsignarUsuarioEmpresaRequest(
            Guid.Empty,
            Guid.NewGuid(),
            RolEmpresa.Administrador);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("usuario", error);
    }

    [Fact]
    public void AsignarUsuarioEmpresa_rechaza_empresa_vacia()
    {
        var request = new AsignarUsuarioEmpresaRequest(
            Guid.NewGuid(),
            Guid.Empty,
            RolEmpresa.Administrador);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("empresa", error);
    }

    [Fact]
    public void AsignarUsuarioEmpresa_rechaza_rol_invalido()
    {
        var request = new AsignarUsuarioEmpresaRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            (RolEmpresa)999);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("rol", error);
    }

    [Fact]
    public void CambiarRolUsuarioEmpresa_rechaza_rol_invalido()
    {
        var request = new CambiarRolUsuarioEmpresaRequest((RolEmpresa)999);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("rol", error);
    }

    [Fact]
    public void GuardarConfiguracionFiscal_acepta_request_valido()
    {
        var request = CrearConfiguracionFiscalRequest();

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.True(esValido);
        Assert.Equal(string.Empty, error);
    }

    [Theory]
    [InlineData("", "RUC")]
    [InlineData("123", "RUC")]
    [InlineData("2060123456A", "RUC")]
    public void GuardarConfiguracionFiscal_rechaza_ruc_invalido(string ruc, string campoEsperado)
    {
        var request = CrearConfiguracionFiscalRequest(ruc: ruc);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains(campoEsperado, error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "ubigeo")]
    [InlineData("15010", "ubigeo")]
    [InlineData("15010A", "ubigeo")]
    public void GuardarConfiguracionFiscal_rechaza_ubigeo_invalido(string ubigeo, string campoEsperado)
    {
        var request = CrearConfiguracionFiscalRequest(ubigeo: ubigeo);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains(campoEsperado, error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "razon social")]
    public void GuardarConfiguracionFiscal_rechaza_razon_social_vacia(
        string razonSocial,
        string campoEsperado)
    {
        var request = CrearConfiguracionFiscalRequest(razonSocial: razonSocial);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains(campoEsperado, error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "direccion")]
    public void GuardarConfiguracionFiscal_rechaza_direccion_vacia(
        string direccion,
        string campoEsperado)
    {
        var request = CrearConfiguracionFiscalRequest(direccion: direccion);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains(campoEsperado, error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AjustarStockProducto_acepta_request_valido()
    {
        var request = new AjustarStockProductoRequest(Guid.NewGuid(), Guid.NewGuid(), null, 10m);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.True(esValido);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void AjustarStockProducto_rechaza_sede_vacia()
    {
        var request = new AjustarStockProductoRequest(Guid.Empty, Guid.NewGuid(), null, 10m);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("sede", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AjustarStockProducto_rechaza_producto_vacio()
    {
        var request = new AjustarStockProductoRequest(Guid.NewGuid(), Guid.Empty, null, 10m);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("producto", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AjustarStockProducto_rechaza_variante_vacia()
    {
        var request = new AjustarStockProductoRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 10m);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("variante", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AjustarStockProducto_rechaza_cantidad_negativa()
    {
        var request = new AjustarStockProductoRequest(Guid.NewGuid(), Guid.NewGuid(), null, -1m);

        var esValido = EndpointInputValidator.TryValidate(request, out var error);

        Assert.False(esValido);
        Assert.Contains("cantidad", error, StringComparison.OrdinalIgnoreCase);
    }

    private static GuardarConfiguracionFiscalEmpresaRequest CrearConfiguracionFiscalRequest(
        string ruc = "20601234567",
        string razonSocial = "CapitalPOS SAC",
        string? nombreComercial = "CapitalPOS",
        string ubigeo = "150101",
        string direccion = "AV. AREQUIPA 123",
        string departamento = "LIMA",
        string provincia = "LIMA",
        string distrito = "LIMA")
    {
        return new GuardarConfiguracionFiscalEmpresaRequest(
            ruc,
            razonSocial,
            nombreComercial,
            ubigeo,
            direccion,
            departamento,
            provincia,
            distrito);
    }
}
