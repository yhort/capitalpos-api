using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationConfiguracionFiscalEmpresaTests
{
    [Fact]
    public async Task Guardar_configuracion_fiscal_crea_configuracion_para_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var repository = new ConfiguracionFiscalEmpresaRepositoryFake();
        var useCase = new GuardarConfiguracionFiscalEmpresaUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));
        var request = CrearRequest();

        var configuracion = await useCase.EjecutarAsync(request);

        Assert.Equal(empresaId, configuracion.EmpresaId);
        Assert.Equal("20600000001", configuracion.Ruc);
        Assert.Equal("CapitalPOS Demo SAC", configuracion.RazonSocial);
        Assert.True(configuracion.Activa);
        Assert.Same(configuracion, repository.Configuraciones.Single());
    }

    [Fact]
    public async Task Guardar_configuracion_fiscal_actualiza_configuracion_existente()
    {
        var empresaId = Guid.NewGuid();
        var repository = new ConfiguracionFiscalEmpresaRepositoryFake();
        var existente = new ConfiguracionFiscalEmpresa(
            empresaId,
            "20600000001",
            "CapitalPOS Demo SAC",
            "CapitalPOS",
            "150101",
            "Av. Demo 123",
            "Lima",
            "Lima",
            "Lima");
        await repository.GuardarAsync(existente);
        var useCase = new GuardarConfiguracionFiscalEmpresaUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));
        var request = new GuardarConfiguracionFiscalEmpresaRequest(
            "20601234567",
            "CapitalPOS Beta SAC",
            "CapitalPOS Beta",
            "150102",
            "Calle Beta 456",
            "LIMA",
            "LIMA",
            "ANCON",
            Activa: false);

        var configuracion = await useCase.EjecutarAsync(request);

        Assert.Same(existente, configuracion);
        Assert.Equal("20601234567", configuracion.Ruc);
        Assert.Equal("CapitalPOS Beta SAC", configuracion.RazonSocial);
        Assert.Equal("150102", configuracion.Ubigeo);
        Assert.False(configuracion.Activa);
        Assert.Single(repository.Configuraciones);
    }

    [Fact]
    public async Task Guardar_configuracion_fiscal_falla_si_no_hay_empresa_activa()
    {
        var repository = new ConfiguracionFiscalEmpresaRepositoryFake();
        var useCase = new GuardarConfiguracionFiscalEmpresaUseCase(
            repository,
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(CrearRequest()));
        Assert.Empty(repository.Configuraciones);
    }

    [Fact]
    public async Task Obtener_configuracion_fiscal_usa_empresa_activa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var repository = new ConfiguracionFiscalEmpresaRepositoryFake();
        var configuracionA = CrearConfiguracion(empresaAId);
        var configuracionB = CrearConfiguracion(empresaBId);
        await repository.GuardarAsync(configuracionA);
        await repository.GuardarAsync(configuracionB);
        var useCase = new ObtenerConfiguracionFiscalEmpresaUseCase(
            repository,
            new EmpresaActivaContextFake(empresaAId));

        var configuracion = await useCase.EjecutarAsync();

        Assert.Same(configuracionA, configuracion);
    }

    [Fact]
    public async Task Obtener_configuracion_fiscal_falla_si_no_hay_empresa_activa()
    {
        var useCase = new ObtenerConfiguracionFiscalEmpresaUseCase(
            new ConfiguracionFiscalEmpresaRepositoryFake(),
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync());
    }

    private static GuardarConfiguracionFiscalEmpresaRequest CrearRequest()
    {
        return new GuardarConfiguracionFiscalEmpresaRequest(
            "20600000001",
            "CapitalPOS Demo SAC",
            "CapitalPOS",
            "150101",
            "Av. Demo 123",
            "Lima",
            "Lima",
            "Lima");
    }

    private static ConfiguracionFiscalEmpresa CrearConfiguracion(Guid empresaId)
    {
        return new ConfiguracionFiscalEmpresa(
            empresaId,
            "20600000001",
            "CapitalPOS Demo SAC",
            "CapitalPOS",
            "150101",
            "Av. Demo 123",
            "Lima",
            "Lima",
            "Lima");
    }

    private sealed class ConfiguracionFiscalEmpresaRepositoryFake : IConfiguracionFiscalEmpresaRepository
    {
        public List<ConfiguracionFiscalEmpresa> Configuraciones { get; } = new();

        public Task<ConfiguracionFiscalEmpresa?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            var configuracion = Configuraciones.SingleOrDefault(
                configuracion => configuracion.EmpresaId == empresaId);

            return Task.FromResult(configuracion);
        }

        public Task GuardarAsync(
            ConfiguracionFiscalEmpresa configuracion,
            CancellationToken cancellationToken = default)
        {
            var index = Configuraciones.FindIndex(
                actual => actual.EmpresaId == configuracion.EmpresaId);
            if (index >= 0)
            {
                Configuraciones[index] = configuracion;
            }
            else
            {
                Configuraciones.Add(configuracion);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class EmpresaActivaContextFake : IEmpresaActivaContext
    {
        public EmpresaActivaContextFake()
        {
        }

        public EmpresaActivaContextFake(Guid empresaId)
        {
            UsuarioId = Guid.NewGuid();
            EmpresaId = empresaId;
            Rol = RolEmpresa.Administrador;
            TieneEmpresaActiva = true;
        }

        public bool TieneEmpresaActiva { get; }

        public Guid UsuarioId { get; }

        public Guid EmpresaId { get; }

        public RolEmpresa Rol { get; }
    }
}
