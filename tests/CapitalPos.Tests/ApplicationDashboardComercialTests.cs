using CapitalPos.Application.Dashboard;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationDashboardComercialTests
{
    private static readonly Guid SedeIdPrueba = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PuntoVentaIdPrueba = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Dashboard_comercial_resume_ventas_registradas_de_hoy_lima()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        repos.Productos.Productos.Add(new Producto(productoId, empresaId, "Polo", 50m, codigoSku: "POLO"));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 2m, 100m)]));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.PROVINCIA,
            new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 3m, 90m)]));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 14, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 9m, 900m)],
            EstadoVenta.Anulada));
        repos.Ventas.Ventas.Add(CrearVenta(
            otraEmpresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 99m, 990m)]));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 7, 18, 0, 1, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 1m, 10m)]));
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal(new DateOnly(2026, 7, 17), dashboard.Fecha);
        Assert.Equal(190m, dashboard.Resumen.ImporteTotalVendido);
        Assert.Equal(2, dashboard.Resumen.CantidadOperaciones);
        Assert.Equal(5m, dashboard.Resumen.UnidadesVendidas);
        Assert.NotNull(dashboard.Resumen.CanalLider);
        Assert.Equal("TIENDA", dashboard.Resumen.CanalLider.CanalVenta);
        Assert.Equal(100m, dashboard.Resumen.CanalLider.ImporteVendido);
    }

    [Fact]
    public async Task Dashboard_comercial_devuelve_canal_lider_null_y_top_vacio_sin_ventas()
    {
        var empresaId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal(0m, dashboard.Resumen.ImporteTotalVendido);
        Assert.Equal(0, dashboard.Resumen.CantidadOperaciones);
        Assert.Equal(0m, dashboard.Resumen.UnidadesVendidas);
        Assert.Null(dashboard.Resumen.CanalLider);
        Assert.Empty(dashboard.TopProductos);
    }

    [Fact]
    public async Task Dashboard_comercial_agrupa_top_productos_y_variantes()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        repos.Productos.Productos.Add(new Producto(productoId, empresaId, "Polo", 50m, codigoSku: "POLO"));
        repos.Variantes.Variantes.Add(new ProductoVariante(
            varianteId,
            empresaId,
            productoId,
            talla: "M",
            color: "Negro",
            codigoSku: "POLO-M-NEGRO",
            codigoBarras: "7750000000010"));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [
                (productoId, null, 2m, 100m),
                (productoId, varianteId, 5m, 250m)
            ]));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 4m, 120m)]));
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal(2, dashboard.TopProductos.Count);
        var producto = dashboard.TopProductos.First();
        Assert.Equal(productoId, producto.ProductoId);
        Assert.Null(producto.ProductoVarianteId);
        Assert.Equal(6m, producto.Unidades);
        Assert.Equal(220m, producto.ImporteVendido);
        var variante = dashboard.TopProductos.Last();
        Assert.Equal(varianteId, variante.ProductoVarianteId);
        Assert.Equal("M", variante.Talla);
        Assert.Equal("Negro", variante.Color);
        Assert.Equal("POLO-M-NEGRO", variante.CodigoSku);
        Assert.Equal("7750000000010", variante.CodigoBarras);
    }

    [Fact]
    public async Task Dashboard_comercial_ordena_limita_y_calcula_stock_bajo_desde_stock_producto()
    {
        var empresaId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        for (var index = 0; index < 7; index++)
        {
            var productoId = Guid.NewGuid();
            repos.Productos.Productos.Add(new Producto(productoId, empresaId, $"Producto {index}", 10m));
            repos.Stocks.Stocks.Add(new StockProducto(
            Guid.NewGuid(),
            empresaId,
            SedeIdPrueba,
                productoId,
                null,
                index,
                index == 5 ? 0m : 0m));
        }
        var productoSinStockBajoId = Guid.NewGuid();
        repos.Productos.Productos.Add(new Producto(productoSinStockBajoId, empresaId, "Producto 99", 10m));
        repos.Stocks.Stocks.Add(new StockProducto(
            Guid.NewGuid(),
            empresaId,
            SedeIdPrueba,
            productoSinStockBajoId,
            null,
            6m));
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal(5, dashboard.StockBajo.Count);
        Assert.Equal([0m, 1m, 2m, 3m, 4m], dashboard.StockBajo.Select(item => item.StockLibre).ToArray());
        Assert.DoesNotContain(dashboard.StockBajo, item => item.StockLibre > DashboardComercialUseCase.UmbralStockBajo);
    }

    [Fact]
    public async Task Dashboard_comercial_calcula_dia_actual_en_lima_y_no_en_utc()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        repos.Productos.Productos.Add(new Producto(productoId, empresaId, "Polo", 50m));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 23, 30, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 2m, 100m)]));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 18, 0, 30, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 3m, 150m)]));
        var useCase = CrearUseCase(
            repos,
            empresaId,
            new DateTimeOffset(2026, 7, 18, 0, 15, 0, TimeSpan.FromHours(-5)));

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal(new DateOnly(2026, 7, 18), dashboard.Fecha);
        Assert.Equal(1, dashboard.Resumen.CantidadOperaciones);
        Assert.Equal(3m, dashboard.Resumen.UnidadesVendidas);
        Assert.Equal(150m, dashboard.Resumen.ImporteTotalVendido);
    }

    [Fact]
    public async Task Dashboard_comercial_envia_limites_del_dia_lima_convertidos_a_utc()
    {
        var empresaId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        var ahoraLima = new DateTimeOffset(2026, 7, 17, 15, 42, 10, TimeSpan.FromHours(-5));
        var useCase = CrearUseCase(repos, empresaId, ahoraLima);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal(TimeSpan.FromHours(-5), ahoraLima.Offset);
        Assert.Equal(new DateOnly(2026, 7, 17), dashboard.Fecha);
        Assert.Equal(new DateTimeOffset(2026, 7, 17, 5, 0, 0, TimeSpan.Zero), repos.Ventas.UltimoDesde);
        Assert.Equal(new DateTimeOffset(2026, 7, 18, 5, 0, 0, TimeSpan.Zero), repos.Ventas.UltimoHastaExclusivo);
        Assert.Equal(TimeSpan.Zero, repos.Ventas.UltimoDesde?.Offset);
        Assert.Equal(TimeSpan.Zero, repos.Ventas.UltimoHastaExclusivo?.Offset);
        Assert.Equal(TimeSpan.FromHours(24), repos.Ventas.UltimoHastaExclusivo - repos.Ventas.UltimoDesde);
    }

    [Fact]
    public async Task Dashboard_comercial_desempata_canal_lider_por_canal_ascendente()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        repos.Productos.Productos.Add(new Producto(productoId, empresaId, "Polo", 50m));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 1m, 100m)]));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.MARKETING,
            new DateTimeOffset(2026, 7, 17, 11, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 1m, 100m)]));
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal("MARKETING", dashboard.Resumen.CanalLider?.CanalVenta);
    }

    [Fact]
    public async Task Dashboard_comercial_desempata_top_productos_y_limita_a_cinco()
    {
        var empresaId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        var productos = new[]
        {
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000003"), Nombre: "Beta"),
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000002"), Nombre: "Alpha"),
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000001"), Nombre: "Alpha"),
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000004"), Nombre: "Gamma"),
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000005"), Nombre: "Omega"),
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000006"), Nombre: "Zeta")
        };
        foreach (var producto in productos)
        {
            repos.Productos.Productos.Add(new Producto(producto.Id, empresaId, producto.Nombre, 10m));
            repos.Ventas.Ventas.Add(CrearVenta(
                empresaId,
                CanalVenta.TIENDA,
                new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
                [(producto.Id, null, 2m, 20m)]));
        }
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal(5, dashboard.TopProductos.Count);
        Assert.Equal(
            [
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Guid.Parse("00000000-0000-0000-0000-000000000004"),
                Guid.Parse("00000000-0000-0000-0000-000000000005")
            ],
            dashboard.TopProductos.Select(item => item.ProductoId).ToArray());
    }

    [Fact]
    public async Task Dashboard_comercial_desempata_top_productos_por_variante_null_primero()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var varianteId = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var repos = CrearRepositorios(empresaId);
        repos.Productos.Productos.Add(new Producto(productoId, empresaId, "Alpha", 10m));
        repos.Variantes.Variantes.Add(new ProductoVariante(
            varianteId,
            empresaId,
            productoId,
            talla: "M"));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 2m, 20m)]));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 11, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, varianteId, 2m, 20m)]));
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Null(dashboard.TopProductos.First().ProductoVarianteId);
        Assert.Equal(varianteId, dashboard.TopProductos.Last().ProductoVarianteId);
    }

    [Fact]
    public async Task Dashboard_comercial_excluye_stock_negativo_y_de_otra_empresa()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var productoOtraEmpresaId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        repos.Productos.Productos.Add(new Producto(productoId, empresaId, "Polo", 10m));
        repos.Productos.Productos.Add(new Producto(productoOtraEmpresaId, otraEmpresaId, "Ajeno", 10m));
        repos.Stocks.Stocks.Add(CrearStockForzado(
            empresaId,
            productoId,
            null,
            1m,
            3m));
        repos.Stocks.Stocks.Add(new StockProducto(
            Guid.NewGuid(),
            otraEmpresaId,
            SedeIdPrueba,
            productoOtraEmpresaId,
            null,
            1m));
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Empty(dashboard.StockBajo);
    }

    [Fact]
    public async Task Dashboard_comercial_responde_con_datos_opcionales_nulos()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repos = CrearRepositorios(empresaId);
        repos.Productos.Productos.Add(new Producto(productoId, empresaId, "Polo", 10m));
        repos.Ventas.Ventas.Add(CrearVenta(
            empresaId,
            CanalVenta.TIENDA,
            new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(-5)),
            [(productoId, null, 1m, 10m)]));
        repos.Stocks.Stocks.Add(new StockProducto(productoId, empresaId, SedeIdPrueba, productoId, null, 2m));
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        var top = Assert.Single(dashboard.TopProductos);
        Assert.Null(top.ProductoVarianteId);
        Assert.Null(top.Talla);
        Assert.Null(top.Color);
        Assert.Null(top.CodigoSku);
        Assert.Null(top.CodigoBarras);
        var stock = Assert.Single(dashboard.StockBajo);
        Assert.Null(stock.ProductoVarianteId);
        Assert.Null(stock.Talla);
        Assert.Null(stock.Color);
        Assert.Null(stock.CodigoSku);
        Assert.Null(stock.CodigoBarras);
    }

    [Fact]
    public async Task Dashboard_comercial_desempata_stock_bajo_por_producto_y_variante()
    {
        var empresaId = Guid.NewGuid();
        var productoAId = Guid.Parse("00000000-0000-0000-0000-000000000021");
        var productoBId = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var varianteId = Guid.Parse("00000000-0000-0000-0000-000000000022");
        var repos = CrearRepositorios(empresaId);
        repos.Productos.Productos.Add(new Producto(productoAId, empresaId, "Igual", 10m));
        repos.Productos.Productos.Add(new Producto(productoBId, empresaId, "Igual", 10m));
        repos.Variantes.Variantes.Add(new ProductoVariante(varianteId, empresaId, productoAId, talla: "M"));
        repos.Stocks.Stocks.Add(new StockProducto(
            Guid.NewGuid(),
            empresaId,
            SedeIdPrueba, productoAId, varianteId, 2m));
        repos.Stocks.Stocks.Add(new StockProducto(
            Guid.NewGuid(),
            empresaId,
            SedeIdPrueba, productoBId, null, 2m));
        repos.Stocks.Stocks.Add(new StockProducto(
            Guid.NewGuid(),
            empresaId,
            SedeIdPrueba, productoAId, null, 2m));
        var useCase = CrearUseCase(repos, empresaId);

        var dashboard = await useCase.EjecutarAsync();

        Assert.Equal(
            [
                (productoBId, (Guid?)null),
                (productoAId, (Guid?)null),
                (productoAId, (Guid?)varianteId)
            ],
            dashboard.StockBajo.Select(item => (item.ProductoId, item.ProductoVarianteId)).ToArray());
    }

    private static DashboardComercialUseCase CrearUseCase(Repositorios repos, Guid empresaId)
    {
        return CrearUseCase(
            repos,
            empresaId,
            new DateTimeOffset(2026, 7, 17, 15, 42, 10, TimeSpan.FromHours(-5)));
    }

    private static DashboardComercialUseCase CrearUseCase(
        Repositorios repos,
        Guid empresaId,
        DateTimeOffset ahoraLima)
    {
        return new DashboardComercialUseCase(
            repos.Ventas,
            repos.Productos,
            repos.Variantes,
            repos.Stocks,
            new EmpresaActivaContextFake(empresaId),
            new ClockFake(ahoraLima));
    }

    private static Repositorios CrearRepositorios(Guid empresaId)
    {
        return new Repositorios(
            new VentaRepositoryFake(),
            new ProductoRepositoryFake(),
            new ProductoVarianteRepositoryFake(),
            new StockProductoRepositoryFake());
    }

    private static Venta CrearVenta(
        Guid empresaId,
        CanalVenta canalVenta,
        DateTimeOffset fecha,
        IReadOnlyCollection<(Guid ProductoId, Guid? ProductoVarianteId, decimal Cantidad, decimal Total)> detalles,
        EstadoVenta estado = EstadoVenta.Registrada)
    {
        var ventaId = Guid.NewGuid();
        var ventaDetalles = detalles
            .Select(detalle => new VentaDetalle(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                detalle.ProductoId,
                detalle.Cantidad,
                detalle.Total / detalle.Cantidad,
                0m,
                detalle.Total,
                detalle.ProductoVarianteId))
            .ToArray();
        var total = ventaDetalles.Sum(detalle => detalle.Total);

        return new Venta(
            ventaId,
            empresaId,
            fecha,
            total,
            0m,
            total,
            ventaDetalles,
            SedeIdPrueba,
            PuntoVentaIdPrueba,
            canalVenta: canalVenta,
            estado: estado);
    }

    private sealed record Repositorios(
        VentaRepositoryFake Ventas,
        ProductoRepositoryFake Productos,
        ProductoVarianteRepositoryFake Variantes,
        StockProductoRepositoryFake Stocks);

    private static StockProducto CrearStockForzado(
        Guid empresaId,
        Guid productoId,
        Guid? productoVarianteId,
        decimal cantidadDisponible,
        decimal cantidadReservada)
    {
        var stock = new StockProducto(
            Guid.NewGuid(),
            empresaId,
            SedeIdPrueba,
            productoId,
            productoVarianteId,
            Math.Max(cantidadDisponible, cantidadReservada),
            cantidadReservada);
        typeof(StockProducto)
            .GetProperty(nameof(StockProducto.CantidadDisponible))!
            .SetValue(stock, cantidadDisponible);

        return stock;
    }

    private sealed class ClockFake : IDashboardComercialClock
    {
        private readonly DateTimeOffset _ahoraLima;

        public ClockFake(DateTimeOffset ahoraLima)
        {
            _ahoraLima = ahoraLima;
        }

        public DateTimeOffset AhoraLima() => _ahoraLima;
    }

    private sealed class EmpresaActivaContextFake : IEmpresaActivaContext
    {
        public EmpresaActivaContextFake(Guid empresaId)
        {
            UsuarioId = Guid.NewGuid();
            EmpresaId = empresaId;
            Rol = RolEmpresa.Administrador;
            TieneEmpresaActiva = true;
        }

        public bool TieneEmpresaActiva { get; }

        public Guid UsuarioId { get; }

        public Guid EmpresaId { get; }

        public RolEmpresa Rol { get; }
    }

    private sealed class VentaRepositoryFake : IVentaRepository
    {
        public List<Venta> Ventas { get; } = new();

        public DateTimeOffset? UltimoDesde { get; private set; }

        public DateTimeOffset? UltimoHastaExclusivo { get; private set; }

        public Task AgregarAsync(Venta venta, CancellationToken cancellationToken = default)
        {
            Ventas.Add(venta);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Venta>>(
                Ventas.Where(venta => venta.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<Venta>> ListarRegistradasPorEmpresaYFechaAsync(
            Guid empresaId,
            DateTimeOffset desde,
            DateTimeOffset hastaExclusivo,
            CancellationToken cancellationToken = default)
        {
            UltimoDesde = desde;
            UltimoHastaExclusivo = hastaExclusivo;

            return Task.FromResult<IReadOnlyCollection<Venta>>(
                Ventas.Where(venta =>
                    venta.EmpresaId == empresaId &&
                    venta.Estado == EstadoVenta.Registrada &&
                    venta.Fecha >= desde &&
                    venta.Fecha < hastaExclusivo).ToArray());
        }

        public Task<Venta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Ventas.FirstOrDefault(venta =>
                venta.EmpresaId == empresaId &&
                venta.Id == id));
        }
    }

    private sealed class ProductoRepositoryFake : IProductoRepository
    {
        public List<Producto> Productos { get; } = new();

        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            Productos.Add(producto);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Producto>>(
                Productos.Where(producto => producto.EmpresaId == empresaId).ToArray());
        }

        public Task<Producto?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Productos.FirstOrDefault(producto =>
                producto.EmpresaId == empresaId &&
                producto.Id == id));
        }

        public Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ProductoVarianteRepositoryFake : IProductoVarianteRepository
    {
        public List<ProductoVariante> Variantes { get; } = new();

        public Task AgregarAsync(ProductoVariante variante, CancellationToken cancellationToken = default)
        {
            Variantes.Add(variante);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>(
                Variantes.Where(variante =>
                    variante.EmpresaId == empresaId &&
                    variante.ProductoId == productoId).ToArray());
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProductoVariante>>(
                Variantes.Where(variante => variante.EmpresaId == empresaId).ToArray());
        }

        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.FirstOrDefault(variante =>
                variante.EmpresaId == empresaId &&
                variante.Id == id));
        }

        public Task ActualizarAsync(ProductoVariante variante, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExisteSkuAsync(
            Guid empresaId,
            string codigoSku,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.Any(variante =>
                variante.EmpresaId == empresaId &&
                variante.CodigoSku == codigoSku.Trim()));
        }

        public Task<bool> ExisteCodigoBarrasAsync(
            Guid empresaId,
            string codigoBarras,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Variantes.Any(variante =>
                variante.EmpresaId == empresaId &&
                variante.CodigoBarras == codigoBarras.Trim()));
        }
    }

    private sealed class StockProductoRepositoryFake : IStockProductoRepository
    {
        public List<StockProducto> Stocks { get; } = new();

        public Task<StockProducto?> ObtenerPorProductoAsync(
            Guid empresaId,
            Guid sedeId,
            Guid productoId,
            Guid? productoVarianteId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stocks.FirstOrDefault(stock =>
                stock.EmpresaId == empresaId &&
                stock.SedeId == sedeId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId == productoVarianteId));
        }

        public Task<IReadOnlyCollection<StockProducto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<StockProducto>>(
                Stocks.Where(stock => stock.EmpresaId == empresaId).ToArray());
        }

        public Task<IReadOnlyCollection<StockProducto>> ListarPorSedeAsync(
            Guid empresaId,
            Guid sedeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<StockProducto>>(
                Stocks.Where(stock => stock.EmpresaId == empresaId && stock.SedeId == sedeId).ToArray());
        }

        public Task GuardarAsync(StockProducto stock, CancellationToken cancellationToken = default)
        {
            Stocks.Add(stock);

            return Task.CompletedTask;
        }
    }
}
