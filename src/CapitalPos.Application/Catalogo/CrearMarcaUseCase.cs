using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Application.Catalogo;

public sealed class CrearMarcaUseCase
{
    private readonly IEmpresaActivaContext _empresaActiva;
    private readonly IMarcaRepository _marcaRepository;

    public CrearMarcaUseCase(
        IMarcaRepository marcaRepository,
        IEmpresaActivaContext empresaActiva)
    {
        _marcaRepository = marcaRepository;
        _empresaActiva = empresaActiva;
    }

    public async Task<Marca> EjecutarAsync(
        CrearMarcaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarEmpresaActiva();

        var marca = request.CrearMarca(_empresaActiva.EmpresaId);
        await _marcaRepository.AgregarAsync(marca, cancellationToken);

        return marca;
    }

    private void ValidarEmpresaActiva()
    {
        if (!_empresaActiva.TieneEmpresaActiva || _empresaActiva.EmpresaId == Guid.Empty)
        {
            throw new InvalidOperationException("La empresa activa es obligatoria para operar marcas.");
        }
    }
}
