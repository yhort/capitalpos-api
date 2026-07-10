using CapitalPos.Domain;

namespace CapitalPos.Application.Clientes;

public sealed record CrearClienteRequest(
    string TipoDocumento,
    string? NumeroDocumento,
    string NombreRazonSocial,
    string? Direccion = null,
    bool Activo = true)
{
    public Cliente CrearCliente(Guid empresaId)
    {
        return new Cliente(
            Guid.NewGuid(),
            empresaId,
            TipoDocumento,
            NumeroDocumento,
            NombreRazonSocial,
            Direccion,
            Activo);
    }
}
