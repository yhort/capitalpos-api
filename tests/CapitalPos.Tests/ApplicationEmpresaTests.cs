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

    private sealed class EmpresaRepositoryFake : IEmpresaRepository
    {
        public List<Empresa> Empresas { get; } = new();

        public Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            Empresas.Add(empresa);

            return Task.CompletedTask;
        }
    }
}
