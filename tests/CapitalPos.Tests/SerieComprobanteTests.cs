using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class SerieComprobanteTests
{
    [Fact]
    public void Crear_serie_valida_normaliza_tipo_y_serie()
    {
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var fecha = new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

        var serie = new SerieComprobante(
            Guid.NewGuid(),
            empresaId,
            sedeId,
            " 03 ",
            " b001 ",
            correlativoActual: 12,
            fechaCreacion: fecha);

        Assert.Equal(empresaId, serie.EmpresaId);
        Assert.Equal(sedeId, serie.SedeId);
        Assert.Equal("03", serie.TipoComprobante);
        Assert.Equal("B001", serie.Serie);
        Assert.Equal(12, serie.CorrelativoActual);
        Assert.True(serie.Activa);
        Assert.Equal(fecha, serie.FechaCreacion);
    }

    [Fact]
    public void Crear_serie_exige_id()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new SerieComprobante(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "03", "B001", 0));

        Assert.Contains("identificador", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_serie_exige_empresa()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new SerieComprobante(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "03", "B001", 0));

        Assert.Contains("empresa", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_serie_exige_sede()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new SerieComprobante(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "03", "B001", 0));

        Assert.Contains("sede", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Crear_serie_exige_tipo_comprobante(string tipoComprobante)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new SerieComprobante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), tipoComprobante, "B001", 0));

        Assert.Contains("tipo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Crear_serie_exige_serie(string serie)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new SerieComprobante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "03", serie, 0));

        Assert.Contains("serie", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_serie_rechaza_correlativo_negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SerieComprobante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "03", "B001", -1));
    }

    [Fact]
    public void Obtener_siguiente_correlativo_no_modifica_el_actual()
    {
        var serie = new SerieComprobante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "03", "B001", 7);

        Assert.Equal(8, serie.ObtenerSiguienteCorrelativo());
        Assert.Equal(7, serie.CorrelativoActual);
    }

    [Fact]
    public void Incrementar_correlativo_aumenta_y_devuelve_el_nuevo_valor()
    {
        var serie = new SerieComprobante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "03", "B001", 7);

        var correlativo = serie.IncrementarCorrelativo();

        Assert.Equal(8, correlativo);
        Assert.Equal(8, serie.CorrelativoActual);
    }

    [Fact]
    public void Activar_y_desactivar_serie_cambia_estado()
    {
        var serie = new SerieComprobante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "03", "B001", 0);

        serie.Desactivar();
        Assert.False(serie.Activa);

        serie.Activar();
        Assert.True(serie.Activa);
    }
}
