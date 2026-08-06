namespace CapitalPos.Domain;

public sealed class PedidoDigitalHistorialEstado
{
    private PedidoDigitalHistorialEstado()
    {
        Observacion = string.Empty;
    }

    public PedidoDigitalHistorialEstado(
        Guid id,
        Guid empresaId,
        Guid pedidoDigitalId,
        EstadoPedidoDigital? estadoAnterior,
        EstadoPedidoDigital estadoNuevo,
        Guid? usuarioId = null,
        DateTimeOffset? fecha = null,
        string? observacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del historial de pedido digital es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (pedidoDigitalId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del pedido digital es obligatorio.", nameof(pedidoDigitalId));
        }

        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario no puede estar vacio.", nameof(usuarioId));
        }

        if (estadoAnterior.HasValue && !Enum.IsDefined(estadoAnterior.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(estadoAnterior), "El estado anterior del pedido digital no es valido.");
        }

        if (!Enum.IsDefined(estadoNuevo))
        {
            throw new ArgumentOutOfRangeException(nameof(estadoNuevo), "El estado nuevo del pedido digital no es valido.");
        }

        var fechaNormalizada = fecha ?? DateTimeOffset.UtcNow;
        if (fechaNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fecha), "La fecha del historial no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        PedidoDigitalId = pedidoDigitalId;
        EstadoAnterior = estadoAnterior;
        EstadoNuevo = estadoNuevo;
        UsuarioId = usuarioId;
        Fecha = fechaNormalizada;
        Observacion = NormalizarObservacion(observacion);
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid PedidoDigitalId { get; private set; }

    public EstadoPedidoDigital? EstadoAnterior { get; private set; }

    public EstadoPedidoDigital EstadoNuevo { get; private set; }

    public Guid? UsuarioId { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public string Observacion { get; private set; }

    private static string NormalizarObservacion(string? observacion)
    {
        var valorNormalizado = observacion?.Trim();
        if (valorNormalizado is { Length: > 500 })
        {
            throw new ArgumentException("La observacion del historial no debe exceder 500 caracteres.", nameof(observacion));
        }

        return valorNormalizado ?? string.Empty;
    }
}
