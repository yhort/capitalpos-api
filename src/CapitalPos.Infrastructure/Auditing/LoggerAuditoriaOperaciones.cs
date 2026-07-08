using CapitalPos.Application.Auditoria;
using Microsoft.Extensions.Logging;

namespace CapitalPos.Infrastructure.Auditing;

public sealed class LoggerAuditoriaOperaciones : IAuditoriaOperaciones
{
    private readonly ILogger<LoggerAuditoriaOperaciones> _logger;

    public LoggerAuditoriaOperaciones(ILogger<LoggerAuditoriaOperaciones> logger)
    {
        _logger = logger;
    }

    public Task RegistrarAsync(
        AuditoriaOperacion operacion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Auditoria de operacion {Operacion}: usuario {UsuarioId}, empresa {EmpresaId}, recurso {Recurso}, accion {Accion}, resultado {Resultado}, correlation {CorrelationId}, fecha {FechaUtc}, detalle {DetalleSeguro}.",
            operacion.Operacion,
            operacion.UsuarioId,
            operacion.EmpresaId,
            operacion.Recurso,
            operacion.Accion,
            operacion.Resultado,
            operacion.CorrelationId,
            operacion.FechaUtc.ToUniversalTime(),
            operacion.DetalleSeguro);

        return Task.CompletedTask;
    }
}
