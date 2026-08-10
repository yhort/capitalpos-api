namespace CapitalPos.Domain;

public sealed class PedidoDigital
{
    private readonly List<PedidoDigitalDetalle> _detalles = new();
    private readonly List<PedidoDigitalHistorialEstado> _historialEstados = new();

    private PedidoDigital()
    {
        ReferenciaExterna = string.Empty;
        Observacion = string.Empty;
    }

    public PedidoDigital(
        Guid id,
        Guid empresaId,
        Guid sedeId,
        CanalPedidoDigital canalPedido,
        DateTimeOffset fechaPedido,
        IReadOnlyCollection<PedidoDigitalDetalle> detalles,
        Guid? clienteId = null,
        Guid? puntoVentaId = null,
        string? referenciaExterna = null,
        string? observacion = null,
        EstadoPedidoDigital estado = EstadoPedidoDigital.PendientePago,
        DateTimeOffset? fechaCreacion = null,
        DateTimeOffset? fechaActualizacion = null,
        IReadOnlyCollection<PedidoDigitalHistorialEstado>? historialEstados = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del pedido digital es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        if (clienteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del cliente no puede estar vacio.", nameof(clienteId));
        }

        if (puntoVentaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta no puede estar vacio.", nameof(puntoVentaId));
        }

        if (!Enum.IsDefined(canalPedido))
        {
            throw new ArgumentOutOfRangeException(nameof(canalPedido), "El canal del pedido digital no es valido.");
        }

        if (!Enum.IsDefined(estado))
        {
            throw new ArgumentOutOfRangeException(nameof(estado), "El estado del pedido digital no es valido.");
        }

        if (fechaPedido == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaPedido), "La fecha del pedido digital no es valida.");
        }

        ArgumentNullException.ThrowIfNull(detalles);
        if (detalles.Count == 0)
        {
            throw new ArgumentException("El pedido digital debe tener al menos un detalle.", nameof(detalles));
        }

        if (detalles.Any(detalle => detalle.EmpresaId != empresaId || detalle.PedidoDigitalId != id))
        {
            throw new ArgumentException("Todos los detalles deben pertenecer al mismo pedido digital y empresa.", nameof(detalles));
        }

        if (historialEstados is not null &&
            historialEstados.Any(historial => historial.EmpresaId != empresaId || historial.PedidoDigitalId != id))
        {
            throw new ArgumentException("Todo el historial debe pertenecer al mismo pedido digital y empresa.", nameof(historialEstados));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        var fechaActualizacionNormalizada = fechaActualizacion ?? fechaCreacionNormalizada;
        if (fechaActualizacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaActualizacion), "La fecha de actualizacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        ClienteId = clienteId;
        SedeId = sedeId;
        PuntoVentaId = puntoVentaId;
        CanalPedido = canalPedido;
        Estado = estado;
        FechaPedido = fechaPedido;
        Total = detalles.Sum(detalle => detalle.Total);
        Subtotal = Redondear(Total / 1.18m);
        Igv = Redondear(Total - Subtotal);
        ReferenciaExterna = NormalizarTexto(referenciaExterna);
        Observacion = NormalizarTexto(observacion);
        FechaCreacion = fechaCreacionNormalizada;
        FechaActualizacion = fechaActualizacionNormalizada;
        _detalles.AddRange(detalles);
        if (historialEstados is not null)
        {
            _historialEstados.AddRange(historialEstados);
        }
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid? ClienteId { get; private set; }

    public Guid SedeId { get; private set; }

    public Guid? PuntoVentaId { get; private set; }

    public CanalPedidoDigital CanalPedido { get; private set; }

    public EstadoPedidoDigital Estado { get; private set; }

    public DateTimeOffset FechaPedido { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal Igv { get; private set; }

    public decimal Total { get; private set; }

    public string ReferenciaExterna { get; private set; }

    public string Observacion { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public DateTimeOffset FechaActualizacion { get; private set; }

    public IReadOnlyCollection<PedidoDigitalDetalle> Detalles => _detalles;

    public IReadOnlyCollection<PedidoDigitalHistorialEstado> HistorialEstados => _historialEstados;

    public void ActualizarEstadoOperativo(
        EstadoPedidoDigital estadoNuevo,
        Guid? usuarioId = null,
        string? observacion = null)
    {
        if (Estado is EstadoPedidoDigital.Cancelado or EstadoPedidoDigital.Entregado)
        {
            throw new InvalidOperationException(
                "No se puede actualizar el estado de un pedido digital cancelado o entregado.");
        }

        if (estadoNuevo is not (
            EstadoPedidoDigital.Pagado or
            EstadoPedidoDigital.Empaquetado or
            EstadoPedidoDigital.PendienteEntrega))
        {
            throw new ArgumentOutOfRangeException(
                nameof(estadoNuevo),
                "Solo se permiten los estados operativos Pagado, Empaquetado o PendienteEntrega.");
        }

        var estadoEsperado = Estado switch
        {
            EstadoPedidoDigital.PendientePago => EstadoPedidoDigital.Pagado,
            EstadoPedidoDigital.Pagado => EstadoPedidoDigital.Empaquetado,
            EstadoPedidoDigital.Empaquetado => EstadoPedidoDigital.PendienteEntrega,
            _ => throw new InvalidOperationException(
                $"No hay transicion operativa definida desde el estado {Estado}.")
        };

        if (estadoNuevo != estadoEsperado)
        {
            throw new InvalidOperationException(
                $"Transicion invalida: desde {Estado} solo se permite avanzar a {estadoEsperado}.");
        }

        CambiarEstado(
            estadoNuevo,
            usuarioId,
            string.IsNullOrWhiteSpace(observacion)
                ? $"Actualizacion de estado a {estadoNuevo}."
                : observacion);
    }

    public void Cancelar(Guid? usuarioId = null, string? observacion = null)
    {
        if (Estado == EstadoPedidoDigital.Cancelado)
        {
            throw new InvalidOperationException("El pedido digital ya se encuentra cancelado.");
        }

        if (Estado == EstadoPedidoDigital.Entregado)
        {
            throw new InvalidOperationException("No se puede cancelar un pedido digital ya entregado.");
        }

        CambiarEstado(
            EstadoPedidoDigital.Cancelado,
            usuarioId,
            string.IsNullOrWhiteSpace(observacion)
                ? "Cancelacion del pedido digital."
                : observacion);
    }

    public void CompletarPorConversionAVenta(Guid? usuarioId = null, string? observacion = null)
    {
        if (Estado == EstadoPedidoDigital.Cancelado)
        {
            throw new InvalidOperationException("No se puede convertir un pedido digital cancelado.");
        }

        if (Estado == EstadoPedidoDigital.Entregado)
        {
            throw new InvalidOperationException("El pedido digital ya fue convertido o entregado.");
        }

        CambiarEstado(
            EstadoPedidoDigital.Entregado,
            usuarioId,
            string.IsNullOrWhiteSpace(observacion)
                ? "Conversion del pedido digital a venta."
                : observacion);
    }

    private void CambiarEstado(
        EstadoPedidoDigital estadoNuevo,
        Guid? usuarioId,
        string observacion)
    {
        var estadoAnterior = Estado;
        Estado = estadoNuevo;
        FechaActualizacion = DateTimeOffset.UtcNow;
        _historialEstados.Add(new PedidoDigitalHistorialEstado(
            Guid.NewGuid(),
            EmpresaId,
            Id,
            estadoAnterior,
            estadoNuevo,
            usuarioId,
            observacion: observacion));
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }

    private static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
