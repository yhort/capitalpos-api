using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public interface IUsuarioRepository
{
    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
