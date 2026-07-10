using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationProductoVarianteTests
{
    [Fact]
    public async Task Crear_variante_use_case_asigna_empresa_id_desde_contexto_activo()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repository = new ProductoVarianteRepositoryFake();
        var empresaActiva = new EmpresaActivaContextFake(empresaId);
        var useCase = new CrearProductoVarianteUseCase(repository, empresaActiva);
        var request = new CrearProductoVarianteRequest(
            productoId,
            " M ",
            " Azul ",
            " SKU-AZ-M ",
            " 7750000000104 ",
            12);

        var variante = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, variante.Id);
        Assert.Equal(empresaId, variante.EmpresaId);
        Assert.Equal(productoId, variante.ProductoId);
        Assert.Equal("M", variante.Talla);
        Assert.Equal("Azul", variante.Color);
        Assert.Equal("SKU-AZ-M", variante.CodigoSku);
        Assert.Equal("7750000000104", variante.CodigoBarras);
        Assert.Equal(12, variante.StockActual);
        Assert.Same(variante, repository.Variantes.Single());
    }

    [Fact]
    public async Task Crear_variante_use_case_falla_si_no_hay_empresa_activa()
    {
        var repository = new ProductoVarianteRepositoryFake();
        var useCase = new CrearProductoVarianteUseCase(
            repository,
            new EmpresaActivaContextFake());
        var request = new CrearProductoVarianteRequest(
            Guid.NewGuid(),
            Talla: "M");

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Variantes);
    }

    [Fact]
    public async Task Crear_variante_use_case_propaga_validaciones_de_dominio()
    {
        var repository = new ProductoVarianteRepositoryFake();
        var useCase = new CrearProductoVarianteUseCase(
            repository,
            new EmpresaActivaContextFake(Guid.NewGuid()));
        var request = new CrearProductoVarianteRequest(
            Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Variantes);
    }

    [Fact]
    public async Task Listar_variantes_use_case_lista_solo_producto_y_empresa_activa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoAId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var repository = new ProductoVarianteRepositoryFake();
        var varianteEmpresaA = new ProductoVariante(
            Guid.NewGuid(),
            empresaAId,
            productoAId,
            talla: "M");
        var varianteOtroProducto = new ProductoVariante(
            Guid.NewGuid(),
            empresaAId,
            otroProductoId,
            talla: "L");
        var varianteEmpresaB = new ProductoVariante(
            Guid.NewGuid(),
            empresaBId,
            productoAId,
            talla: "S");
        await repository.AgregarAsync(varianteEmpresaA);
        await repository.AgregarAsync(varianteOtroProducto);
        await repository.AgregarAsync(varianteEmpresaB);
        var useCase = new ListarProductoVariantesUseCase(
            repository,
            new EmpresaActivaContextFake(empresaAId));

        var variantes = await useCase.EjecutarAsync(productoAId);

        Assert.Same(varianteEmpresaA, Assert.Single(variantes));
    }

    [Fact]
    public async Task Listar_variantes_use_case_falla_si_no_hay_empresa_activa()
    {
        var repository = new ProductoVarianteRepositoryFake();
        var useCase = new ListarProductoVariantesUseCase(
            repository,
            new EmpresaActivaContextFake());

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(Guid.NewGuid()));
    }

    private sealed class ProductoVarianteRepositoryFake : IProductoVarianteRepository
    {
        public List<ProductoVariante> Variantes { get; } = new();

        public Task AgregarAsync(
            ProductoVariante variante,
            CancellationToken cancellationToken = default)
        {
            Variantes.Add(variante);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ProductoVariante> variantes = Variantes
                .Where(variante => variante.EmpresaId == empresaId && variante.ProductoId == productoId)
                .ToArray();

            return Task.FromResult(variantes);
        }

        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var variante = Variantes.SingleOrDefault(variante =>
                variante.EmpresaId == empresaId && variante.Id == id);

            return Task.FromResult(variante);
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
