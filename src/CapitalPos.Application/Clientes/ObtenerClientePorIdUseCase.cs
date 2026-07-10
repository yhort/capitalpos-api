using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Clientes;

public sealed class ObtenerClientePorIdUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmpresaActivaContext _empresaActiva;

    public ObtenerClientePorIdUseCase(
        IClienteRepository clienteRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _clienteRepository = clienteRepository;
        _empresaActiva = empresaActiva;
    }

    public Task<Cliente?> EjecutarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidarEmpresaActiva();

        return _clienteRepository.ObtenerPorEmpresaAsync(
            _empresaActiva.EmpresaId,
            id,
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
