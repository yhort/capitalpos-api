using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfComprobanteRepository : IComprobanteRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfComprobanteRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(
        Comprobante comprobante,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comprobante);

        await _dbContext.Comprobantes.AddAsync(comprobante, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
