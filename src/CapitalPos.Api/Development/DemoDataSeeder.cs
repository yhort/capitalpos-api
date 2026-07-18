using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;
using Microsoft.Extensions.Options;

namespace CapitalPos.Api.Development;

public sealed class DemoDataSeeder
{
    private readonly IDemoSeedStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DemoDataSeeder> _logger;
    private readonly DemoSeedOptions _options;

    public DemoDataSeeder(
        IDemoSeedStore store,
        IPasswordHasher passwordHasher,
        IOptions<DemoSeedOptions> options,
        ILogger<DemoDataSeeder> logger)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _options = options.Value;
    }

    public async Task EjecutarAsync(
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        if (!_options.Enabled)
        {
            return;
        }

        var empresa = await ObtenerOCrearEmpresaAsync(cancellationToken);
        var usuario = await ObtenerOCrearUsuarioAsync(cancellationToken);
        await ObtenerOCrearRelacionAsync(usuario.Id, empresa.Id, cancellationToken);
        var sede = await ObtenerOCrearSedeAsync(empresa.Id, cancellationToken);
        await ObtenerOCrearPuntoVentaAsync(empresa.Id, sede.Id, cancellationToken);
        await CrearCredencialSiCorrespondeAsync(usuario.Id, cancellationToken);
        await _store.GuardarCambiosAsync(cancellationToken);

        _logger.LogInformation(
            "Seed demo de desarrollo verificado para empresa {EmpresaRuc} y usuario {UsuarioCorreo}.",
            DemoSeedData.EmpresaRuc,
            DemoSeedData.AdminCorreo);
    }

    private async Task<Empresa> ObtenerOCrearEmpresaAsync(CancellationToken cancellationToken)
    {
        var empresa = await _store.ObtenerEmpresaPorRucAsync(
            DemoSeedData.EmpresaRuc,
            cancellationToken);

        if (empresa is not null)
        {
            return empresa;
        }

        empresa = new Empresa(
            DemoSeedData.EmpresaId,
            DemoSeedData.EmpresaRuc,
            DemoSeedData.EmpresaRazonSocial,
            DemoSeedData.EmpresaNombreComercial);
        await _store.AgregarEmpresaAsync(empresa, cancellationToken);

        return empresa;
    }

    private async Task<Usuario> ObtenerOCrearUsuarioAsync(CancellationToken cancellationToken)
    {
        var usuario = await _store.ObtenerUsuarioPorCorreoAsync(
            DemoSeedData.AdminCorreo,
            cancellationToken);

        if (usuario is not null)
        {
            return usuario;
        }

        usuario = new Usuario(
            DemoSeedData.UsuarioId,
            DemoSeedData.AdminNombre,
            DemoSeedData.AdminApellido,
            DemoSeedData.AdminCorreo);
        await _store.AgregarUsuarioAsync(usuario, cancellationToken);

        return usuario;
    }

    private async Task ObtenerOCrearRelacionAsync(
        Guid usuarioId,
        Guid empresaId,
        CancellationToken cancellationToken)
    {
        var relacion = await _store.ObtenerUsuarioEmpresaAsync(
            usuarioId,
            empresaId,
            cancellationToken);

        if (relacion is not null)
        {
            return;
        }

        relacion = new UsuarioEmpresa(
            DemoSeedData.UsuarioEmpresaId,
            usuarioId,
            empresaId,
            DemoSeedData.AdminRol,
            activo: true);
        await _store.AgregarUsuarioEmpresaAsync(relacion, cancellationToken);
    }

    private async Task CrearCredencialSiCorrespondeAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var credencialExistente = await _store.ObtenerCredencialAsync(usuarioId, cancellationToken);
        if (credencialExistente is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.AdminPassword))
        {
            _logger.LogWarning(
                "DemoSeed esta habilitado, pero DemoSeed:AdminPassword no esta configurado. No se creara la credencial demo.");
            return;
        }

        var credencial = new UsuarioCredencial(
            usuarioId,
            "hash-pendiente",
            DemoSeedData.CredencialAlgoritmo);
        var hash = _passwordHasher.GenerarHash(credencial, _options.AdminPassword);
        credencial.CambiarPasswordHash(hash, DemoSeedData.CredencialAlgoritmo);

        await _store.AgregarCredencialAsync(credencial, cancellationToken);
    }

    private async Task<Sede> ObtenerOCrearSedeAsync(
        Guid empresaId,
        CancellationToken cancellationToken)
    {
        var sede = await _store.ObtenerSedeAsync(
            empresaId,
            DemoSeedData.SedeId,
            cancellationToken);

        if (sede is not null)
        {
            return sede;
        }

        sede = new Sede(
            DemoSeedData.SedeId,
            empresaId,
            DemoSeedData.SedeNombre,
            DemoSeedData.SedeTipo,
            DemoSeedData.SedeCodigoEstablecimiento,
            DemoSeedData.SedeDireccion,
            DemoSeedData.SedeDistrito,
            DemoSeedData.SedeProvincia,
            DemoSeedData.SedeDepartamento);
        await _store.AgregarSedeAsync(sede, cancellationToken);

        return sede;
    }

    private async Task<PuntoVenta> ObtenerOCrearPuntoVentaAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken)
    {
        var puntoVenta = await _store.ObtenerPuntoVentaAsync(
            empresaId,
            DemoSeedData.PuntoVentaId,
            cancellationToken);

        if (puntoVenta is not null)
        {
            return puntoVenta;
        }

        puntoVenta = new PuntoVenta(
            DemoSeedData.PuntoVentaId,
            empresaId,
            sedeId,
            DemoSeedData.PuntoVentaNombre);
        await _store.AgregarPuntoVentaAsync(puntoVenta, cancellationToken);

        return puntoVenta;
    }
}
