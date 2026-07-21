namespace CapitalPos.Application.Caja;

public sealed record CerrarSesionCajaRequest(
    Guid SesionCajaId,
    decimal MontoDeclaradoCierre,
    string? ObservacionCierre = null,
    Guid? UsuarioCierreId = null);
