using CapitalPos.Domain;
namespace CapitalPos.Application.Inventario;
public interface IMovimientoInventarioRepository { Task AgregarAsync(MovimientoInventario movimiento, CancellationToken cancellationToken = default); Task<IReadOnlyCollection<MovimientoInventario>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default); }
