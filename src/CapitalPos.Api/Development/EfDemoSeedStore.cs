using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Api.Development;

public sealed class EfDemoSeedStore : IDemoSeedStore
{
    private readonly CapitalPosDbContext _dbContext;

    public EfDemoSeedStore(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Empresa?> ObtenerEmpresaPorRucAsync(string ruc, CancellationToken cancellationToken)
    {
        return _dbContext.Empresas
            .SingleOrDefaultAsync(empresa => empresa.Ruc == ruc, cancellationToken);
    }

    public Task<Usuario?> ObtenerUsuarioPorCorreoAsync(string correo, CancellationToken cancellationToken)
    {
        return _dbContext.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Correo == correo, cancellationToken);
    }

    public Task<UsuarioEmpresa?> ObtenerUsuarioEmpresaAsync(
        Guid usuarioId,
        Guid empresaId,
        CancellationToken cancellationToken)
    {
        return _dbContext.UsuariosEmpresa
            .SingleOrDefaultAsync(usuarioEmpresa =>
                usuarioEmpresa.UsuarioId == usuarioId &&
                usuarioEmpresa.EmpresaId == empresaId,
                cancellationToken);
    }

    public Task<UsuarioCredencial?> ObtenerCredencialAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.UsuariosCredenciales
            .SingleOrDefaultAsync(credencial => credencial.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task AgregarEmpresaAsync(Empresa empresa, CancellationToken cancellationToken)
    {
        await _dbContext.Empresas.AddAsync(empresa, cancellationToken);
    }

    public async Task AgregarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        await _dbContext.Usuarios.AddAsync(usuario, cancellationToken);
    }

    public async Task AgregarUsuarioEmpresaAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken)
    {
        await _dbContext.UsuariosEmpresa.AddAsync(usuarioEmpresa, cancellationToken);
    }

    public async Task AgregarCredencialAsync(UsuarioCredencial credencial, CancellationToken cancellationToken)
    {
        await _dbContext.UsuariosCredenciales.AddAsync(credencial, cancellationToken);
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
