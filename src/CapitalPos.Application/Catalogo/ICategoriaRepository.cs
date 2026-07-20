using CapitalPos.Domain;

namespace CapitalPos.Application.Catalogo;

public interface ICategoriaRepository
{
    Task AgregarAsync(Categoria categoria, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Categoria>> ListarPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<Categoria?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreAsync(
        Guid empresaId,
        string nombre,
        CancellationToken cancellationToken = default);
}
