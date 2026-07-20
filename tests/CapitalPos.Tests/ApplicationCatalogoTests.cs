using CapitalPos.Application.Catalogo;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationCatalogoTests
{
    [Fact]
    public async Task Crear_categoria_use_case_asigna_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var repository = new CategoriaRepositoryFake();
        var useCase = new CrearCategoriaUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var categoria = await useCase.EjecutarAsync(new CrearCategoriaRequest(" Polos "));

        Assert.Equal(empresaId, categoria.EmpresaId);
        Assert.Equal("Polos", categoria.Nombre);
        Assert.Null(categoria.CategoriaPadreId);
        Assert.Same(categoria, repository.Categorias.Single());
    }

    [Fact]
    public async Task Crear_categoria_use_case_permita_un_nivel_de_subcategoria()
    {
        var empresaId = Guid.NewGuid();
        var padre = new Categoria(Guid.NewGuid(), empresaId, "Ropa");
        var repository = new CategoriaRepositoryFake();
        await repository.AgregarAsync(padre);
        var useCase = new CrearCategoriaUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var categoria = await useCase.EjecutarAsync(new CrearCategoriaRequest("Polos", padre.Id));

        Assert.Equal(padre.Id, categoria.CategoriaPadreId);
    }

    [Fact]
    public async Task Crear_categoria_use_case_rechaza_mas_de_un_nivel()
    {
        var empresaId = Guid.NewGuid();
        var abueloId = Guid.NewGuid();
        var padre = new Categoria(Guid.NewGuid(), empresaId, "Polos", abueloId);
        var repository = new CategoriaRepositoryFake();
        await repository.AgregarAsync(padre);
        var useCase = new CrearCategoriaUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(new CrearCategoriaRequest("Manga corta", padre.Id)));

        Assert.Contains("un nivel", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Listar_categorias_use_case_filtra_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var repository = new CategoriaRepositoryFake();
        await repository.AgregarAsync(new Categoria(Guid.NewGuid(), empresaId, "Polos"));
        await repository.AgregarAsync(new Categoria(Guid.NewGuid(), otraEmpresaId, "Ajena"));
        var useCase = new ListarCategoriasUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var categorias = await useCase.EjecutarAsync();

        Assert.Equal("Polos", Assert.Single(categorias).Nombre);
    }

    [Fact]
    public async Task Crear_marca_use_case_asigna_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var repository = new MarcaRepositoryFake();
        var useCase = new CrearMarcaUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var marca = await useCase.EjecutarAsync(new CrearMarcaRequest(" Brooklyn "));

        Assert.Equal(empresaId, marca.EmpresaId);
        Assert.Equal("Brooklyn", marca.Nombre);
        Assert.Same(marca, repository.Marcas.Single());
    }

    [Fact]
    public async Task Listar_marcas_use_case_filtra_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var repository = new MarcaRepositoryFake();
        await repository.AgregarAsync(new Marca(Guid.NewGuid(), empresaId, "Brooklyn"));
        await repository.AgregarAsync(new Marca(Guid.NewGuid(), otraEmpresaId, "Ajena"));
        var useCase = new ListarMarcasUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));

        var marcas = await useCase.EjecutarAsync();

        Assert.Equal("Brooklyn", Assert.Single(marcas).Nombre);
    }

    private sealed class CategoriaRepositoryFake : ICategoriaRepository
    {
        public List<Categoria> Categorias { get; } = new();

        public Task AgregarAsync(Categoria categoria, CancellationToken cancellationToken = default)
        {
            Categorias.Add(categoria);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Categoria>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Categoria>>(
                Categorias.Where(categoria => categoria.EmpresaId == empresaId).ToArray());
        }

        public Task<Categoria?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Categorias.SingleOrDefault(categoria =>
                categoria.EmpresaId == empresaId &&
                categoria.Id == id));
        }
    }

    private sealed class MarcaRepositoryFake : IMarcaRepository
    {
        public List<Marca> Marcas { get; } = new();

        public Task AgregarAsync(Marca marca, CancellationToken cancellationToken = default)
        {
            Marcas.Add(marca);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Marca>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Marca>>(
                Marcas.Where(marca => marca.EmpresaId == empresaId).ToArray());
        }

        public Task<Marca?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Marcas.SingleOrDefault(marca =>
                marca.EmpresaId == empresaId &&
                marca.Id == id));
        }
    }

    private sealed class EmpresaActivaContextFake : IEmpresaActivaContext
    {
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
