using CapitalPos.Domain;

namespace CapitalPos.Api.Development;

public interface IDemoSeedStore
{
    Task<Empresa?> ObtenerEmpresaPorRucAsync(string ruc, CancellationToken cancellationToken);

    Task<Usuario?> ObtenerUsuarioPorCorreoAsync(string correo, CancellationToken cancellationToken);

    Task<UsuarioEmpresa?> ObtenerUsuarioEmpresaAsync(
        Guid usuarioId,
        Guid empresaId,
        CancellationToken cancellationToken);

    Task<UsuarioCredencial?> ObtenerCredencialAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<Sede?> ObtenerSedeAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken);

    Task<PuntoVenta?> ObtenerPuntoVentaAsync(
        Guid empresaId,
        Guid puntoVentaId,
        CancellationToken cancellationToken);

    Task AgregarEmpresaAsync(Empresa empresa, CancellationToken cancellationToken);

    Task AgregarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken);

    Task AgregarUsuarioEmpresaAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken);

    Task AgregarCredencialAsync(UsuarioCredencial credencial, CancellationToken cancellationToken);

    Task AgregarSedeAsync(Sede sede, CancellationToken cancellationToken);

    Task AgregarPuntoVentaAsync(PuntoVenta puntoVenta, CancellationToken cancellationToken);

    Task GuardarCambiosAsync(CancellationToken cancellationToken);
}
