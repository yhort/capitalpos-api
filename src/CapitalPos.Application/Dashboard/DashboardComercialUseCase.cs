using CapitalPos.Application.Inventario;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Application.Dashboard;

public sealed class DashboardComercialUseCase
{
    private const int LimiteTopProductos = 5;
    private const int LimiteStockBajo = 5;

    // Deuda tecnica: mover este umbral a configuracion por empresa, producto o variante.
    public const decimal UmbralStockBajo = 5m;

    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IDashboardComercialClock _clock;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IStockProductoRepository _stockRepository;
    private readonly IVentaRepository _ventaRepository;

    public DashboardComercialUseCase(
        IVentaRepository ventaRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IStockProductoRepository stockRepository,
        IEmpresaActivaContext empresaActiva,
        IDashboardComercialClock clock)
    {
        _ventaRepository = ventaRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _stockRepository = stockRepository;
        _empresaActiva = empresaActiva;
        _clock = clock;
    }

    public async Task<DashboardComercialResponse> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        var ultimaActualizacion = _clock.AhoraLima();
        var fecha = DateOnly.FromDateTime(ultimaActualizacion.Date);
        var inicioLima = new DateTimeOffset(fecha.ToDateTime(TimeOnly.MinValue), ultimaActualizacion.Offset);
        var desde = inicioLima.ToUniversalTime();
        var hastaExclusivo = inicioLima.AddDays(1).ToUniversalTime();
        var empresaId = _empresaActiva.EmpresaId;

        var ventas = await _ventaRepository.ListarRegistradasPorEmpresaYFechaAsync(
            empresaId,
            desde,
            hastaExclusivo,
            cancellationToken);
        var productos = (await _productoRepository.ListarPorEmpresaAsync(empresaId, cancellationToken))
            .ToDictionary(producto => producto.Id);
        var variantes = (await _productoVarianteRepository.ListarPorEmpresaAsync(empresaId, cancellationToken))
            .ToDictionary(variante => variante.Id);

        var resumen = CrearResumen(ventas);
        var topProductos = CrearTopProductos(ventas, productos, variantes);
        var stockBajo = await CrearStockBajoAsync(empresaId, productos, variantes, cancellationToken);

