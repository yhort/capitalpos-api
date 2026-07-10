using CapitalPos.Domain;

namespace CapitalPos.Application.Clientes;

public interface IClienteRepository
{
    Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Cliente>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<Cliente?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        Cliente cliente,
        CancellationToken cancellationToken = default);
}
