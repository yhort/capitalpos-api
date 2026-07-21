using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class SesionCajaTests
{
    private static readonly Guid EmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid SedeId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid PuntoVentaId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    [Fact]
    public void Crear_sesion_caja_valida_y_normaliza_observacion()
    {
        var usuarioId = Guid.NewGuid();
        var fechaApertura = DateTimeOffset.Parse("2026-07-21T09:00:00Z");

        var sesion = new SesionCaja(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            PuntoVentaId,
            100m,
            usuarioId,
            " Apertura demo ",
            fechaApertura);

        Assert.Equal(EmpresaId, sesion.EmpresaId);
        Assert.Equal(SedeId, sesion.SedeId);
        Assert.Equal(PuntoVentaId, sesion.PuntoVentaId);
        Assert.Equal(usuarioId, sesion.UsuarioAperturaId);
        Assert.Equal(EstadoSesionCaja.Abierta, sesion.Estado);
        Assert.Equal(100m, sesion.MontoInicial);
        Assert.Equal(fechaApertura, sesion.FechaApertura);
        Assert.Equal("Apertura demo", sesion.ObservacionApertura);
        Assert.Null(sesion.FechaCierre);
        Assert.Null(sesion.MontoDeclaradoCierre);
        Assert.Null(sesion.DiferenciaCierre);
    }

    [Fact]
    public void Crear_sesion_caja_rechaza_campos_obligatorios_y_monto_negativo()
    {
        Assert.Throws<ArgumentException>(() => CrearSesion(id: Guid.Empty));
        Assert.Throws<ArgumentException>(() => CrearSesion(empresaId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => CrearSesion(sedeId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => CrearSesion(puntoVentaId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => CrearSesion(usuarioAperturaId: Guid.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => CrearSesion(montoInicial: -1m));
    }

    [Fact]
    public void Cerrar_sesion_abierta_calcula_diferencia()
    {
        var sesion = CrearSesion(montoInicial: 100m);
        var usuarioCierreId = Guid.NewGuid();
        var fechaCierre = sesion.FechaApertura.AddHours(8);

        sesion.Cerrar(130m, fechaCierre, usuarioCierreId, " Cierre correcto ");

        Assert.Equal(EstadoSesionCaja.Cerrada, sesion.Estado);
        Assert.Equal(usuarioCierreId, sesion.UsuarioCierreId);
        Assert.Equal(130m, sesion.MontoDeclaradoCierre);
        Assert.Equal(30m, sesion.DiferenciaCierre);
        Assert.Equal(fechaCierre, sesion.FechaCierre);
        Assert.Equal("Cierre correcto", sesion.ObservacionCierre);
    }

    [Fact]
    public void Cerrar_rechaza_sesion_cerrada_monto_negativo_fecha_invalida_y_usuario_vacio()
    {
        var sesion = CrearSesion();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sesion.Cerrar(-1m, sesion.FechaApertura.AddHours(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sesion.Cerrar(10m, sesion.FechaApertura.AddMinutes(-1)));
        Assert.Throws<ArgumentException>(() =>
            sesion.Cerrar(10m, sesion.FechaApertura.AddHours(1), Guid.Empty));

        sesion.Cerrar(10m, sesion.FechaApertura.AddHours(1));

        Assert.Throws<InvalidOperationException>(() =>
            sesion.Cerrar(10m, sesion.FechaApertura.AddHours(2)));
    }

    private static SesionCaja CrearSesion(
        Guid? id = null,
        Guid? empresaId = null,
        Guid? sedeId = null,
        Guid? puntoVentaId = null,
        Guid? usuarioAperturaId = null,
        decimal montoInicial = 0m)
    {
        return new SesionCaja(
            id ?? Guid.NewGuid(),
            empresaId ?? EmpresaId,
            sedeId ?? SedeId,
            puntoVentaId ?? PuntoVentaId,
            montoInicial,
            usuarioAperturaId ?? Guid.NewGuid(),
            fechaApertura: DateTimeOffset.Parse("2026-07-21T09:00:00Z"));
    }
}