        return new DashboardComercialResponse(
            fecha,
            ultimaActualizacion,
            resumen,
            topProductos,
            stockBajo);
    }

    private static DashboardComercialResumen CrearResumen(IReadOnlyCollection<Venta> ventas)
    {
        var importeTotal = ventas.Sum(venta => venta.Total);
        var unidades = ventas.Sum(venta => venta.Detalles.Sum(detalle => detalle.Cantidad));
        var canalLider = ventas
            .GroupBy(venta => venta.CanalVenta)
            .Select(grupo => new DashboardComercialCanalLider(
                grupo.Key.ToString(),
                grupo.Sum(venta => venta.Total)))
            .OrderByDescending(canal => canal.ImporteVendido)
            .ThenBy(canal => canal.CanalVenta)
            .FirstOrDefault();

        return new DashboardComercialResumen(
            importeTotal,
            ventas.Count,
            unidades,
            canalLider);
    }

    private static IReadOnlyCollection<DashboardComercialTopProducto> CrearTopProductos(
        IReadOnlyCollection<Venta> ventas,
        IReadOnlyDictionary<Guid, Producto> productos,
        IReadOnlyDictionary<Guid, ProductoVariante> variantes)
    {
        return ventas
            .SelectMany(venta => venta.Detalles)
            .GroupBy(detalle => new
            {
                detalle.ProductoId,
                detalle.ProductoVarianteId
            })
            .Select(grupo => CrearTopProducto(grupo, productos, variantes))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.Unidades)
            .ThenByDescending(item => item.ImporteVendido)
            .ThenBy(item => item.Producto, StringComparer.Ordinal)
            .ThenBy(item => item.ProductoId)
            .ThenBy(item => item.ProductoVarianteId ?? Guid.Empty)
            .Take(LimiteTopProductos)
            .ToArray();
    }

    private static DashboardComercialTopProducto? CrearTopProducto<TKey>(
        IGrouping<TKey, VentaDetalle> grupo,
        IReadOnlyDictionary<Guid, Producto> productos,
        IReadOnlyDictionary<Guid, ProductoVariante> variantes)
    {
        var primerDetalle = grupo.First();
        if (!productos.TryGetValue(primerDetalle.ProductoId, out var producto))
        {
            return null;
        }

        ProductoVariante? variante = null;
        if (primerDetalle.ProductoVarianteId is Guid varianteId)
        {
            variantes.TryGetValue(varianteId, out variante);
        }

        return new DashboardComercialTopProducto(
            primerDetalle.ProductoId,
            primerDetalle.ProductoVarianteId,
            producto.Nombre,
            ValorOpcional(variante?.Talla),
            ValorOpcional(variante?.Color),
            ValorOpcional(variante?.CodigoSku ?? producto.CodigoSku),
            ValorOpcional(variante?.CodigoBarras ?? producto.CodigoBarras),
            grupo.Sum(detalle => detalle.Cantidad),
            grupo.Sum(detalle => detalle.Total));
    }

    private async Task<IReadOnlyCollection<DashboardComercialStockBajo>> CrearStockBajoAsync(
        Guid empresaId,
        IReadOnlyDictionary<Guid, Producto> productos,
        IReadOnlyDictionary<Guid, ProductoVariante> variantes,
        CancellationToken cancellationToken)
    {
        var stocks = await _stockRepository.ListarPorEmpresaAsync(empresaId, cancellationToken);

        return stocks
            .Where(stock => stock.CantidadLibre >= 0 && stock.CantidadLibre <= UmbralStockBajo)
            .Select(stock => CrearStockBajo(stock, productos, variantes))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => item.StockLibre)
            .ThenBy(item => item.Producto, StringComparer.Ordinal)
            .ThenBy(item => item.ProductoId)
            .ThenBy(item => item.ProductoVarianteId ?? Guid.Empty)
            .Take(LimiteStockBajo)
            .ToArray();
    }

    private static DashboardComercialStockBajo? CrearStockBajo(
        StockProducto stock,
        IReadOnlyDictionary<Guid, Producto> productos,
        IReadOnlyDictionary<Guid, ProductoVariante> variantes)
    {
        if (!productos.TryGetValue(stock.ProductoId, out var producto))
        {
            return null;
        }

        ProductoVariante? variante = null;
        if (stock.ProductoVarianteId is Guid varianteId)
        {
            variantes.TryGetValue(varianteId, out variante);
        }

        return new DashboardComercialStockBajo(
            stock.ProductoId,
            stock.ProductoVarianteId,
            producto.Nombre,
            ValorOpcional(variante?.Talla),
            ValorOpcional(variante?.Color),
            ValorOpcional(variante?.CodigoSku ?? producto.CodigoSku),
            ValorOpcional(variante?.CodigoBarras ?? producto.CodigoBarras),
            stock.CantidadDisponible,
            stock.CantidadReservada,
            stock.CantidadLibre);
    }

    private static string? ValorOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar el dashboard comercial.");
        }
    }
}

public interface IDashboardComercialClock
{
    DateTimeOffset AhoraLima();
}

public sealed class DashboardComercialClock : IDashboardComercialClock
{
    private static readonly TimeZoneInfo LimaTimeZone = ObtenerLimaTimeZone();

    public DateTimeOffset AhoraLima()
    {
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, LimaTimeZone);
    }

    private static TimeZoneInfo ObtenerLimaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
    }
}

public sealed record DashboardComercialResponse(
    DateOnly Fecha,
    DateTimeOffset UltimaActualizacion,
    DashboardComercialResumen Resumen,
    IReadOnlyCollection<DashboardComercialTopProducto> TopProductos,
    IReadOnlyCollection<DashboardComercialStockBajo> StockBajo);

public sealed record DashboardComercialResumen(
    decimal ImporteTotalVendido,
    int CantidadOperaciones,
    decimal UnidadesVendidas,
    DashboardComercialCanalLider? CanalLider);

public sealed record DashboardComercialCanalLider(
    string CanalVenta,
    decimal ImporteVendido);

public sealed record DashboardComercialTopProducto(
    Guid ProductoId,
    Guid? ProductoVarianteId,
    string Producto,
    string? Talla,
    string? Color,
    string? CodigoSku,
    string? CodigoBarras,
    decimal Unidades,
    decimal ImporteVendido);

public sealed record DashboardComercialStockBajo(
    Guid ProductoId,
    Guid? ProductoVarianteId,
    string Producto,
    string? Talla,
    string? Color,
    string? CodigoSku,
    string? CodigoBarras,
    decimal CantidadDisponible,
    decimal CantidadReservada,
    decimal StockLibre);
