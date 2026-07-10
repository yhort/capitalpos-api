using CapitalPos.Application.Clientes;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfClienteRepository : IClienteRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfClienteRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        await _dbContext.Clientes.AddAsync(cliente, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Cliente>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clientes
            .AsNoTracking()
            .Where(cliente => cliente.EmpresaId == empresaId)
            .OrderBy(cliente => cliente.NombreRazonSocial)
            .ToListAsync(cancellationToken);
    }

    public Task<Cliente?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Clientes
            .SingleOrDefaultAsync(
                cliente => cliente.EmpresaId == empresaId && cliente.Id == id,
                cancellationToken);
    }

    public async Task ActualizarAsync(
        Cliente cliente,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        _dbContext.Clientes.Update(cliente);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
