namespace CapitalPos.Application.Ventas;

public sealed record EmitirCpeDesdeVentaRequest(
    string TipoComprobante,
    string Serie,
    int Correlativo,
    string RucEmisor);
