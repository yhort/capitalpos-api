using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Pedidos;

public sealed record ActualizarEstadoPedidoDigitalRequest(
    string Estado,
    string? Observacion = null);

public sealed class ActualizarEstadoPedidoDigitalUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IPedidoDigitalRepository _pedidoRepository;

    public ActualizarEstadoPedidoDigitalUseCase(
        IPedidoDigitalRepository pedidoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _pedidoRepository = pedidoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<PedidoDigital?> EjecutarAsync(
        Guid pedidoId,
        ActualizarEstadoPedidoDigitalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (pedidoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del pedido digital es obligatorio.", nameof(pedidoId));
        }

        if (string.IsNullOrWhiteSpace(request.Estado))
        {
            throw new ArgumentException("El estado del pedido digital es obligatorio.", nameof(request));
        }

        if (!Enum.TryParse<EstadoPedidoDigital>(request.Estado, true, out var estadoNuevo)
            || !Enum.IsDefined(estadoNuevo))
        {
            throw new ArgumentException("El estado del pedido digital no es valido.", nameof(request));
        }

        ValidarEmpresaActiva();
        var empresaId = _empresaActiva.EmpresaId;
        var pedido = await _pedidoRepository.ObtenerPorEmpresaAsync(empresaId, pedidoId, cancellationToken);
        if (pedido is null)
        {
            return null;
        }

        var usuarioId = _empresaActiva.UsuarioId == Guid.Empty
            ? (Guid?)null
            : _empresaActiva.UsuarioId;
        pedido.ActualizarEstadoOperativo(estadoNuevo, usuarioId, request.Observacion);
        await _pedidoRepository.GuardarCambiosAsync(cancellationToken);
        return pedido;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "La empresa activa es obligatoria para actualizar el estado de pedidos digitales.");
        }
    }
}
