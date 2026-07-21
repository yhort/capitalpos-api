namespace CapitalPos.Application.Caja;

public sealed record AbrirSesionCajaRequest(
    Guid PuntoVentaId,
    decimal MontoInicial,
    string? ObservacionApertura = null,
    Guid? UsuarioAperturaId = null);
