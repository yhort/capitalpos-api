using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed class ListarUnidadesMedidaUseCase
{
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;

    public ListarUnidadesMedidaUseCase(IUnidadMedidaRepository unidadMedidaRepository)
    {
        _unidadMedidaRepository = unidadMedidaRepository;
    }

    public async Task<IReadOnlyCollection<UnidadMedida>> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var unidades = await _unidadMedidaRepository.ListarAsync(cancellationToken);

        return unidades
            .Where(unidad => unidad.Activa)
            .OrderBy(unidad => unidad.Codigo, StringComparer.Ordinal)
            .ToArray();
    }
}
