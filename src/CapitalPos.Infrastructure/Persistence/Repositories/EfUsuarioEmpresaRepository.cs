using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfUsuarioEmpresaRepository : IUsuarioEmpresaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfUsuarioEmpresaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuarioEmpresa);

        await _dbContext.UsuariosEmpresa.AddAsync(usuarioEmpresa, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UsuarioEmpresa>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UsuariosEmpresa
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<UsuarioEmpresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.UsuariosEmpresa
            .SingleOrDefaultAsync(usuarioEmpresa => usuarioEmpresa.Id == id, cancellationToken);
    }

    public Task<UsuarioEmpresa?> ObtenerPorUsuarioYEmpresaAsync(
        Guid usuarioId,
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UsuariosEmpresa
            .AsNoTracking()
            .SingleOrDefaultAsync(usuarioEmpresa =>
                usuarioEmpresa.UsuarioId == usuarioId &&
                usuarioEmpresa.EmpresaId == empresaId,
                cancellationToken);
    }

    public async Task ActualizarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuarioEmpresa);

        _dbContext.UsuariosEmpresa.Update(usuarioEmpresa);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExisteAsignacionAsync(
        Guid usuarioId,
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UsuariosEmpresa
            .AsNoTracking()
            .AnyAsync(usuarioEmpresa =>
                usuarioEmpresa.UsuarioId == usuarioId &&
                usuarioEmpresa.EmpresaId == empresaId,
                cancellationToken);
    }
}
