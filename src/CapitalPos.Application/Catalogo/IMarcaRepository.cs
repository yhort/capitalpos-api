using CapitalPos.Domain;

namespace CapitalPos.Application.Catalogo;

public interface IMarcaRepository
{
    Task AgregarAsync(Marca marca, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Marca>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<Marca?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreAsync(
        Guid empresaId,
        string nombre,
        CancellationToken cancellationToken = default);
}
