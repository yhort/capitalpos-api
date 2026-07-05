using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfUsuarioRepository : IUsuarioRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfUsuarioRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        await _dbContext.Usuarios.AddAsync(usuario, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Usuario>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Usuarios
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);
    }

    public async Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        _dbContext.Usuarios.Update(usuario);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default)
    {
        return _dbContext.Usuarios
            .AsNoTracking()
            .AnyAsync(usuario => usuario.Correo == correo, cancellationToken);
    }
}
