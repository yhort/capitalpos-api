using CapitalPos.Application.Caja;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationSesionCajaTests
{
    private static readonly Guid EmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OtraEmpresaId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SedeId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid PuntoVentaId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid UsuarioId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Abrir_sesion_caja_resuelve_sede_desde_punto_venta_y_usa_empresa_activa()
    {
        var repos = CrearRepositorios();
        var useCase = new AbrirSesionCajaUseCase(
            repos.SesionesCaja,
            repos.PuntosVenta,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        var sesion = await useCase.EjecutarAsync(new AbrirSesionCajaRequest(
            PuntoVentaId,
            120m,
            " Inicio "));

        Assert.Equal(EmpresaId, sesion.EmpresaId);
        Assert.Equal(SedeId, sesion.SedeId);
        Assert.Equal(PuntoVentaId, sesion.PuntoVentaId);
        Assert.Equal(UsuarioId, sesion.UsuarioAperturaId);
        Assert.Equal(120m, sesion.MontoInicial);
        Assert.Equal("Inicio", sesion.ObservacionApertura);
        Assert.Same(sesion, Assert.Single(repos.SesionesCaja.Sesiones));
    }

    [Fact]
    public async Task Abrir_sesion_caja_impide_doble_apertura()
    {
        var repos = CrearRepositorios();
        repos.SesionesCaja.Sesiones.Add(new SesionCaja(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            PuntoVentaId,
            50m));
        var useCase = new AbrirSesionCajaUseCase(
            repos.SesionesCaja,
            repos.PuntosVenta,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new AbrirSesionCajaRequest(PuntoVentaId, 10m)));

        Assert.Contains("abierta", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(repos.SesionesCaja.Sesiones);
    }

    [Fact]
    public async Task Abrir_sesion_caja_rechaza_punto_venta_de_otra_empresa_o_inactivo()
    {
        var reposOtraEmpresa = CrearRepositorios(empresaPuntoVenta: OtraEmpresaId);
        var useCaseOtraEmpresa = new AbrirSesionCajaUseCase(
            reposOtraEmpresa.SesionesCaja,
            reposOtraEmpresa.PuntosVenta,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCaseOtraEmpresa.EjecutarAsync(new AbrirSesionCajaRequest(PuntoVentaId, 10m)));

        var reposInactivo = CrearRepositorios(puntoVentaActivo: false);
        var useCaseInactivo = new AbrirSesionCajaUseCase(
            reposInactivo.SesionesCaja,
            reposInactivo.PuntosVenta,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCaseInactivo.EjecutarAsync(new AbrirSesionCajaRequest(PuntoVentaId, 10m)));
    }

    [Fact]
    public async Task Obtener_sesion_caja_abierta_filtra_por_empresa_activa_y_punto_venta()
    {
        var sesionEsperada = new SesionCaja(Guid.NewGuid(), EmpresaId, SedeId, PuntoVentaId, 10m);
        var repos = CrearRepositorios();
        repos.SesionesCaja.Sesiones.Add(new SesionCaja(Guid.NewGuid(), OtraEmpresaId, SedeId, PuntoVentaId, 10m));
        repos.SesionesCaja.Sesiones.Add(new SesionCaja(Guid.NewGuid(), EmpresaId, SedeId, Guid.NewGuid(), 10m));
        repos.SesionesCaja.Sesiones.Add(sesionEsperada);
        var useCase = new ObtenerSesionCajaAbiertaUseCase(
            repos.SesionesCaja,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        var sesion = await useCase.EjecutarAsync(PuntoVentaId);

        Assert.NotNull(sesion);
        Assert.Equal(sesionEsperada.Id, sesion.Id);
    }

    [Fact]
    public async Task Cerrar_sesion_caja_cierra_abierta_y_guarda()
    {
        var sesionAbierta = new SesionCaja(Guid.NewGuid(), EmpresaId, SedeId, PuntoVentaId, 100m);
        var repos = CrearRepositorios();
        repos.SesionesCaja.Sesiones.Add(sesionAbierta);
        var useCase = new CerrarSesionCajaUseCase(
            repos.SesionesCaja,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        var sesion = await useCase.EjecutarAsync(new CerrarSesionCajaRequest(
            sesionAbierta.Id,
            90m,
            " Cierre ",
            UsuarioId));

        Assert.Equal(EstadoSesionCaja.Cerrada, sesion.Estado);
        Assert.Equal(90m, sesion.MontoDeclaradoCierre);
        Assert.Equal(-10m, sesion.DiferenciaCierre);
        Assert.Equal("Cierre", sesion.ObservacionCierre);
        Assert.True(repos.SesionesCaja.Guardado);
    }

    [Fact]
    public async Task Cerrar_sesion_caja_rechaza_inexistente_o_ya_cerrada()
    {
        var repos = CrearRepositorios();
        var useCase = new CerrarSesionCajaUseCase(
            repos.SesionesCaja,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CerrarSesionCajaRequest(Guid.NewGuid(), 10m)));

        var sesionCerrada = new SesionCaja(Guid.NewGuid(), EmpresaId, SedeId, PuntoVentaId, 10m);
        sesionCerrada.Cerrar(10m, sesionCerrada.FechaApertura.AddHours(1));
        repos.SesionesCaja.Sesiones.Add(sesionCerrada);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CerrarSesionCajaRequest(sesionCerrada.Id, 10m)));
    }

    [Fact]
    public async Task Use_cases_fallan_sin_empresa_activa()
    {
        var repos = CrearRepositorios();
        var empresaActiva = new EmpresaActivaContextFake(Guid.Empty, UsuarioId, tieneEmpresaActiva: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new AbrirSesionCajaUseCase(repos.SesionesCaja, repos.PuntosVenta, empresaActiva)
                .EjecutarAsync(new AbrirSesionCajaRequest(PuntoVentaId, 10m)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ObtenerSesionCajaAbiertaUseCase(repos.SesionesCaja, empresaActiva)
                .EjecutarAsync(PuntoVentaId));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CerrarSesionCajaUseCase(repos.SesionesCaja, empresaActiva)
                .EjecutarAsync(new CerrarSesionCajaRequest(Guid.NewGuid(), 10m)));
    }

    [Fact]
    public async Task Resumen_caja_abierta_suma_ventas_y_pagos_por_metodo()
    {
        var apertura = new DateTimeOffset(2026, 7, 25, 9, 0, 0, TimeSpan.FromHours(-5));
        var sesion = new SesionCaja(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            PuntoVentaId,
            100m,
            fechaApertura: apertura);
        var repos = CrearRepositorios();
        repos.SesionesCaja.Sesiones.Add(sesion);
        var ventas = new VentaRepositoryFake();
        ventas.Ventas.Add(CrearVenta(
            EmpresaId,
            PuntoVentaId,
            apertura.AddMinutes(30),
            60m,
            [(MetodoPago.EFECTIVO, 20m), (MetodoPago.YAPE, 40m)]));
        var useCase = new ObtenerResumenSesionCajaUseCase(
            repos.SesionesCaja,
            ventas,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        var resumen = await useCase.EjecutarAsync(sesion.Id);

        Assert.NotNull(resumen);
        Assert.Equal("Abierta", resumen.Estado);
        Assert.Equal(60m, resumen.TotalVentas);
        Assert.Equal(1, resumen.CantidadVentas);
        Assert.Equal(60m, resumen.TotalPagado);
        Assert.Null(resumen.DiferenciaOperativa);
        Assert.Collection(
            resumen.PagosPorMetodo.Where(item => item.Total > 0).OrderBy(item => item.MetodoPago),
            efectivo =>
            {
                Assert.Equal("EFECTIVO", efectivo.MetodoPago);
                Assert.Equal(20m, efectivo.Total);
                Assert.Equal(1, efectivo.CantidadPagos);
            },
            yape =>
            {
                Assert.Equal("YAPE", yape.MetodoPago);
                Assert.Equal(40m, yape.Total);
                Assert.Equal(1, yape.CantidadPagos);
            });
    }

    [Fact]
    public async Task Resumen_caja_cerrada_excluye_ventas_fuera_intervalo_punto_empresa_y_anuladas()
    {
        var apertura = new DateTimeOffset(2026, 7, 25, 9, 0, 0, TimeSpan.FromHours(-5));
        var cierre = apertura.AddHours(3);
        var sesion = new SesionCaja(
            Guid.NewGuid(),
            EmpresaId,
            SedeId,
            PuntoVentaId,
            100m,
            fechaApertura: apertura);
        sesion.Cerrar(200m, cierre);
        var repos = CrearRepositorios();
        repos.SesionesCaja.Sesiones.Add(sesion);
        var ventas = new VentaRepositoryFake();
        ventas.Ventas.Add(CrearVenta(
            EmpresaId,
            PuntoVentaId,
            apertura.AddHours(1),
            60m,
            [(MetodoPago.TARJETA, 60m)]));
        ventas.Ventas.Add(CrearVenta(EmpresaId, PuntoVentaId, apertura.AddMinutes(-1), 10m));
        ventas.Ventas.Add(CrearVenta(EmpresaId, PuntoVentaId, cierre.AddMinutes(1), 10m));
        ventas.Ventas.Add(CrearVenta(EmpresaId, Guid.NewGuid(), apertura.AddHours(1), 10m));
        ventas.Ventas.Add(CrearVenta(OtraEmpresaId, PuntoVentaId, apertura.AddHours(1), 10m));
        ventas.Ventas.Add(CrearVenta(
            EmpresaId,
            PuntoVentaId,
            apertura.AddHours(1),
            10m,
            estado: EstadoVenta.Anulada));
        var useCase = new ObtenerResumenSesionCajaUseCase(
            repos.SesionesCaja,
            ventas,
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        var resumen = await useCase.EjecutarAsync(sesion.Id);

        Assert.NotNull(resumen);
        Assert.Equal("Cerrada", resumen.Estado);
        Assert.Equal(60m, resumen.TotalVentas);
        Assert.Equal(1, resumen.CantidadVentas);
        Assert.Equal(60m, resumen.TotalPagado);
        Assert.Equal(100m, resumen.DiferenciaCierre);
        Assert.Equal(40m, resumen.DiferenciaOperativa);
        var tarjeta = Assert.Single(
            resumen.PagosPorMetodo,
            item => item.MetodoPago == "TARJETA");
        Assert.Equal(60m, tarjeta.Total);
        Assert.Equal(1, tarjeta.CantidadPagos);
    }

    [Fact]
    public async Task Resumen_caja_ajena_no_se_expone()
    {
        var repos = CrearRepositorios();
        var sesionAjena = new SesionCaja(
            Guid.NewGuid(),
            OtraEmpresaId,
            SedeId,
            PuntoVentaId,
            10m);
        repos.SesionesCaja.Sesiones.Add(sesionAjena);
        var useCase = new ObtenerResumenSesionCajaUseCase(
            repos.SesionesCaja,
            new VentaRepositoryFake(),
            new EmpresaActivaContextFake(EmpresaId, UsuarioId));

        var resumen = await useCase.EjecutarAsync(sesionAjena.Id);

        Assert.Null(resumen);
    }

    private static Venta CrearVenta(
        Guid empresaId,
        Guid puntoVentaId,
        DateTimeOffset fechaCreacion,
        decimal total,
        IReadOnlyCollection<(MetodoPago Metodo, decimal Monto)>? pagos = null,
        EstadoVenta estado = EstadoVenta.Registrada)
    {
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            Guid.NewGuid(),
            1m,
            total,
            0m,
            total);
        var pagosVenta = (pagos ?? [(MetodoPago.EFECTIVO, total)])
            .Select(pago => new VentaPago(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                pago.Metodo,
                pago.Monto))
            .ToArray();

        return new Venta(
            ventaId,
            empresaId,
            fechaCreacion,
            total,
            0m,
            total,
            [detalle],
            SedeId,
            puntoVentaId,
            estado: estado,
            fechaCreacion: fechaCreacion,
            pagos: pagosVenta);
    }

    private static Repositorios CrearRepositorios(
        Guid? empresaPuntoVenta = null,
        bool puntoVentaActivo = true)
    {
        var puntosVenta = new PuntoVentaRepositoryFake();
        puntosVenta.PuntosVenta.Add(new PuntoVenta(
            PuntoVentaId,
            empresaPuntoVenta ?? EmpresaId,
            SedeId,
            "Caja 1",
            puntoVentaActivo));

        return new Repositorios(puntosVenta, new SesionCajaRepositoryFake());
    }

    private sealed record Repositorios(
        PuntoVentaRepositoryFake PuntosVenta,
        SesionCajaRepositoryFake SesionesCaja);

    private sealed class EmpresaActivaContextFake : IEmpresaActivaContext
    {
        public EmpresaActivaContextFake(
            Guid empresaId,
            Guid usuarioId,
            bool tieneEmpresaActiva = true)
        {
            EmpresaId = empresaId;
            UsuarioId = usuarioId;
            TieneEmpresaActiva = tieneEmpresaActiva;
        }

        public bool TieneEmpresaActiva { get; }

        public Guid UsuarioId { get; }

        public Guid EmpresaId { get; }

        public RolEmpresa Rol { get; } = RolEmpresa.Administrador;
    }

    private sealed class PuntoVentaRepositoryFake : IPuntoVentaRepository
    {
        public List<PuntoVenta> PuntosVenta { get; } = [];

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
                PuntosVenta.Where(puntoVenta =>
                    puntoVenta.EmpresaId == empresaId &&
                    puntoVenta.SedeId == sedeId).ToArray());
        }

        public Task<PuntoVenta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PuntosVenta.SingleOrDefault(puntoVenta =>
                puntoVenta.EmpresaId == empresaId &&
                puntoVenta.Id == id));
        }
    }

    private sealed class SesionCajaRepositoryFake : ISesionCajaRepository
    {
        public List<SesionCaja> Sesiones { get; } = [];

        public bool Guardado { get; private set; }

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
            return Task.FromResult(Sesiones.SingleOrDefault(sesion =>
                sesion.EmpresaId == empresaId &&
                sesion.Id == id));
        }

        public Task<SesionCaja?> ObtenerAbiertaPorPuntoVentaAsync(
            Guid empresaId,
            Guid puntoVentaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sesiones.SingleOrDefault(sesion =>
                sesion.EmpresaId == empresaId &&
                sesion.PuntoVentaId == puntoVentaId &&
                sesion.Estado == EstadoSesionCaja.Abierta));
        }

        public Task GuardarAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
        {
            Guardado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class VentaRepositoryFake : IVentaRepository
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
                    venta.EmpresaId == empresaId
                    && venta.Estado == EstadoVenta.Registrada
                    && venta.Fecha >= desde
                    && venta.Fecha < hastaExclusivo).ToArray());
        }

        public Task<Venta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Ventas.SingleOrDefault(venta => venta.EmpresaId == empresaId && venta.Id == id));
        }
    }
}
