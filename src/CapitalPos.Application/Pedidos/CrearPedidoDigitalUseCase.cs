using CapitalPos.Application.Clientes;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Pedidos;

public sealed class CrearPedidoDigitalUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IMovimientoInventarioRepository? _movimientos;
    private readonly IPedidoDigitalRepository _pedidoRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoPresentacionRepository _productoPresentacionRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IPuntoVentaRepository _puntoVentaRepository;
    private readonly ISedeRepository _sedeRepository;
    private readonly IStockProductoRepository _stockRepository;

    public CrearPedidoDigitalUseCase(
        IPedidoDigitalRepository pedidoRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IProductoPresentacionRepository productoPresentacionRepository,
        IClienteRepository clienteRepository,
        ISedeRepository sedeRepository,
        IPuntoVentaRepository puntoVentaRepository,
        IStockProductoRepository stockRepository,
        IEmpresaActivaContext empresaActiva,
        IMovimientoInventarioRepository? movimientos = null)
    {
        _pedidoRepository = pedidoRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _productoPresentacionRepository = productoPresentacionRepository;
        _clienteRepository = clienteRepository;
        _sedeRepository = sedeRepository;
        _puntoVentaRepository = puntoVentaRepository;
        _stockRepository = stockRepository;
        _empresaActiva = empresaActiva;
        _movimientos = movimientos;
    }

    public async Task<PedidoDigital> EjecutarAsync(
        CrearPedidoDigitalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();
        var empresaId = _empresaActiva.EmpresaId;
        var canal = NormalizarCanal(request.CanalPedido);

        await ValidarClienteAsync(empresaId, request.ClienteId, cancellationToken);
        await ValidarSedeAsync(empresaId, request.SedeId, cancellationToken);
        await ValidarPuntoVentaAsync(empresaId, request.SedeId, request.PuntoVentaId, cancellationToken);

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            throw new ArgumentException("El pedido digital debe tener al menos un detalle.", nameof(request));
        }

        var pedidoId = Guid.NewGuid();
        var detalles = new List<PedidoDigitalDetalle>();
        foreach (var detalleRequest in request.Detalles)
        {
            detalles.Add(await CrearDetalleAsync(empresaId, pedidoId, detalleRequest, cancellationToken));
        }

        var stocksAReservar = await ValidarYPrepararReservasAsync(
            empresaId,
            request.SedeId,
            detalles,
            cancellationToken);

        var historial = new PedidoDigitalHistorialEstado(
            Guid.NewGuid(),
            empresaId,
            pedidoId,
            null,
            EstadoPedidoDigital.PendientePago,
            _empresaActiva.UsuarioId == Guid.Empty ? null : _empresaActiva.UsuarioId,
            observacion: "Creacion del pedido digital.");
        var pedido = new PedidoDigital(
            pedidoId,
            empresaId,
            request.SedeId,
            canal,
            request.FechaPedido ?? DateTimeOffset.UtcNow,
            detalles,
            request.ClienteId,
            request.PuntoVentaId,
            request.ReferenciaExterna,
            request.Observacion,
            historialEstados: [historial]);

        try
        {
            foreach (var stockAReservar in stocksAReservar)
            {
                var stockLibreAnterior = stockAReservar.Stock.CantidadLibre;
                stockAReservar.Stock.Reservar(stockAReservar.Cantidad);
                await _stockRepository.GuardarAsync(stockAReservar.Stock, cancellationToken);

                if (_movimientos is not null)
                {
                    await _movimientos.AgregarAsync(
                        new MovimientoInventario(
                            Guid.NewGuid(),
                            empresaId,
                            request.SedeId,
                            stockAReservar.Stock.ProductoId,
                            stockAReservar.Stock.ProductoVarianteId,
                            TipoMovimientoInventario.RESERVA,
                            stockAReservar.Cantidad,
                            stockLibreAnterior,
                            stockAReservar.Stock.CantidadLibre,
                            "PEDIDO_DIGITAL",
                            pedidoId,
                            motivo: "Reserva por pedido digital",
                            usuarioId: _empresaActiva.UsuarioId == Guid.Empty
                                ? null
                                : _empresaActiva.UsuarioId),
                        cancellationToken);
                }
            }

            await _pedidoRepository.AgregarAsync(pedido, cancellationToken);
        }
        catch
        {
            foreach (var stockAReservar in stocksAReservar)
            {
                stockAReservar.Restaurar();
            }

            throw;
        }

        return pedido;
    }

    private async Task<IReadOnlyCollection<StockAReservar>> ValidarYPrepararReservasAsync(
        Guid empresaId,
        Guid sedeId,
        IReadOnlyCollection<PedidoDigitalDetalle> detalles,
        CancellationToken cancellationToken)
    {
        var reservas = new List<StockAReservar>();
        var cantidadesPorStock = detalles
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
                sedeId,
                item.ProductoId,
                item.ProductoVarianteId,
                cancellationToken);
            if (stock is null)
            {
                throw new InvalidOperationException(
                    CrearMensajeStockNoDisponible(item.ProductoId, item.ProductoVarianteId));
            }

            if (stock.CantidadLibre < item.Cantidad)
            {
                throw new InvalidOperationException(
                    CrearMensajeStockInsuficiente(item.ProductoId, item.ProductoVarianteId));
            }

            reservas.Add(new StockAReservar(stock, item.Cantidad));
        }

        return reservas;
    }

    private async Task<PedidoDigitalDetalle> CrearDetalleAsync(
        Guid empresaId,
        Guid pedidoId,
        CrearPedidoDigitalDetalleRequest detalleRequest,
        CancellationToken cancellationToken)
    {
        var producto = await _productoRepository.ObtenerPorEmpresaAsync(
            empresaId,
            detalleRequest.ProductoId,
            cancellationToken);
        if (producto is null)
        {
            throw new InvalidOperationException("El producto no pertenece a la empresa activa.");
        }

        await ValidarVarianteAsync(empresaId, detalleRequest, cancellationToken);
        var factorConversionAplicado = 1m;
        if (detalleRequest.ProductoPresentacionId.HasValue)
        {
            var presentacion = await _productoPresentacionRepository.ObtenerPorEmpresaAsync(
                empresaId,
                detalleRequest.ProductoPresentacionId.Value,
                cancellationToken);
            if (presentacion is null)
            {
                throw new InvalidOperationException("La presentacion no pertenece a la empresa activa.");
            }

            if (presentacion.ProductoId != detalleRequest.ProductoId)
            {
                throw new InvalidOperationException("La presentacion no pertenece al producto activo.");
            }

            if (!presentacion.Activa)
            {
                throw new InvalidOperationException("La presentacion del producto no esta activa.");
            }

            factorConversionAplicado = presentacion.FactorConversion;
        }

        var descripcion = string.IsNullOrWhiteSpace(detalleRequest.Descripcion)
            ? producto.Nombre
            : detalleRequest.Descripcion;
        return new PedidoDigitalDetalle(
            Guid.NewGuid(),
            empresaId,
            pedidoId,
            detalleRequest.ProductoId,
            descripcion,
            detalleRequest.Cantidad,
            detalleRequest.PrecioUnitario,
            detalleRequest.ProductoVarianteId,
            detalleRequest.ProductoPresentacionId,
            factorConversionAplicado);
    }

    private async Task ValidarVarianteAsync(
        Guid empresaId,
        CrearPedidoDigitalDetalleRequest detalleRequest,
        CancellationToken cancellationToken)
    {
        if (!detalleRequest.ProductoVarianteId.HasValue)
        {
            return;
        }

        var variante = await _productoVarianteRepository.ObtenerPorEmpresaAsync(
            empresaId,
            detalleRequest.ProductoVarianteId.Value,
            cancellationToken);
        if (variante is null || variante.ProductoId != detalleRequest.ProductoId)
        {
            throw new InvalidOperationException("La variante no pertenece al producto y empresa activos.");
        }
    }

    private async Task ValidarClienteAsync(
        Guid empresaId,
        Guid? clienteId,
        CancellationToken cancellationToken)
    {
        if (!clienteId.HasValue)
        {
            return;
        }

        if (clienteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del cliente no puede estar vacio.", nameof(clienteId));
        }

        var cliente = await _clienteRepository.ObtenerPorEmpresaAsync(
            empresaId,
            clienteId.Value,
            cancellationToken);
        if (cliente is null)
        {
            throw new InvalidOperationException("El cliente no pertenece a la empresa activa.");
        }
    }

    private async Task ValidarSedeAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken)
    {
        var sede = await _sedeRepository.ObtenerPorEmpresaAsync(empresaId, sedeId, cancellationToken);
        if (sede is null)
        {
            throw new InvalidOperationException("La sede no pertenece a la empresa activa.");
        }

        if (!sede.Activa)
        {
            throw new InvalidOperationException("La sede no esta activa.");
        }
    }

    private async Task ValidarPuntoVentaAsync(
        Guid empresaId,
        Guid sedeId,
        Guid? puntoVentaId,
        CancellationToken cancellationToken)
    {
        if (!puntoVentaId.HasValue)
        {
            return;
        }

        if (puntoVentaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta no puede estar vacio.", nameof(puntoVentaId));
        }

        var puntoVenta = await _puntoVentaRepository.ObtenerPorEmpresaAsync(
            empresaId,
            puntoVentaId.Value,
            cancellationToken);
        if (puntoVenta is null || puntoVenta.SedeId != sedeId)
        {
            throw new InvalidOperationException("El punto de venta no pertenece a la sede y empresa activas.");
        }

        if (!puntoVenta.Activo)
        {
            throw new InvalidOperationException("El punto de venta no esta activo.");
        }
    }

    private static CanalPedidoDigital NormalizarCanal(string canalPedido)
    {
        if (string.IsNullOrWhiteSpace(canalPedido))
        {
            throw new ArgumentException("El canal del pedido digital es obligatorio.", nameof(canalPedido));
        }

        if (Enum.TryParse<CanalPedidoDigital>(
            canalPedido.Trim(),
            ignoreCase: true,
            out var canal) &&
            Enum.IsDefined(canal))
        {
            return canal;
        }

        throw new ArgumentException("El canal del pedido digital no es valido.", nameof(canalPedido));
    }

    private static string CrearMensajeStockNoDisponible(Guid productoId, Guid? productoVarianteId)
    {
        return productoVarianteId is null
            ? $"No existe stock registrado para el producto {productoId}."
            : $"No existe stock registrado para el producto {productoId} y variante {productoVarianteId}.";
    }

    private static string CrearMensajeStockInsuficiente(Guid productoId, Guid? productoVarianteId)
    {
        return productoVarianteId is null
            ? $"Stock libre insuficiente para el producto {productoId}."
            : $"Stock libre insuficiente para el producto {productoId} y variante {productoVarianteId}.";
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar pedidos digitales.");
        }
    }

    private sealed record StockKey(Guid ProductoId, Guid? ProductoVarianteId);

    private sealed record StockAReservar(StockProducto Stock, decimal Cantidad)
    {
        public void Restaurar()
        {
            if (Stock.CantidadReservada >= Cantidad)
            {
                Stock.LiberarReserva(Cantidad);
            }
        }
    }
}
