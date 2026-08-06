using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Pedidos;

public sealed class ListarPedidosDigitalesUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IPedidoDigitalRepository _pedidoRepository;

    public ListarPedidosDigitalesUseCase(
        IPedidoDigitalRepository pedidoRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _pedidoRepository = pedidoRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<IReadOnlyCollection<PedidoDigital>> EjecutarAsync(
        EstadoPedidoDigital? estado = null,
        CanalPedidoDigital? canalPedido = null,
        Guid? sedeId = null,
        CancellationToken cancellationToken = default)
    {
        ValidarEntrada(sedeId);
        ValidarEmpresaActiva();

        var pedidos = await _pedidoRepository.ListarPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);

        return pedidos
            .Where(pedido =>
                (!estado.HasValue || pedido.Estado == estado.Value) &&
                (!canalPedido.HasValue || pedido.CanalPedido == canalPedido.Value) &&
                (!sedeId.HasValue || pedido.SedeId == sedeId.Value))
            .OrderByDescending(pedido => pedido.FechaPedido)
            .ToArray();
    }

    private static void ValidarEntrada(Guid? sedeId)
    {
        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede no puede estar vacio.", nameof(sedeId));
        }
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para consultar pedidos digitales.");
        }
    }
}
