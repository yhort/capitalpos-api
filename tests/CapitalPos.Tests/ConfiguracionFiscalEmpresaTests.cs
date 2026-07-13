using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ConfiguracionFiscalEmpresaTests
{
    [Fact]
    public void Crear_configuracion_fiscal_valida()
    {
        var empresaId = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var configuracion = new ConfiguracionFiscalEmpresa(
            empresaId,
            " 20600000001 ",
            " CapitalPOS Demo SAC ",
            " CapitalPOS ",
            " 150101 ",
            " Av. Demo 123 ",
            " Lima ",
            " Lima ",
            " Lima ",
            fechaCreacion: fechaCreacion);

        Assert.Equal(empresaId, configuracion.EmpresaId);
        Assert.Equal("20600000001", configuracion.Ruc);
        Assert.Equal("CapitalPOS Demo SAC", configuracion.RazonSocial);
        Assert.Equal("CapitalPOS", configuracion.NombreComercial);
        Assert.Equal("150101", configuracion.Ubigeo);
        Assert.Equal("Av. Demo 123", configuracion.Direccion);
        Assert.Equal("Lima", configuracion.Departamento);
        Assert.Equal("Lima", configuracion.Provincia);
        Assert.Equal("Lima", configuracion.Distrito);
        Assert.True(configuracion.Activa);
        Assert.Equal(fechaCreacion, configuracion.FechaCreacion);
    }

    [Fact]
    public void Actualizar_datos_fiscales_normaliza_y_actualiza()
    {
        var configuracion = CrearConfiguracion();

        configuracion.ActualizarDatosFiscales(
            "20601234567",
            "Nueva Razon SAC",
            null,
            "150102",
            "Calle Uno",
            "LIMA",
            "LIMA",
            "ANCON");

        Assert.Equal("20601234567", configuracion.Ruc);
        Assert.Equal("Nueva Razon SAC", configuracion.RazonSocial);
        Assert.Equal(string.Empty, configuracion.NombreComercial);
        Assert.Equal("150102", configuracion.Ubigeo);
        Assert.Equal("Calle Uno", configuracion.Direccion);
        Assert.Equal("ANCON", configuracion.Distrito);
    }

    [Fact]
    public void Activar_y_desactivar_cambian_estado()
    {
        var configuracion = CrearConfiguracion();

        configuracion.Desactivar();
        Assert.False(configuracion.Activa);

        configuracion.Activar();
        Assert.True(configuracion.Activa);
    }

    [Fact]
    public void Rechaza_empresa_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new ConfiguracionFiscalEmpresa(
                Guid.Empty,
                "20600000001",
                "CapitalPOS Demo SAC",
                "CapitalPOS",
                "150101",
                "Av. Demo 123",
                "Lima",
                "Lima",
                "Lima"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("2060000000A")]
    public void Rechaza_ruc_invalido(string ruc)
    {
        Assert.Throws<ArgumentException>(() =>
            new ConfiguracionFiscalEmpresa(
                Guid.NewGuid(),
                ruc,
                "CapitalPOS Demo SAC",
                "CapitalPOS",
                "150101",
                "Av. Demo 123",
                "Lima",
                "Lima",
                "Lima"));
    }

    [Fact]
    public void Rechaza_razon_social_vacia()
    {
        Assert.Throws<ArgumentException>(() =>
            new ConfiguracionFiscalEmpresa(
                Guid.NewGuid(),
                "20600000001",
                " ",
                "CapitalPOS",
                "150101",
                "Av. Demo 123",
                "Lima",
                "Lima",
                "Lima"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("15010")]
    [InlineData("15010A")]
    public void Rechaza_ubigeo_invalido(string ubigeo)
    {
        Assert.Throws<ArgumentException>(() =>
            new ConfiguracionFiscalEmpresa(
                Guid.NewGuid(),
                "20600000001",
                "CapitalPOS Demo SAC",
                "CapitalPOS",
                ubigeo,
                "Av. Demo 123",
                "Lima",
                "Lima",
                "Lima"));
    }

    [Theory]
    [InlineData("", "Lima", "Lima", "Lima")]
    [InlineData("Av. Demo 123", "", "Lima", "Lima")]
    [InlineData("Av. Demo 123", "Lima", "", "Lima")]
    [InlineData("Av. Demo 123", "Lima", "Lima", "")]
    public void Rechaza_direccion_o_ubicacion_vacia(
        string direccion,
        string departamento,
        string provincia,
        string distrito)
    {
        Assert.Throws<ArgumentException>(() =>
            new ConfiguracionFiscalEmpresa(
                Guid.NewGuid(),
                "20600000001",
                "CapitalPOS Demo SAC",
                "CapitalPOS",
                "150101",
                direccion,
                departamento,
                provincia,
                distrito));
    }

    private static ConfiguracionFiscalEmpresa CrearConfiguracion()
    {
        return new ConfiguracionFiscalEmpresa(
            Guid.NewGuid(),
            "20600000001",
            "CapitalPOS Demo SAC",
            "CapitalPOS",
            "150101",
            "Av. Demo 123",
            "Lima",
            "Lima",
            "Lima");
    }
}
