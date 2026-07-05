using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;

namespace CapitalPos.Infrastructure.Persistence.InMemory;

public sealed class InMemoryUsuarioRepository : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = new();

    public IReadOnlyCollection<Usuario> Usuarios => _usuarios.AsReadOnly();

    public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        _usuarios.Add(usuario);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Usuario>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Usuarios);
    }

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = _usuarios.SingleOrDefault(usuario => usuario.Id == id);

        return Task.FromResult(usuario);
    }
}
