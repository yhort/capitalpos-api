namespace CapitalPos.Application.Ventas;

public sealed record RegistrarComprobanteCpeRequest(
    Guid VentaId,
    string TipoComprobante,
    string Serie,
    int Correlativo,
    string EstadoCpe,
    string? Mensaje = null,
    string? Hash = null,
    string? NombreXml = null,
    string? NombreZip = null,
    string? NombreCdr = null);
