namespace CapitalPos.Domain;

public sealed class Sede
{
    private Sede()
    {
        Nombre = string.Empty;
        CodigoEstablecimiento = string.Empty;
        Direccion = string.Empty;
        Distrito = string.Empty;
        Provincia = string.Empty;
        Departamento = string.Empty;
    }

    public Sede(
        Guid id,
        Guid empresaId,
        string nombre,
        TipoSede tipo,
        string? codigoEstablecimiento = null,
        string? direccion = null,
        string? distrito = null,
        string? provincia = null,
        string? departamento = null,
        bool activa = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        var nombreNormalizado = NormalizarTexto(nombre);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ArgumentException("El nombre de la sede es obligatorio.", nameof(nombre));
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), "El tipo de sede no es valido.");
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        Nombre = nombreNormalizado;
        Tipo = tipo;
        CodigoEstablecimiento = NormalizarTexto(codigoEstablecimiento);
        Direccion = NormalizarTexto(direccion);
        Distrito = NormalizarTexto(distrito);
        Provincia = NormalizarTexto(provincia);
        Departamento = NormalizarTexto(departamento);
        Activa = activa;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public string Nombre { get; private set; }

    public TipoSede Tipo { get; private set; }

    public string CodigoEstablecimiento { get; private set; }

    public string Direccion { get; private set; }

    public string Distrito { get; private set; }

    public string Provincia { get; private set; }

    public string Departamento { get; private set; }

    public bool Activa { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public void Desactivar()
    {
        Activa = false;
    }

    public void Activar()
    {
        Activa = true;
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
