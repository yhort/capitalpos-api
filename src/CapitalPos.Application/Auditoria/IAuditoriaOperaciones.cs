namespace CapitalPos.Application.Auditoria;

public interface IAuditoriaOperaciones
{
    Task RegistrarAsync(
        AuditoriaOperacion operacion,
        CancellationToken cancellationToken = default);
}
