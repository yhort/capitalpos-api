using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Clientes;

public sealed class ListarClientesUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public ListarClientesUseCase(
        IClienteRepository clienteRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _clienteRepository = clienteRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<IReadOnlyCollection<Cliente>> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        return _clienteRepository.ListarPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            cancellationToken);
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar clientes.");
        }
    }
}
