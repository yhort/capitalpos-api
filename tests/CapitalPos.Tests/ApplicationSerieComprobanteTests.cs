using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Series;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationSerieComprobanteTests
{
    private static readonly Guid EmpresaId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OtraEmpresaId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SedeId = Guid.Parse("10000000-0000-0000-0000-000000000004");

    [Fact]
    public async Task Listar_series_filtra_por_empresa_activa_y_sede_activa()
    {
        var serieActiva = new SerieComprobante(Guid.NewGuid(), EmpresaId, SedeId, "03", "B001", 0);
        var serieInactiva = new SerieComprobante(Guid.NewGuid(), EmpresaId, SedeId, "01", "F001", 0, activa: false);
        var serieOtraEmpresa = new SerieComprobante(Guid.NewGuid(), OtraEmpresaId, SedeId, "03", "B001", 0);
        var repos = CrearRepositorios();
        repos.Series.Series.AddRange([serieInactiva, serieOtraEmpresa, serieActiva]);
        var useCase = new ListarSeriesComprobanteUseCase(
            repos.Series,
            repos.Sedes,
            new EmpresaActivaContextFake(EmpresaId));

        var series = await useCase.EjecutarAsync(SedeId);

        var serie = Assert.Single(series!);
        Assert.Equal(serieActiva.Id, serie.Id);
    }

    [Fact]
    public async Task Listar_series_devuelve_null_si_sede_no_pertenece_a_empresa()
    {
        var repos = CrearRepositorios();
        var useCase = new ListarSeriesComprobanteUseCase(
            repos.Series,
            repos.Sedes,
            new EmpresaActivaContextFake(OtraEmpresaId));

        var series = await useCase.EjecutarAsync(SedeId);

        Assert.Null(series);
    }

    [Fact]
    public async Task Obtener_serie_activa_normaliza_tipo_y_serie()
    {
        var serieEsperada = new SerieComprobante(Guid.NewGuid(), EmpresaId, SedeId, "03", "B001", 5);
        var repos = CrearRepositorios();
        repos.Series.Series.Add(serieEsperada);
        var useCase = new ObtenerSerieComprobanteActivaUseCase(
            repos.Series,
            repos.Sedes,
            new EmpresaActivaContextFake(EmpresaId));

        var serie = await useCase.EjecutarAsync(SedeId, " 03 ", " b001 ");

        Assert.NotNull(serie);
        Assert.Equal(serieEsperada.Id, serie.Id);
    }

    [Fact]
    public async Task Obtener_serie_activa_no_devuelve_series_inactivas()
    {
        var repos = CrearRepositorios();
        repos.Series.Series.Add(new SerieComprobante(Guid.NewGuid(), EmpresaId, SedeId, "03", "B001", 5, activa: false));
        var useCase = new ObtenerSerieComprobanteActivaUseCase(
            repos.Series,
            repos.Sedes,
            new EmpresaActivaContextFake(EmpresaId));

        var serie = await useCase.EjecutarAsync(SedeId, "03", "B001");

        Assert.Null(serie);
    }

    [Fact]
    public async Task Obtener_serie_activa_falla_sin_empresa_activa()
    {
        var repos = CrearRepositorios();
        var useCase = new ObtenerSerieComprobanteActivaUseCase(
            repos.Series,
            repos.Sedes,
            new EmpresaActivaContextFake(Guid.Empty, tieneEmpresaActiva: false));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(SedeId, "03", "B001"));
    }

    private static Repositorios CrearRepositorios()
    {
        var sedes = new SedeRepositoryFake();
        sedes.Sedes.Add(new Sede(SedeId, EmpresaId, "Tienda demo", TipoSede.TIENDA));

        return new Repositorios(sedes, new SerieComprobanteRepositoryFake());
    }

    private sealed record Repositorios(
        SedeRepositoryFake Sedes,
        SerieComprobanteRepositoryFake Series);

    private sealed class EmpresaActivaContextFake : IEmpresaActivaContext
    {
        public EmpresaActivaContextFake(Guid empresaId, bool tieneEmpresaActiva = true)
        {
            EmpresaId = empresaId;
            TieneEmpresaActiva = tieneEmpresaActiva;
        }

        public bool TieneEmpresaActiva { get; }

        public Guid UsuarioId { get; } = Guid.NewGuid();

        public Guid EmpresaId { get; }

        public RolEmpresa Rol { get; } = RolEmpresa.Administrador;
    }

    private sealed class SedeRepositoryFake : ISedeRepository
    {
        public List<Sede> Sedes { get; } = [];

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

    private sealed class SerieComprobanteRepositoryFake : ISerieComprobanteRepository
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
    }
}
