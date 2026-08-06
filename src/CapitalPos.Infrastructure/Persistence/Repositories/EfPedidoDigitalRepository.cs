using CapitalPos.Application.Pedidos;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfPedidoDigitalRepository : IPedidoDigitalRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfPedidoDigitalRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(PedidoDigital pedido, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        await _dbContext.PedidosDigitales.AddAsync(pedido, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PedidoDigital>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PedidosDigitales
            .AsNoTracking()
            .Include(pedido => pedido.Detalles)
            .Include(pedido => pedido.HistorialEstados)
            .Where(pedido => pedido.EmpresaId == empresaId)
            .OrderByDescending(pedido => pedido.FechaPedido)
            .ToListAsync(cancellationToken);
    }

    public Task<PedidoDigital?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PedidosDigitales
            .Include(pedido => pedido.Detalles)
            .Include(pedido => pedido.HistorialEstados)
            .SingleOrDefaultAsync(
                pedido => pedido.EmpresaId == empresaId && pedido.Id == id,
                cancellationToken);
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
