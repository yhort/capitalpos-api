namespace CapitalPos.Domain;

public sealed class Cliente
{
    private Cliente()
    {
        TipoDocumento = string.Empty;
        NumeroDocumento = string.Empty;
        NombreRazonSocial = string.Empty;
        Direccion = string.Empty;
    }

    public Cliente(
        Guid id,
        Guid empresaId,
        string tipoDocumento,
        string? numeroDocumento,
        string nombreRazonSocial,
        string? direccion = null,
        bool activo = true,
        DateTimeOffset? fechaCreacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del cliente es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        var tipoDocumentoNormalizado = NormalizarTexto(tipoDocumento).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(tipoDocumentoNormalizado))
        {
            throw new ArgumentException("El tipo de documento del cliente es obligatorio.", nameof(tipoDocumento));
        }

        var nombreNormalizado = NormalizarTexto(nombreRazonSocial);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ArgumentException("El nombre o razon social del cliente es obligatorio.", nameof(nombreRazonSocial));
        }

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        TipoDocumento = tipoDocumentoNormalizado;
        NumeroDocumento = NormalizarTexto(numeroDocumento);
        NombreRazonSocial = nombreNormalizado;
        Direccion = NormalizarTexto(direccion);
        Activo = activo;
        FechaCreacion = fechaCreacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public string TipoDocumento { get; private set; }

    public string NumeroDocumento { get; private set; }

    public string NombreRazonSocial { get; private set; }

    public string Direccion { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public void ActualizarDatosBasicos(
        string tipoDocumento,
        string? numeroDocumento,
        string nombreRazonSocial,
        string? direccion = null)
    {
        var tipoDocumentoNormalizado = NormalizarTexto(tipoDocumento).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(tipoDocumentoNormalizado))
        {
            throw new ArgumentException("El tipo de documento del cliente es obligatorio.", nameof(tipoDocumento));
        }

        var nombreNormalizado = NormalizarTexto(nombreRazonSocial);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ArgumentException("El nombre o razon social del cliente es obligatorio.", nameof(nombreRazonSocial));
        }

        TipoDocumento = tipoDocumentoNormalizado;
        NumeroDocumento = NormalizarTexto(numeroDocumento);
        NombreRazonSocial = nombreNormalizado;
        Direccion = NormalizarTexto(direccion);
    }

    public void Desactivar()
    {
        Activo = false;
    }

    public void Activar()
    {
        Activo = true;
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
