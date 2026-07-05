using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;

namespace CapitalPos.Infrastructure.Persistence.InMemory;

public sealed class InMemoryUsuarioEmpresaRepository : IUsuarioEmpresaRepository
{
    private readonly List<UsuarioEmpresa> _usuariosEmpresa = new();

    public IReadOnlyCollection<UsuarioEmpresa> UsuariosEmpresa => _usuariosEmpresa.AsReadOnly();

    public Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuarioEmpresa);

        _usuariosEmpresa.Add(usuarioEmpresa);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<UsuarioEmpresa>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UsuariosEmpresa);
    }
}
