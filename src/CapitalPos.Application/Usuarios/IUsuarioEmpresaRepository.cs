using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public interface IUsuarioEmpresaRepository
{
    Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UsuarioEmpresa>> ListarAsync(CancellationToken cancellationToken = default);

    Task<UsuarioEmpresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task ActualizarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default);
}
