using CapitalPos.Application.Empresas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationEmpresaTests
{
    [Fact]
    public async Task Crear_empresa_use_case_construye_y_guarda_empresa_valida()
    {
        var repository = new EmpresaRepositoryFake();
        var useCase = new CrearEmpresaUseCase(repository);
        var request = new CrearEmpresaRequest(
            "20606264004",
            "CapitalPOS SAC",
            "CapitalPOS");

        var empresa = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, empresa.Id);
        Assert.Equal("20606264004", empresa.Ruc);
        Assert.Equal("CapitalPOS SAC", empresa.RazonSocial);
        Assert.Equal("CapitalPOS", empresa.NombreComercial);
        Assert.True(empresa.Activa);
        Assert.Same(empresa, repository.Empresas.Single());
    }

    [Fact]
    public async Task Crear_empresa_use_case_propaga_reglas_de_dominio()
    {
        var repository = new EmpresaRepositoryFake();
        var useCase = new CrearEmpresaUseCase(repository);
        var request = new CrearEmpresaRequest("123", "CapitalPOS SAC");

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Empresas);
    }

    [Fact]
    public async Task Listar_empresas_use_case_devuelve_empresas_guardadas()
    {
        var repository = new EmpresaRepositoryFake();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC");
        await repository.AgregarAsync(empresa);
        var useCase = new ListarEmpresasUseCase(repository);

        var empresas = await useCase.EjecutarAsync();

        Assert.Same(empresa, empresas.Single());
    }

    [Fact]
    public async Task Obtener_empresa_por_id_use_case_devuelve_empresa_guardada()
    {
        var repository = new EmpresaRepositoryFake();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC");
        await repository.AgregarAsync(empresa);
        var useCase = new ObtenerEmpresaPorIdUseCase(repository);

        var empresaEncontrada = await useCase.EjecutarAsync(empresa.Id);

        Assert.Same(empresa, empresaEncontrada);
    }

    [Fact]
    public async Task Obtener_empresa_por_id_use_case_devuelve_null_si_no_existe()
    {
        var repository = new EmpresaRepositoryFake();
        var useCase = new ObtenerEmpresaPorIdUseCase(repository);

        var empresa = await useCase.EjecutarAsync(Guid.NewGuid());

        Assert.Null(empresa);
    }

    [Fact]
    public async Task Desactivar_empresa_use_case_cambia_estado_y_actualiza_repositorio()
    {
        var repository = new EmpresaRepositoryFake();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC");
        await repository.AgregarAsync(empresa);
        var useCase = new DesactivarEmpresaUseCase(repository);

        var empresaDesactivada = await useCase.EjecutarAsync(empresa.Id);

        Assert.Same(empresa, empresaDesactivada);
        Assert.False(empresa.Activa);
        Assert.Same(empresa, repository.EmpresaActualizada);
    }

    [Fact]
    public async Task Activar_empresa_use_case_cambia_estado_y_actualiza_repositorio()
    {
        var repository = new EmpresaRepositoryFake();
        var empresa = new Empresa(
            Guid.NewGuid(),
            "20606264004",
            "CapitalPOS SAC",
            activa: false);
        await repository.AgregarAsync(empresa);
        var useCase = new ActivarEmpresaUseCase(repository);

        var empresaActivada = await useCase.EjecutarAsync(empresa.Id);

        Assert.Same(empresa, empresaActivada);
        Assert.True(empresa.Activa);
        Assert.Same(empresa, repository.EmpresaActualizada);
    }

    [Fact]
    public async Task Desactivar_empresa_use_case_devuelve_null_si_no_existe()
    {
        var repository = new EmpresaRepositoryFake();
        var useCase = new DesactivarEmpresaUseCase(repository);

        var empresa = await useCase.EjecutarAsync(Guid.NewGuid());

        Assert.Null(empresa);
        Assert.Null(repository.EmpresaActualizada);
    }

    private sealed class EmpresaRepositoryFake : IEmpresaRepository
    {
        public List<Empresa> Empresas { get; } = new();

        public Empresa? EmpresaActualizada { get; private set; }

        public Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            Empresas.Add(empresa);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Empresa>> ListarAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Empresa>>(Empresas);
        }

        public Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var empresa = Empresas.SingleOrDefault(empresa => empresa.Id == id);

            return Task.FromResult(empresa);
        }

        public Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            EmpresaActualizada = empresa;

            return Task.CompletedTask;
        }
    }
}
