using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfUsuarioCredencialRepository : IUsuarioCredencialRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfUsuarioCredencialRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UsuarioCredencial?> ObtenerPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UsuariosCredenciales
            .AsNoTracking()
            .SingleOrDefaultAsync(credencial => credencial.UsuarioId == usuarioId, cancellationToken);
    }
}
