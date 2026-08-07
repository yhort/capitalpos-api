using CapitalPos.Api.Development;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CapitalPos.Tests;

public class DemoDataSeederTests
{
    private const string PasswordDemo = "password-demo-local-no-real";

    [Fact]
    public async Task Seed_no_se_ejecuta_en_production()
    {
        var store = new DemoSeedStoreFake();
        var seeder = CrearSeeder(store, enabled: true, adminPassword: PasswordDemo);

        await seeder.EjecutarAsync(new HostEnvironmentFake("Production"));

        Assert.Empty(store.Empresas);
        Assert.Empty(store.Usuarios);
        Assert.Empty(store.Relaciones);
        Assert.Empty(store.Credenciales);
        Assert.Empty(store.Sedes);
        Assert.Empty(store.PuntosVenta);
        Assert.Empty(store.Categorias);
        Assert.Empty(store.Marcas);
        Assert.Empty(store.UnidadesMedida);
        Assert.Empty(store.Productos);
        Assert.Empty(store.Stocks);
        Assert.Empty(store.SeriesComprobante);
        Assert.Equal(0, store.SaveChangesCount);
    }

    [Fact]
    public async Task Seed_no_se_ejecuta_si_enabled_es_false()
    {
        var store = new DemoSeedStoreFake();
        var seeder = CrearSeeder(store, enabled: false, adminPassword: PasswordDemo);

        await seeder.EjecutarAsync(new HostEnvironmentFake("Development"));

        Assert.Empty(store.Empresas);
        Assert.Empty(store.Usuarios);
        Assert.Empty(store.Relaciones);
        Assert.Empty(store.Credenciales);
        Assert.Empty(store.Sedes);
        Assert.Empty(store.PuntosVenta);
        Assert.Empty(store.Categorias);
        Assert.Empty(store.Marcas);
        Assert.Empty(store.UnidadesMedida);
        Assert.Empty(store.Productos);
        Assert.Empty(store.Stocks);
        Assert.Empty(store.SeriesComprobante);
        Assert.Equal(0, store.SaveChangesCount);
    }

