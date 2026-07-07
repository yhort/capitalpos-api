using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.ActiveCompany;

public sealed class EmpresaActivaContext : IEmpresaActivaContext
{
    public bool TieneEmpresaActiva { get; private set; }

    public Guid UsuarioId { get; private set; }

    public Guid EmpresaId { get; private set; }

    public RolEmpresa Rol { get; private set; }

    public void Establecer(Guid usuarioId, Guid empresaId, RolEmpresa rol)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(usuarioId));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (!Enum.IsDefined(rol))
        {
            throw new ArgumentOutOfRangeException(nameof(rol), "El rol de empresa no es valido.");
        }

        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        Rol = rol;
        TieneEmpresaActiva = true;
    }
}
