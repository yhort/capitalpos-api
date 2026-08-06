using CapitalPos.Application.Caja;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Application.Pedidos;

public sealed record ConvertirPedidoDigitalAVentaRequest(
    Guid? PuntoVentaId = null,
    IReadOnlyCollection<CrearVentaPagoRequest>? Pagos = null,
    string? Observacion = null);

public sealed record ConvertirPedidoDigitalAVentaResult(PedidoDigital Pedido, Venta Venta);

public sealed class ConvertirPedidoDigitalAVentaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IMovimientoInventarioRepository? _movimientos;
    private readonly IPedidoDigitalRepository _pedidoRepository;
    private readonly IPuntoVentaRepository _puntoVentaRepository;
    private readonly ISesionCajaRepository _sesionCajaRepository;
    private readonly IStockProductoRepository _stockRepository;
    private readonly IVentaRepository _ventaRepository;

    public ConvertirPedidoDigitalAVentaUseCase(
        IPedidoDigitalRepository pedidoRepository,
        IVentaRepository ventaRepository,
        IStockProductoRepository stockRepository,
        ISesionCajaRepository sesionCajaRepository,
        IPuntoVentaRepository puntoVentaRepository,
        IEmpresaActivaContext empresaActiva,
        IMovimientoInventarioRepository? movimientos = null)
    {
        _pedidoRepository = pedidoRepository;
        _ventaRepository = ventaRepository;
        _stockRepository = stockRepository;
        _sesionCajaRepository = sesionCajaRepository;
        _puntoVentaRepository = puntoVentaRepository;
        _empresaActiva = empresaActiva;
        _movimientos = movimientos;
    }

    public async Task<ConvertirPedidoDigitalAVentaResult?> EjecutarAsync(
        Guid pedidoId,
        ConvertirPedidoDigitalAVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (pedidoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del pedido digital es obligatorio.", nameof(pedidoId));
        }

        ValidarEmpresaActiva();
        var empresaId = _empresaActiva.EmpresaId;
        var pedido = await _pedidoRepository.ObtenerPorEmpresaAsync(empresaId, pedidoId, cancellationToken);
        if (pedido is null)
        {
            return null;
        }

        if (pedido.Estado == EstadoPedidoDigital.Cancelado)
        {
            throw new InvalidOperationException("No se puede convertir un pedido digital cancelado.");
        }

        if (pedido.Estado == EstadoPedidoDigital.Entregado)
        {
            throw new InvalidOperationException("El pedido digital ya fue convertido o entregado.");
        }

        var puntoVenta = await ObtenerPuntoVentaAsync(
            empresaId,
            pedido,
            request.PuntoVentaId,
            cancellationToken);
        await ValidarSesionCajaAbiertaAsync(empresaId, puntoVenta.Id, cancellationToken);

        var ventaId = Guid.NewGuid();
        var detalles = pedido.Detalles
            .Select(detalle => CrearDetalleVenta(empresaId, ventaId, detalle))
            .ToArray();
        var total = detalles.Sum(detalle => detalle.Total);
        var igv = detalles.Sum(detalle => detalle.Igv);
        var subtotal = total - igv;
        var pagos = CrearPagos(request.Pagos, empresaId, ventaId, total);
        var venta = new Venta(
            ventaId,
            empresaId,
            DateTimeOffset.UtcNow,
            subtotal,
            igv,
            total,
            detalles,
            pedido.SedeId,
            puntoVenta.Id,
            pedido.ClienteId,
            CanalVenta.MARKETING,
            pagos: pagos);

        var stocksAConfirmar = await PrepararConfirmacionesAsync(empresaId, pedido, cancellationToken);
        var stocksConfirmados = new List<StockAConfirmar>();
        var usuarioId = _empresaActiva.UsuarioId == Guid.Empty
            ? (Guid?)null
            : _empresaActiva.UsuarioId;

        try
        {
            foreach (var stockAConfirmar in stocksAConfirmar)
            {
                var disponibleAnterior = stockAConfirmar.Stock.CantidadDisponible;
                stockAConfirmar.Stock.ConfirmarReserva(stockAConfirmar.Cantidad);
                await _stockRepository.GuardarAsync(stockAConfirmar.Stock, cancellationToken);
                stocksConfirmados.Add(stockAConfirmar);

                if (_movimientos is not null)
                {
                    await _movimientos.AgregarAsync(
                        new MovimientoInventario(
                            Guid.NewGuid(),
                            empresaId,
                            pedido.SedeId,
                            stockAConfirmar.Stock.ProductoId,
                            stockAConfirmar.Stock.ProductoVarianteId,
                            TipoMovimientoInventario.VENTA,
                            stockAConfirmar.Cantidad,
                            disponibleAnterior,
                            stockAConfirmar.Stock.CantidadDisponible,
                            "PEDIDO_DIGITAL",
                            pedido.Id,
                            motivo: request.Observacion ?? "Conversion de pedido digital a venta",
                            usuarioId: usuarioId),
                        cancellationToken);
                }
            }

            await _ventaRepository.AgregarAsync(venta, cancellationToken);
            pedido.CompletarPorConversionAVenta(usuarioId, request.Observacion);
            await _pedidoRepository.GuardarCambiosAsync(cancellationToken);
        }
        catch
        {
            foreach (var stockAConfirmar in stocksConfirmados)
            {
                stockAConfirmar.Restaurar();
            }

            throw;
        }

        return new ConvertirPedidoDigitalAVentaResult(pedido, venta);
    }

    private async Task<PuntoVenta> ObtenerPuntoVentaAsync(
        Guid empresaId,
        PedidoDigital pedido,
        Guid? puntoVentaIdRequest,
        CancellationToken cancellationToken)
    {
        var puntoVentaId = puntoVentaIdRequest ?? pedido.PuntoVentaId;
        if (!puntoVentaId.HasValue || puntoVentaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del punto de venta es obligatorio para convertir el pedido digital a venta.",
                nameof(puntoVentaIdRequest));
        }

        var puntoVenta = await _puntoVentaRepository.ObtenerPorEmpresaAsync(
            empresaId,
            puntoVentaId.Value,
            cancellationToken);
        if (puntoVenta is null || puntoVenta.SedeId != pedido.SedeId)
        {
            throw new InvalidOperationException("El punto de venta no pertenece a la sede y empresa activas.");
        }

        if (!puntoVenta.Activo)
        {
            throw new InvalidOperationException("El punto de venta no esta activo.");
        }

        return puntoVenta;
    }

    private async Task ValidarSesionCajaAbiertaAsync(
        Guid empresaId,
        Guid puntoVentaId,
        CancellationToken cancellationToken)
    {
        var sesionCaja = await _sesionCajaRepository.ObtenerAbiertaPorPuntoVentaAsync(
            empresaId,
            puntoVentaId,
            cancellationToken);
        if (sesionCaja is null)
        {
            throw new InvalidOperationException("Debe abrir una sesion de caja antes de convertir el pedido digital a venta.");
        }
    }

    private async Task<IReadOnlyCollection<StockAConfirmar>> PrepararConfirmacionesAsync(
        Guid empresaId,
        PedidoDigital pedido,
        CancellationToken cancellationToken)
    {
        var confirmaciones = new List<StockAConfirmar>();
        var cantidadesPorStock = pedido.Detalles
            .GroupBy(detalle => new StockKey(detalle.ProductoId, detalle.ProductoVarianteId))
            .Select(grupo => new
            {
                grupo.Key.ProductoId,
                grupo.Key.ProductoVarianteId,
                Cantidad = grupo.Sum(detalle => detalle.CantidadBase)
            })
            .ToArray();

        foreach (var item in cantidadesPorStock)
        {
            var stock = await _stockRepository.ObtenerPorProductoAsync(
                empresaId,
                pedido.SedeId,
                item.ProductoId,
                item.ProductoVarianteId,
                cancellationToken);
            if (stock is null)
            {
                throw new InvalidOperationException(
                    item.ProductoVarianteId is null
                        ? $"No se encontro el stock para confirmar la reserva del producto {item.ProductoId}."
                        : $"No se encontro el stock para confirmar la reserva del producto {item.ProductoId} y variante {item.ProductoVarianteId}.");
            }

            if (stock.CantidadReservada < item.Cantidad)
            {
                throw new InvalidOperationException(
                    item.ProductoVarianteId is null
                        ? $"La reserva del producto {item.ProductoId} es insuficiente para convertir a venta."
                        : $"La reserva del producto {item.ProductoId} y variante {item.ProductoVarianteId} es insuficiente para convertir a venta.");
            }

            confirmaciones.Add(new StockAConfirmar(stock, item.Cantidad));
        }

        return confirmaciones;
    }

    private static VentaDetalle CrearDetalleVenta(
        Guid empresaId,
        Guid ventaId,
        PedidoDigitalDetalle detalle)
    {
        var total = detalle.Total;
        var subtotal = Redondear(total / 1.18m);
        var igv = Redondear(total - subtotal);
        return new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            detalle.ProductoId,
            detalle.Cantidad,
            detalle.PrecioUnitario,
            igv,
            total,
            detalle.ProductoVarianteId,
            detalle.ProductoPresentacionId,
            detalle.FactorConversionAplicado,
            detalle.CantidadBase);
    }

    private static IReadOnlyCollection<VentaPago> CrearPagos(
        IReadOnlyCollection<CrearVentaPagoRequest>? pagosRequest,
        Guid empresaId,
        Guid ventaId,
        decimal total)
    {
        if (pagosRequest is null || pagosRequest.Count == 0)
        {
            return
            [
                new VentaPago(
                    Guid.NewGuid(),
                    empresaId,
                    ventaId,
                    MetodoPago.EFECTIVO,
                    total)
            ];
        }

        var pagos = pagosRequest
            .Select(pago => new VentaPago(
                Guid.NewGuid(),
                empresaId,
                ventaId,
                NormalizarMetodoPago(pago.MetodoPago),
                pago.Monto,
                pago.CodigoOperacion,
                pago.Observacion))
            .ToArray();

        if (pagos.Sum(pago => pago.Monto) != total)
        {
            throw new ArgumentException("La suma de los pagos debe ser igual al total de la venta.", nameof(pagosRequest));
        }

        return pagos;
    }

    private static MetodoPago NormalizarMetodoPago(string? metodoPago)
    {
        if (!string.IsNullOrWhiteSpace(metodoPago)
            && Enum.TryParse<MetodoPago>(metodoPago.Trim(), true, out var metodoNormalizado)
            && Enum.IsDefined(metodoNormalizado))
        {
            return metodoNormalizado;
        }

        throw new ArgumentException("El metodo de pago no es valido.", nameof(metodoPago));
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para convertir pedidos digitales.");
        }
    }

    private static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    private sealed record StockKey(Guid ProductoId, Guid? ProductoVarianteId);

    private sealed record StockAConfirmar(StockProducto Stock, decimal Cantidad)
    {
        public void Restaurar()
        {
            Stock.Incrementar(Cantidad);
            Stock.Reservar(Cantidad);
        }
    }
}
