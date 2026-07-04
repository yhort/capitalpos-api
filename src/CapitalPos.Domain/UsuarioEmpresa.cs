namespace CapitalPos.Domain;

public sealed class UsuarioEmpresa
{
    public UsuarioEmpresa(
        Guid id,
        Guid usuarioId,
        Guid empresaId,
        RolEmpresa rol,
        bool activo = true,
        DateTimeOffset? fechaAsignacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la asignacion es obligatorio.", nameof(id));
        }

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

        var fechaAsignacionNormalizada = fechaAsignacion ?? DateTimeOffset.UtcNow;
        if (fechaAsignacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaAsignacion), "La fecha de asignacion no es valida.");
        }

        Id = id;
        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        Rol = rol;
        Activo = activo;
        FechaAsignacion = fechaAsignacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public Guid EmpresaId { get; private set; }

    public RolEmpresa Rol { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset FechaAsignacion { get; private set; }

    public void CambiarRol(RolEmpresa rol)
    {
        if (!Enum.IsDefined(rol))
        {
            throw new ArgumentOutOfRangeException(nameof(rol), "El rol de empresa no es valido.");
        }

        Rol = rol;
    }

    public void Desactivar()
    {
        Activo = false;
    }

    public void Activar()
    {
        Activo = true;
    }
}
