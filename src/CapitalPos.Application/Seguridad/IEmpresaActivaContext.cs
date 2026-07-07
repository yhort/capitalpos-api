using CapitalPos.Domain;

namespace CapitalPos.Application.Seguridad;

public interface IEmpresaActivaContext
{
    bool TieneEmpresaActiva { get; }

    Guid UsuarioId { get; }

    Guid EmpresaId { get; }

    RolEmpresa Rol { get; }
}
