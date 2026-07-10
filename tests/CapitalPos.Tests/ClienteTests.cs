using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ClienteTests
{
    [Fact]
    public void Crear_cliente_valido()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var cliente = new Cliente(
            id,
            empresaId,
            " dni ",
            " 12345678 ",
            " Juan Perez ",
            " Av. Lima 123 ",
            fechaCreacion: fechaCreacion);

        Assert.Equal(id, cliente.Id);
        Assert.Equal(empresaId, cliente.EmpresaId);
        Assert.Equal("DNI", cliente.TipoDocumento);
        Assert.Equal("12345678", cliente.NumeroDocumento);
        Assert.Equal("Juan Perez", cliente.NombreRazonSocial);
        Assert.Equal("Av. Lima 123", cliente.Direccion);
        Assert.True(cliente.Activo);
        Assert.Equal(fechaCreacion, cliente.FechaCreacion);
    }

    [Fact]
    public void Permite_numero_documento_y_direccion_opcionales()
    {
        var cliente = new Cliente(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SIN_DOCUMENTO",
            null,
            "Cliente generico");

        Assert.Equal(string.Empty, cliente.NumeroDocumento);
        Assert.Equal(string.Empty, cliente.Direccion);
    }

    [Fact]
    public void Rechaza_identificador_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cliente(
                Guid.Empty,
                Guid.NewGuid(),
                "DNI",
                "12345678",
                "Juan Perez"));
    }

    [Fact]
    public void Rechaza_empresa_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cliente(
                Guid.NewGuid(),
                Guid.Empty,
                "DNI",
                "12345678",
                "Juan Perez"));
    }

    [Fact]
    public void Rechaza_tipo_documento_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cliente(
                Guid.NewGuid(),
                Guid.NewGuid(),
                " ",
                "12345678",
                "Juan Perez"));
    }

    [Fact]
    public void Rechaza_nombre_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cliente(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DNI",
                "12345678",
                " "));
    }

    [Fact]
    public void Actualizar_datos_basicos_normaliza_y_actualiza_campos()
    {
        var cliente = new Cliente(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DNI",
            "12345678",
            "Juan Perez");

        cliente.ActualizarDatosBasicos(
            " ruc ",
            " 20601234567 ",
            " CapitalPOS SAC ",
            " Calle Uno ");

        Assert.Equal("RUC", cliente.TipoDocumento);
        Assert.Equal("20601234567", cliente.NumeroDocumento);
        Assert.Equal("CapitalPOS SAC", cliente.NombreRazonSocial);
        Assert.Equal("Calle Uno", cliente.Direccion);
    }

    [Fact]
    public void Activar_y_desactivar_cambian_estado()
    {
        var cliente = new Cliente(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DNI",
            "12345678",
            "Juan Perez");

        cliente.Desactivar();
        Assert.False(cliente.Activo);

        cliente.Activar();
        Assert.True(cliente.Activo);
    }
}
