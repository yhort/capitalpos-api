using CapitalPos.Application.Clientes;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Ventas;

public sealed class CrearVentaUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IVentaRepository _ventaRepository;

    public CrearVentaUseCase(
        IVentaRepository ventaRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IClienteRepository clienteRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _ventaRepository = ventaRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _clienteRepository = clienteRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<Venta> EjecutarAsync(
        CrearVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var empresaId = _empresaActiva.EmpresaId;
        await ValidarClienteAsync(empresaId, request.ClienteId, cancellationToken);

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            throw new ArgumentException("La venta debe tener al menos un detalle.", nameof(request));
        }

        var ventaId = Guid.NewGuid();
        var detalles = new List<VentaDetalle>();
        foreach (var detalleRequest in request.Detalles)
        {
            await ValidarProductoAsync(empresaId, detalleRequest, cancellationToken);
            detalles.Add(detalleRequest.CrearDetalle(empresaId, ventaId));
        }

        var total = detalles.Sum(detalle => detalle.Total);
        var igv = detalles.Sum(detalle => detalle.Igv);
        var subtotal = total - igv;
        var venta = new Venta(
            ventaId,
            empresaId,
            request.Fecha ?? DateTimeOffset.UtcNow,
            subtotal,
            igv,
            total,
            detalles,
            request.ClienteId);

        await _ventaRepository.AgregarAsync(venta, cancellationToken);

        return venta;
    }

    private async Task ValidarClienteAsync(
        Guid empresaId,
        Guid? clienteId,
        CancellationToken cancellationToken)
    {
        if (clienteId is null)
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

    private async Task ValidarProductoAsync(
        Guid empresaId,
        CrearVentaDetalleRequest detalleRequest,
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

        if (detalleRequest.ProductoVarianteId is null)
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

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar ventas.");
        }
    }
}
