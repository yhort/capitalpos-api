using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ComprobanteTests
{
    [Fact]
    public void Crear_comprobante_valido_normaliza_campos()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var fechaCreacion = DateTimeOffset.UtcNow;

        var comprobante = new Comprobante(
            id,
            empresaId,
            ventaId,
            " 03 ",
            " b001 ",
            12,
            " simulado ",
            " Aceptado ",
            " hash ",
            " xml.xml ",
            " zip.zip ",
            " cdr.zip ",
            fechaCreacion);

        Assert.Equal(id, comprobante.Id);
        Assert.Equal(empresaId, comprobante.EmpresaId);
        Assert.Equal(ventaId, comprobante.VentaId);
        Assert.Equal("03", comprobante.TipoComprobante);
        Assert.Equal("B001", comprobante.Serie);
        Assert.Equal(12, comprobante.Correlativo);
        Assert.Equal("SIMULADO", comprobante.EstadoCpe);
        Assert.Equal("Aceptado", comprobante.Mensaje);
        Assert.Equal("hash", comprobante.Hash);
        Assert.Equal("xml.xml", comprobante.NombreXml);
        Assert.Equal("zip.zip", comprobante.NombreZip);
        Assert.Equal("cdr.zip", comprobante.NombreCdr);
        Assert.Equal(fechaCreacion, comprobante.FechaCreacion);
    }

    [Fact]
    public void Rechaza_empresa_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Comprobante(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                "03",
                "B001",
                1,
                "SIMULADO"));
    }

    [Fact]
    public void Rechaza_venta_id_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Comprobante(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty,
                "03",
                "B001",
                1,
                "SIMULADO"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_correlativo_no_positivo(int correlativo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Comprobante(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "03",
                "B001",
                correlativo,
                "SIMULADO"));
    }

    [Fact]
    public void Rechaza_estado_cpe_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Comprobante(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "03",
                "B001",
                1,
                " "));
    }
}
