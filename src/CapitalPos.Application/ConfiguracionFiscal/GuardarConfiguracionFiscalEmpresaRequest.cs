namespace CapitalPos.Application.ConfiguracionFiscal;

public sealed record GuardarConfiguracionFiscalEmpresaRequest(
    string Ruc,
    string RazonSocial,
    string? NombreComercial,
    string Ubigeo,
    string Direccion,
    string Departamento,
    string Provincia,
    string Distrito,
    bool Activa = true);
