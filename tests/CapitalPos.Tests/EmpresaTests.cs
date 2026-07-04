using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class EmpresaTests
{
    [Fact]
    public void Crear_empresa_valida()
    {
        var id = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var empresa = new Empresa(
            id,
            " 20606264004 ",
            " CapitalPOS SAC ",
            " CapitalPOS ",
            fechaCreacion: fechaCreacion);

        Assert.Equal(id, empresa.Id);
        Assert.Equal("20606264004", empresa.Ruc);
        Assert.Equal("CapitalPOS SAC", empresa.RazonSocial);
        Assert.Equal("CapitalPOS", empresa.NombreComercial);
        Assert.True(empresa.Activa);
        Assert.Equal(fechaCreacion, empresa.FechaCreacion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890A")]
    [InlineData("123456789012")]
    public void Rechaza_ruc_invalido(string ruc)
    {
        Assert.Throws<ArgumentException>(() =>
            new Empresa(Guid.NewGuid(), ruc, "CapitalPOS SAC"));
    }
}
