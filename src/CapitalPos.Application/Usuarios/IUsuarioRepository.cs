using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public interface IUsuarioRepository
{
    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Usuario>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
