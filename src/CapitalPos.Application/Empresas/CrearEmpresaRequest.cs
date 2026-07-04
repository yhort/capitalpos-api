using CapitalPos.Domain;

namespace CapitalPos.Application.Empresas;

public sealed record CrearEmpresaRequest(
    string Ruc,
    string RazonSocial,
    string? NombreComercial = null,
    bool Activa = true)
{
    public Empresa CrearEmpresa()
    {
        return new Empresa(
            Guid.NewGuid(),
            Ruc,
            RazonSocial,
            NombreComercial,
            Activa);
    }
}
