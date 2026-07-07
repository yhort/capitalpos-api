using CapitalPos.Domain;

namespace CapitalPos.Application.Seguridad;

public interface IUsuarioCredencialRepository
{
    Task<UsuarioCredencial?> ObtenerPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}
