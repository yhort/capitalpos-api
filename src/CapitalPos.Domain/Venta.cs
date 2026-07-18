namespace CapitalPos.Domain;

public sealed class Venta
{
    private readonly List<VentaDetalle> _detalles = new();

    private Venta()
    {
    }

    public Venta(
        Guid id,
        Guid empresaId,
        DateTimeOffset fecha,
        decimal subtotal,
        decimal igv,
        decimal total,
        IReadOnlyCollection<VentaDetalle> detalles,
        Guid sedeId,
        Guid puntoVentaId,
        Guid? clienteId = null,
        CanalVenta canalVenta = CanalVenta.TIENDA,
        Guid? vendedorId = null,
        EstadoVenta estado = EstadoVenta.Registrada,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la venta es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (fecha == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fecha), "La fecha de venta no es valida.");
        }

        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        if (puntoVentaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta es obligatorio.", nameof(puntoVentaId));
        }

        if (clienteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del cliente no puede estar vacio.", nameof(clienteId));
        }

        if (!Enum.IsDefined(canalVenta))
        {
            throw new ArgumentOutOfRangeException(nameof(canalVenta), "El canal de venta no es valido.");
        }

        if (vendedorId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del vendedor no puede estar vacio.", nameof(vendedorId));
        }

        if (!Enum.IsDefined(estado))
        {
            throw new ArgumentOutOfRangeException(nameof(estado), "El estado de venta no es valido.");
        }

        if (subtotal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subtotal), "El subtotal no puede ser negativo.");
        }

        if (igv < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(igv), "El IGV no puede ser negativo.");
        }

        if (total <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "El total de la venta debe ser mayor que cero.");
        }

        ArgumentNullException.ThrowIfNull(detalles);
        if (detalles.Count == 0)
        {
            throw new ArgumentException("La venta debe tener al menos un detalle.", nameof(detalles));
        }

        if (detalles.Any(detalle => detalle.EmpresaId != empresaId || detalle.VentaId != id))
        {
            throw new ArgumentException("Todos los detalles deben pertenecer a la misma venta y empresa.", nameof(detalles));
        }

        var totalDetalles = detalles.Sum(detalle => detalle.Total);
        var igvDetalles = detalles.Sum(detalle => detalle.Igv);
        var subtotalDetalles = totalDetalles - igvDetalles;
        if (subtotal != subtotalDetalles || igv != igvDetalles || total != totalDetalles)
        {
            throw new ArgumentException("Los totales de la venta deben coincidir con sus detalles.", nameof(detalles));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        SedeId = sedeId;
        PuntoVentaId = puntoVentaId;
        ClienteId = clienteId;
        CanalVenta = canalVenta;
        VendedorId = vendedorId;
        Fecha = fecha;
        Subtotal = subtotal;
        Igv = igv;
        Total = total;
        Estado = estado;
        FechaCreacion = fechaCreacionNormalizada;
        _detalles.AddRange(detalles);
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid SedeId { get; private set; }

    public Guid PuntoVentaId { get; private set; }

    public Guid? ClienteId { get; private set; }

    public CanalVenta CanalVenta { get; private set; }

    public Guid? VendedorId { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal Igv { get; private set; }

    public decimal Total { get; private set; }

    public EstadoVenta Estado { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public IReadOnlyCollection<VentaDetalle> Detalles => _detalles;

    public void Anular()
    {
        Estado = EstadoVenta.Anulada;
    }
}
