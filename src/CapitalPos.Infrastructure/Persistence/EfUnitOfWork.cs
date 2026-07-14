using CapitalPos.Application.Persistence;

namespace CapitalPos.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly CapitalPosDbContext _dbContext;

    public EfUnitOfWork(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
