using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class SedeTests
{
    [Fact]
    public void Crear_sede_valida_asigna_empresa_y_datos()
    {
        var empresaId = Guid.NewGuid();
        var fecha = new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

        var sede = new Sede(
            Guid.NewGuid(),
            empresaId,
            " Tienda Centro ",
            TipoSede.TIENDA,
            codigoEstablecimiento: " 0001 ",
            direccion: " Av. Demo 123 ",
            distrito: " Lima ",
            provincia: " Lima ",
            departamento: " Lima ",
            fechaCreacion: fecha);

        Assert.Equal(empresaId, sede.EmpresaId);
        Assert.Equal("Tienda Centro", sede.Nombre);
        Assert.Equal(TipoSede.TIENDA, sede.Tipo);
        Assert.Equal("0001", sede.CodigoEstablecimiento);
        Assert.Equal("Av. Demo 123", sede.Direccion);
        Assert.Equal("Lima", sede.Distrito);
        Assert.True(sede.Activa);
        Assert.Equal(fecha, sede.FechaCreacion);
    }

    [Fact]
    public void Crear_sede_exige_empresa()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Sede(Guid.NewGuid(), Guid.Empty, "Tienda", TipoSede.TIENDA));

        Assert.Contains("empresa", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_sede_exige_nombre()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Sede(Guid.NewGuid(), Guid.NewGuid(), " ", TipoSede.TIENDA));

        Assert.Contains("nombre", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_sede_rechaza_tipo_invalido()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Sede(Guid.NewGuid(), Guid.NewGuid(), "Tienda", (TipoSede)999));
    }

    [Fact]
    public void Activar_y_desactivar_sede_cambia_estado()
    {
        var sede = new Sede(Guid.NewGuid(), Guid.NewGuid(), "Almacen", TipoSede.ALMACEN);

        sede.Desactivar();
        Assert.False(sede.Activa);

        sede.Activar();
        Assert.True(sede.Activa);
    }
}
