namespace CapitalPos.Application.Ventas;

public sealed record EmitirCpeDesdeVentaRequest(
    string TipoComprobante,
    string Serie,
    int Correlativo,
    string RucEmisor);

public sealed record EmitirCpeDesdeVentaResult(
    CapitalPos.Application.Cpe.CpeGatewayResponse GatewayResponse,
    string TipoComprobante,
    string Serie,
    int Correlativo);