    [Fact]
    public async Task Seed_crea_empresa_usuario_relacion_activa_y_credencial_hasheada()
    {
        var store = new DemoSeedStoreFake();
        var logger = new DemoSeedLoggerFake();
        var seeder = CrearSeeder(store, enabled: true, adminPassword: PasswordDemo, logger);

        await seeder.EjecutarAsync(new HostEnvironmentFake("Development"));

        var empresa = Assert.Single(store.Empresas);
        Assert.Equal(DemoSeedData.EmpresaRuc, empresa.Ruc);
        Assert.Equal(DemoSeedData.EmpresaRazonSocial, empresa.RazonSocial);

        var usuario = Assert.Single(store.Usuarios);
        Assert.Equal(DemoSeedData.AdminCorreo, usuario.Correo);

        var relacion = Assert.Single(store.Relaciones);
        Assert.Equal(usuario.Id, relacion.UsuarioId);
        Assert.Equal(empresa.Id, relacion.EmpresaId);
        Assert.Equal(RolEmpresa.Administrador, relacion.Rol);
        Assert.True(relacion.Activo);

        var sede = Assert.Single(store.Sedes);
        Assert.Equal(empresa.Id, sede.EmpresaId);
        Assert.Equal(DemoSeedData.SedeNombre, sede.Nombre);
        Assert.Equal(TipoSede.TIENDA, sede.Tipo);
        Assert.True(sede.Activa);

        var puntoVenta = Assert.Single(store.PuntosVenta);
        Assert.Equal(empresa.Id, puntoVenta.EmpresaId);
        Assert.Equal(sede.Id, puntoVenta.SedeId);
        Assert.Equal(DemoSeedData.PuntoVentaNombre, puntoVenta.Nombre);
        Assert.True(puntoVenta.Activo);

        var categoria = Assert.Single(store.Categorias);
        Assert.Equal(empresa.Id, categoria.EmpresaId);
        Assert.Equal(DemoSeedData.CategoriaNombre, categoria.Nombre);
        Assert.Null(categoria.CategoriaPadreId);
        Assert.True(categoria.Activa);

        var marca = Assert.Single(store.Marcas);
        Assert.Equal(empresa.Id, marca.EmpresaId);
        Assert.Equal(DemoSeedData.MarcaNombre, marca.Nombre);
        Assert.True(marca.Activa);

        Assert.Equal(5, store.UnidadesMedida.Count);
        Assert.Contains(store.UnidadesMedida, unidad => unidad.Codigo == "UND");
        Assert.Contains(store.UnidadesMedida, unidad => unidad.Codigo == "CAJ");
        Assert.Contains(store.UnidadesMedida, unidad => unidad.Codigo == "PAQ");
        Assert.Contains(store.UnidadesMedida, unidad => unidad.Codigo == "DOC");
        Assert.Contains(store.UnidadesMedida, unidad => unidad.Codigo == "KG");

        var producto = Assert.Single(store.Productos);
        Assert.Equal(empresa.Id, producto.EmpresaId);
        Assert.Equal(DemoSeedData.ProductoNombre, producto.Nombre);
        Assert.Equal(DemoSeedData.ProductoCodigoSku, producto.CodigoSku);
        Assert.Equal(categoria.Id, producto.CategoriaId);
        Assert.Equal(marca.Id, producto.MarcaId);
        Assert.Equal(DemoSeedData.ProductoModoManejo, producto.ModoManejo);

        var stock = Assert.Single(store.Stocks);
        Assert.Equal(empresa.Id, stock.EmpresaId);
        Assert.Equal(sede.Id, stock.SedeId);
        Assert.Equal(producto.Id, stock.ProductoId);
        Assert.Null(stock.ProductoVarianteId);
        Assert.Equal(DemoSeedData.StockProductoCantidadDisponible, stock.CantidadDisponible);

        var series = store.SeriesComprobante.OrderBy(s => s.Serie).ToArray();
        Assert.Equal(3, series.Length);
        Assert.Contains(series, s =>
            s.TipoComprobante == DemoSeedData.SerieComprobanteTipo &&
            s.Serie == DemoSeedData.SerieComprobanteSerie);
        Assert.Contains(series, s =>
            s.TipoComprobante == DemoSeedData.SerieNotaCreditoTipo &&
            s.Serie == DemoSeedData.SerieNotaCreditoBoletaSerie);
        Assert.Contains(series, s =>
            s.TipoComprobante == DemoSeedData.SerieNotaCreditoTipo &&
            s.Serie == DemoSeedData.SerieNotaCreditoFacturaSerie);
        Assert.All(series, s =>
        {
            Assert.Equal(empresa.Id, s.EmpresaId);
            Assert.Equal(sede.Id, s.SedeId);
            Assert.Equal(DemoSeedData.SerieComprobanteCorrelativoActual, s.CorrelativoActual);
            Assert.True(s.Activa);
        });

        var credencial = Assert.Single(store.Credenciales);
        Assert.Equal(usuario.Id, credencial.UsuarioId);
        Assert.NotEqual(PasswordDemo, credencial.PasswordHash);
        Assert.StartsWith("hash-seguro:", credencial.PasswordHash, StringComparison.Ordinal);
        Assert.Equal(DemoSeedData.CredencialAlgoritmo, credencial.Algoritmo);
        Assert.Equal(1, store.SaveChangesCount);
        Assert.DoesNotContain(logger.Messages, message => message.Contains(PasswordDemo, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Seed_no_crea_credencial_si_falta_password_y_loguea_mensaje_seguro()
    {
        var store = new DemoSeedStoreFake();
        var logger = new DemoSeedLoggerFake();
        var seeder = CrearSeeder(store, enabled: true, adminPassword: "", logger);

        await seeder.EjecutarAsync(new HostEnvironmentFake("Development"));

        Assert.Single(store.Empresas);
        Assert.Single(store.Usuarios);
        Assert.Single(store.Relaciones);
        Assert.Single(store.Sedes);
        Assert.Single(store.PuntosVenta);
        Assert.Single(store.Categorias);
        Assert.Single(store.Marcas);
        Assert.Equal(5, store.UnidadesMedida.Count);
        Assert.Single(store.Productos);
        Assert.Single(store.Stocks);
        Assert.Equal(3, store.SeriesComprobante.Count);
        Assert.Empty(store.Credenciales);
        Assert.Contains(logger.Messages, message => message.Contains("AdminPassword no esta configurado", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("password", StringComparison.Ordinal) &&
            message.Contains("=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Seed_es_idempotente_y_no_recrea_credencial_existente()
    {
        var store = new DemoSeedStoreFake();
        var seeder = CrearSeeder(store, enabled: true, adminPassword: PasswordDemo);

        await seeder.EjecutarAsync(new HostEnvironmentFake("Development"));
        var credencialOriginal = Assert.Single(store.Credenciales);
        var hashOriginal = credencialOriginal.PasswordHash;

        await seeder.EjecutarAsync(new HostEnvironmentFake("Development"));

        Assert.Single(store.Empresas);
        Assert.Single(store.Usuarios);
        Assert.Single(store.Relaciones);
        Assert.Single(store.Sedes);
        Assert.Single(store.PuntosVenta);
        Assert.Single(store.Categorias);
        Assert.Single(store.Marcas);
        Assert.Equal(5, store.UnidadesMedida.Count);
        Assert.Single(store.Productos);
        Assert.Single(store.Stocks);
        Assert.Equal(3, store.SeriesComprobante.Count);
        var credencial = Assert.Single(store.Credenciales);
        Assert.Equal(hashOriginal, credencial.PasswordHash);
        Assert.Equal(2, store.SaveChangesCount);
    }

    [Fact]
    public void Appsettings_development_mantiene_enabled_false_y_no_contiene_password_demo()
    {
        var root = EncontrarRaizRepo();
        var appsettings = File.ReadAllText(Path.Combine(
            root,
            "src",
            "CapitalPos.Api",
            "appsettings.Development.json"));

        Assert.Contains("\"DemoSeed\"", appsettings);
        Assert.Contains("\"Enabled\": false", appsettings);
        Assert.DoesNotContain("AdminPassword", appsettings, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PasswordDemo, appsettings, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentacion_de_datos_demo_indica_user_secrets_y_no_incluye_password_real()
    {
        var root = EncontrarRaizRepo();
        var documentacion = File.ReadAllText(Path.Combine(root, "Docs", "DatosDemo.md"));

        Assert.Contains("DemoSeed:Enabled", documentacion);
        Assert.Contains("DemoSeed:AdminPassword", documentacion);
        Assert.Contains("dotnet user-secrets set", documentacion);
        Assert.Contains("admin@capitalpos.test", documentacion);
        Assert.Contains("20600000001", documentacion);
        Assert.Contains("Nunca debe habilitarse en producción", documentacion);
        Assert.DoesNotContain(PasswordDemo, documentacion, StringComparison.Ordinal);
        Assert.Contains("<password-demo-local>", documentacion);
    }

    private static DemoDataSeeder CrearSeeder(
        DemoSeedStoreFake store,
        bool enabled,
        string adminPassword,
        DemoSeedLoggerFake? logger = null)
    {
        return new DemoDataSeeder(
            store,
            new PasswordHasherFake(),
            Options.Create(new DemoSeedOptions
            {
                Enabled = enabled,
                AdminPassword = adminPassword
            }),
            logger ?? new DemoSeedLoggerFake());
    }

    private sealed class DemoSeedStoreFake : IDemoSeedStore
    {
        public List<Empresa> Empresas { get; } = [];

        public List<Usuario> Usuarios { get; } = [];

        public List<UsuarioEmpresa> Relaciones { get; } = [];

        public List<UsuarioCredencial> Credenciales { get; } = [];

        public List<Sede> Sedes { get; } = [];

        public List<PuntoVenta> PuntosVenta { get; } = [];

        public List<Categoria> Categorias { get; } = [];

        public List<Marca> Marcas { get; } = [];

        public List<UnidadMedida> UnidadesMedida { get; } = [];

        public List<Producto> Productos { get; } = [];

        public List<StockProducto> Stocks { get; } = [];

        public List<SerieComprobante> SeriesComprobante { get; } = [];

        public int SaveChangesCount { get; private set; }

        public Task<Empresa?> ObtenerEmpresaPorRucAsync(string ruc, CancellationToken cancellationToken)
        {
            return Task.FromResult(Empresas.SingleOrDefault(empresa => empresa.Ruc == ruc));
        }

        public Task<Usuario?> ObtenerUsuarioPorCorreoAsync(string correo, CancellationToken cancellationToken)
        {
            return Task.FromResult(Usuarios.SingleOrDefault(usuario => usuario.Correo == correo));
        }

        public Task<UsuarioEmpresa?> ObtenerUsuarioEmpresaAsync(
            Guid usuarioId,
            Guid empresaId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Relaciones.SingleOrDefault(relacion =>
                relacion.UsuarioId == usuarioId &&
                relacion.EmpresaId == empresaId));
        }

        public Task<UsuarioCredencial?> ObtenerCredencialAsync(Guid usuarioId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Credenciales.SingleOrDefault(credencial => credencial.UsuarioId == usuarioId));
        }

        public Task<Sede?> ObtenerSedeAsync(
            Guid empresaId,
            Guid sedeId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Sedes.SingleOrDefault(sede =>
                sede.EmpresaId == empresaId &&
                sede.Id == sedeId));
        }

        public Task<PuntoVenta?> ObtenerPuntoVentaAsync(
            Guid empresaId,
            Guid puntoVentaId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PuntosVenta.SingleOrDefault(puntoVenta =>
                puntoVenta.EmpresaId == empresaId &&
                puntoVenta.Id == puntoVentaId));
        }

        public Task<Categoria?> ObtenerCategoriaAsync(
            Guid empresaId,
            string nombre,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Categorias.SingleOrDefault(categoria =>
                categoria.EmpresaId == empresaId &&
                categoria.Nombre == nombre.Trim()));
        }

        public Task<Marca?> ObtenerMarcaAsync(
            Guid empresaId,
            string nombre,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Marcas.SingleOrDefault(marca =>
                marca.EmpresaId == empresaId &&
                marca.Nombre == nombre.Trim()));
        }

        public Task<UnidadMedida?> ObtenerUnidadMedidaAsync(
            string codigo,
            CancellationToken cancellationToken)
        {
            var codigoNormalizado = codigo.Trim().ToUpperInvariant();

            return Task.FromResult(UnidadesMedida.SingleOrDefault(unidadMedida =>
                unidadMedida.Codigo == codigoNormalizado));
        }

        public Task<Producto?> ObtenerProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Productos.SingleOrDefault(producto =>
                producto.EmpresaId == empresaId &&
                producto.Id == productoId));
        }

        public Task<StockProducto?> ObtenerStockProductoAsync(
            Guid empresaId,
            Guid sedeId,
            Guid productoId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Stocks.SingleOrDefault(stock =>
                stock.EmpresaId == empresaId &&
                stock.SedeId == sedeId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId is null));
        }

        public Task<SerieComprobante?> ObtenerSerieComprobanteAsync(
            Guid empresaId,
            Guid sedeId,
            string tipoComprobante,
            string serie,
            CancellationToken cancellationToken)
        {
            var tipoNormalizado = tipoComprobante.Trim().ToUpperInvariant();
            var serieNormalizada = serie.Trim().ToUpperInvariant();

            return Task.FromResult(SeriesComprobante.SingleOrDefault(serieComprobante =>
                serieComprobante.EmpresaId == empresaId &&
                serieComprobante.SedeId == sedeId &&
                serieComprobante.TipoComprobante == tipoNormalizado &&
                serieComprobante.Serie == serieNormalizada));
        }

        public Task AgregarEmpresaAsync(Empresa empresa, CancellationToken cancellationToken)
        {
            Empresas.Add(empresa);
            return Task.CompletedTask;
        }

        public Task AgregarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            Usuarios.Add(usuario);
            return Task.CompletedTask;
        }

        public Task AgregarUsuarioEmpresaAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken)
        {
            Relaciones.Add(usuarioEmpresa);
            return Task.CompletedTask;
        }

        public Task AgregarCredencialAsync(UsuarioCredencial credencial, CancellationToken cancellationToken)
        {
            Credenciales.Add(credencial);
            return Task.CompletedTask;
        }

        public Task AgregarSedeAsync(Sede sede, CancellationToken cancellationToken)
        {
            Sedes.Add(sede);
            return Task.CompletedTask;
        }

        public Task AgregarPuntoVentaAsync(PuntoVenta puntoVenta, CancellationToken cancellationToken)
        {
            PuntosVenta.Add(puntoVenta);
            return Task.CompletedTask;
        }

        public Task AgregarCategoriaAsync(Categoria categoria, CancellationToken cancellationToken)
        {
            Categorias.Add(categoria);
            return Task.CompletedTask;
        }

        public Task AgregarMarcaAsync(Marca marca, CancellationToken cancellationToken)
        {
            Marcas.Add(marca);
            return Task.CompletedTask;
        }

        public Task AgregarUnidadMedidaAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken)
        {
            UnidadesMedida.Add(unidadMedida);
            return Task.CompletedTask;
        }

        public Task AgregarProductoAsync(Producto producto, CancellationToken cancellationToken)
        {
            Productos.Add(producto);
            return Task.CompletedTask;
        }

        public Task AgregarStockProductoAsync(StockProducto stock, CancellationToken cancellationToken)
        {
            Stocks.Add(stock);
            return Task.CompletedTask;
        }

        public Task AgregarSerieComprobanteAsync(SerieComprobante serie, CancellationToken cancellationToken)
        {
            SeriesComprobante.Add(serie);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class PasswordHasherFake : IPasswordHasher
    {
        public string GenerarHash(UsuarioCredencial credencial, string password)
        {
            return $"hash-seguro:{credencial.UsuarioId:N}:{password.Length}";
        }

        public PasswordVerificationResult Verificar(UsuarioCredencial credencial, string password)
        {
            return new PasswordVerificationResult(EsValida: true, RequiereRehash: false);
        }
    }

    private sealed class DemoSeedLoggerFake : ILogger<DemoDataSeeder>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class HostEnvironmentFake : IHostEnvironment
    {
        public HostEnvironmentFake(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "CapitalPos.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private static string EncontrarRaizRepo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CapitalPos.Api.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo encontrar la raiz del repositorio.");
    }
}
