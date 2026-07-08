namespace CapitalPos.Application.Auditoria;

public sealed record AuditoriaOperacion(
    string Operacion,
    Guid UsuarioId,
    Guid EmpresaId,
    string Recurso,
    string Accion,
    string Resultado,
    DateTimeOffset FechaUtc,
    string CorrelationId,
    string? DetalleSeguro = null);
