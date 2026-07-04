using CapitalPos.Application.Empresas;

namespace CapitalPos.Tests;

public class ApplicationEmpresaTests
{
    [Fact]
    public void Crear_empresa_use_case_construye_empresa_valida()
    {
        var useCase = new CrearEmpresaUseCase();
        var request = new CrearEmpresaRequest(
            "20606264004",
            "CapitalPOS SAC",
            "CapitalPOS");

        var empresa = useCase.Ejecutar(request);

        Assert.NotEqual(Guid.Empty, empresa.Id);
        Assert.Equal("20606264004", empresa.Ruc);
        Assert.Equal("CapitalPOS SAC", empresa.RazonSocial);
        Assert.Equal("CapitalPOS", empresa.NombreComercial);
        Assert.True(empresa.Activa);
    }

    [Fact]
    public void Crear_empresa_use_case_propaga_reglas_de_dominio()
    {
        var useCase = new CrearEmpresaUseCase();
        var request = new CrearEmpresaRequest("123", "CapitalPOS SAC");

        Assert.Throws<ArgumentException>(() => useCase.Ejecutar(request));
    }
}
