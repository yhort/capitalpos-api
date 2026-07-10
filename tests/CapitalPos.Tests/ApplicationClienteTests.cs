using CapitalPos.Application.Clientes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationClienteTests
{
    [Fact]
    public async Task Crear_cliente_use_case_asigna_empresa_id_desde_contexto_activo()
    {
        var empresaId = Guid.NewGuid();
        var repository = new ClienteRepositoryFake();
        var useCase = new CrearClienteUseCase(
            repository,
            new EmpresaActivaContextFake(empresaId));
        var request = new CrearClienteRequest(
            " dni ",
            " 12345678 ",
            " Juan Perez ",
            " Av. Lima 123 ");

        var cliente = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, cliente.Id);
        Assert.Equal(empresaId, cliente.EmpresaId);
        Assert.Equal("DNI", cliente.TipoDocumento);
        Assert.Equal("12345678", cliente.NumeroDocumento);
        Assert.Equal("Juan Perez", cliente.NombreRazonSocial);
        Assert.Equal("Av. Lima 123", cliente.Direccion);
        Assert.Same(cliente, repository.Clientes.Single());
    }

    [Fact]
    public async Task Crear_cliente_use_case_falla_si_no_hay_empresa_activa()
    {
        var repository = new ClienteRepositoryFake();
        var useCase = new CrearClienteUseCase(
            repository,
            new EmpresaActivaContextFake());
        var request = new CrearClienteRequest("DNI", "12345678", "Juan Perez");

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Clientes);
    }

    [Fact]
    public async Task Listar_clientes_use_case_lista_solo_empresa_activa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var repository = new ClienteRepositoryFake();
        var clienteEmpresaA = new Cliente(
            Guid.NewGuid(),
            empresaAId,
            "DNI",
            "12345678",
            "Juan Perez");
        var clienteEmpresaB = new Cliente(
            Guid.NewGuid(),
            empresaBId,
            "DNI",
            "87654321",
            "Ana Torres");
        await repository.AgregarAsync(clienteEmpresaA);
        await repository.AgregarAsync(clienteEmpresaB);
        var useCase = new ListarClientesUseCase(
            repository,
            new EmpresaActivaContextFake(empresaAId));

        var clientes = await useCase.EjecutarAsync();

        Assert.Same(clienteEmpresaA, Assert.Single(clientes));
    }

    [Fact]
    public async Task Obtener_cliente_por_id_use_case_no_devuelve_cliente_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var repository = new ClienteRepositoryFake();
        await repository.AgregarAsync(new Cliente(
            clienteId,
            empresaBId,
            "DNI",
            "87654321",
            "Ana Torres"));
        var useCase = new ObtenerClientePorIdUseCase(
            repository,
            new EmpresaActivaContextFake(empresaAId));

        var cliente = await useCase.EjecutarAsync(clienteId);

        Assert.Null(cliente);
    }

    private sealed class ClienteRepositoryFake : IClienteRepository
    {
        public List<Cliente> Clientes { get; } = new();

        public Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            Clientes.Add(cliente);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Cliente>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Cliente> clientes = Clientes
                .Where(cliente => cliente.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(clientes);
        }

        public Task<Cliente?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var cliente = Clientes.SingleOrDefault(cliente =>
                cliente.EmpresaId == empresaId && cliente.Id == id);

            return Task.FromResult(cliente);
        }

        public Task ActualizarAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
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
