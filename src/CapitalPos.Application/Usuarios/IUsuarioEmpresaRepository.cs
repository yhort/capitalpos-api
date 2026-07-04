using CapitalPos.Domain;

namespace CapitalPos.Application.Usuarios;

public interface IUsuarioEmpresaRepository
{
    Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default);
}
