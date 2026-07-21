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
        await ObtenerOCrearUnidadesMedidaBasicasAsync(cancellationToken);
        var categoria = await ObtenerOCrearCategoriaAsync(empresa.Id, cancellationToken);
        var marca = await ObtenerOCrearMarcaAsync(empresa.Id, cancellationToken);
        var producto = await ObtenerOCrearProductoAsync(
            empresa.Id,
            categoria.Id,
            marca.Id,
            cancellationToken);
        await ObtenerOCrearStockProductoAsync(empresa.Id, sede.Id, producto.Id, cancellationToken);
        await ObtenerOCrearSerieComprobanteAsync(empresa.Id, sede.Id, cancellationToken);
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

    private async Task<Categoria> ObtenerOCrearCategoriaAsync(
        Guid empresaId,
        CancellationToken cancellationToken)
    {
        var categoria = await _store.ObtenerCategoriaAsync(
            empresaId,
            DemoSeedData.CategoriaNombre,
            cancellationToken);

        if (categoria is not null)
        {
            return categoria;
        }

        categoria = new Categoria(
            DemoSeedData.CategoriaId,
            empresaId,
            DemoSeedData.CategoriaNombre);
        await _store.AgregarCategoriaAsync(categoria, cancellationToken);

        return categoria;
    }

    private async Task<Marca> ObtenerOCrearMarcaAsync(
        Guid empresaId,
        CancellationToken cancellationToken)
    {
        var marca = await _store.ObtenerMarcaAsync(
            empresaId,
            DemoSeedData.MarcaNombre,
            cancellationToken);

        if (marca is not null)
        {
            return marca;
        }

        marca = new Marca(
            DemoSeedData.MarcaId,
            empresaId,
            DemoSeedData.MarcaNombre);
        await _store.AgregarMarcaAsync(marca, cancellationToken);

        return marca;
    }

    private async Task<Producto> ObtenerOCrearProductoAsync(
        Guid empresaId,
        Guid categoriaId,
        Guid marcaId,
        CancellationToken cancellationToken)
    {
        var producto = await _store.ObtenerProductoAsync(
            empresaId,
            DemoSeedData.ProductoId,
            cancellationToken);

        if (producto is not null)
        {
            return producto;
        }

        producto = new Producto(
            DemoSeedData.ProductoId,
            empresaId,
            DemoSeedData.ProductoNombre,
            DemoSeedData.ProductoPrecioVenta,
            DemoSeedData.ProductoCodigoSku,
            categoriaId: categoriaId,
            marcaId: marcaId,
            modoManejo: DemoSeedData.ProductoModoManejo);
        await _store.AgregarProductoAsync(producto, cancellationToken);

        return producto;
    }

    private async Task ObtenerOCrearUnidadesMedidaBasicasAsync(CancellationToken cancellationToken)
    {
        foreach (var unidad in DemoSeedData.UnidadesMedidaBasicas)
        {
            var existente = await _store.ObtenerUnidadMedidaAsync(unidad.Codigo, cancellationToken);
            if (existente is not null)
            {
                continue;
            }

            await _store.AgregarUnidadMedidaAsync(
                new UnidadMedida(unidad.Id, unidad.Codigo, unidad.Nombre),
                cancellationToken);
        }
    }

    private async Task<StockProducto> ObtenerOCrearStockProductoAsync(
        Guid empresaId,
        Guid sedeId,
        Guid productoId,
        CancellationToken cancellationToken)
    {
        var stock = await _store.ObtenerStockProductoAsync(
            empresaId,
            sedeId,
            productoId,
            cancellationToken);

        if (stock is not null)
        {
            return stock;
        }

        stock = new StockProducto(
            DemoSeedData.StockProductoId,
            empresaId,
            sedeId,
            productoId,
            null,
            DemoSeedData.StockProductoCantidadDisponible);
        await _store.AgregarStockProductoAsync(stock, cancellationToken);

        return stock;
    }

    private async Task<SerieComprobante> ObtenerOCrearSerieComprobanteAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken)
    {
        var serie = await _store.ObtenerSerieComprobanteAsync(
            empresaId,
            sedeId,
            DemoSeedData.SerieComprobanteTipo,
            DemoSeedData.SerieComprobanteSerie,
            cancellationToken);

        if (serie is not null)
        {
            return serie;
        }

        serie = new SerieComprobante(
            DemoSeedData.SerieComprobanteId,
            empresaId,
            sedeId,
            DemoSeedData.SerieComprobanteTipo,
            DemoSeedData.SerieComprobanteSerie,
            DemoSeedData.SerieComprobanteCorrelativoActual);
        await _store.AgregarSerieComprobanteAsync(serie, cancellationToken);

        return serie;
    }
}
