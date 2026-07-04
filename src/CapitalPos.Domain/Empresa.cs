namespace CapitalPos.Domain;

public sealed class Empresa
{
    public Empresa(
        Guid id,
        string ruc,
        string razonSocial,
        string? nombreComercial = null,
        bool activa = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(id));
        }

        var rucNormalizado = NormalizarTexto(ruc);
        if (rucNormalizado.Length != 11 || !rucNormalizado.All(char.IsDigit))
        {
            throw new ArgumentException("El RUC debe tener 11 digitos.", nameof(ruc));
        }

        var razonSocialNormalizada = NormalizarTexto(razonSocial);
        if (string.IsNullOrWhiteSpace(razonSocialNormalizada))
        {
            throw new ArgumentException("La razon social es obligatoria.", nameof(razonSocial));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        Ruc = rucNormalizado;
        RazonSocial = razonSocialNormalizada;
        NombreComercial = NormalizarTexto(nombreComercial);
        Activa = activa;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public string Ruc { get; private set; }

    public string RazonSocial { get; private set; }

    public string NombreComercial { get; private set; }

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
