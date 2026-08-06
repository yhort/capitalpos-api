using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Pedidos;

public sealed class ObtenerPedidoDigitalUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IPedidoDigitalRepository _pedidoRepository;

    public ObtenerPedidoDigitalUseCase(
        IPedidoDigitalRepository pedidoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _pedidoRepository = pedidoRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<PedidoDigital?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar pedidos digitales.");
        }

        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del pedido digital es obligatorio.", nameof(id));
        }

        return _pedidoRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            id,
            cancellationToken);
    }
}
