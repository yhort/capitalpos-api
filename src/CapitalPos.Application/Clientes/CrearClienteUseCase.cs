using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Clientes;

public sealed class CrearClienteUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public CrearClienteUseCase(
        IClienteRepository clienteRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _clienteRepository = clienteRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<Cliente> EjecutarAsync(
        CrearClienteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var cliente = request.CrearCliente(_empresaActiva.EmpresaId);
        await _clienteRepository.AgregarAsync(cliente, cancellationToken);

        return cliente;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar clientes.");
        }
    }
}
