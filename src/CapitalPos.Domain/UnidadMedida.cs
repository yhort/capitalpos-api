namespace CapitalPos.Domain;

public sealed class UnidadMedida
{
    private UnidadMedida()
    {
        Codigo = string.Empty;
        Nombre = string.Empty;
    }

    public UnidadMedida(
        Guid id,
        string codigo,
        string nombre,
        bool activa = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la unidad de medida es obligatorio.", nameof(id));
        }

        var codigoNormalizado = NormalizarCodigo(codigo);
        if (string.IsNullOrWhiteSpace(codigoNormalizado))
        {
            throw new ArgumentException("El codigo de la unidad de medida es obligatorio.", nameof(codigo));
        }

        var nombreNormalizado = NormalizarTexto(nombre);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ArgumentException("El nombre de la unidad de medida es obligatorio.", nameof(nombre));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        Codigo = codigoNormalizado;
        Nombre = nombreNormalizado;
        Activa = activa;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public string Codigo { get; private set; }

    public string Nombre { get; private set; }

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

    private static string NormalizarCodigo(string? valor)
    {
        return NormalizarTexto(valor).ToUpperInvariant();
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
