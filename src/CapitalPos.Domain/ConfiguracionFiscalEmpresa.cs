namespace CapitalPos.Domain;

public sealed class ConfiguracionFiscalEmpresa
{
    private ConfiguracionFiscalEmpresa()
    {
        Ruc = string.Empty;
        RazonSocial = string.Empty;
        NombreComercial = string.Empty;
        Ubigeo = string.Empty;
        Direccion = string.Empty;
        Departamento = string.Empty;
        Provincia = string.Empty;
        Distrito = string.Empty;
    }

    public ConfiguracionFiscalEmpresa(
        Guid empresaId,
        string ruc,
        string razonSocial,
        string? nombreComercial,
        string ubigeo,
        string direccion,
        string departamento,
        string provincia,
        string distrito,
        bool activa = true,
        DateTimeOffset? fechaCreacion = null)
    {
        ValidarEmpresaId(empresaId);

        EmpresaId = empresaId;
        FechaCreacion = NormalizarFecha(fechaCreacion);
        Activa = activa;
        ActualizarDatosFiscales(
            ruc,
            razonSocial,
            nombreComercial,
            ubigeo,
            direccion,
            departamento,
            provincia,
            distrito);
    }

    public Guid EmpresaId { get; private set; }

    public string Ruc { get; private set; } = string.Empty;

    public string RazonSocial { get; private set; } = string.Empty;

    public string NombreComercial { get; private set; } = string.Empty;

    public string Ubigeo { get; private set; } = string.Empty;

    public string Direccion { get; private set; } = string.Empty;

    public string Departamento { get; private set; } = string.Empty;

    public string Provincia { get; private set; } = string.Empty;

    public string Distrito { get; private set; } = string.Empty;

    public bool Activa { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public void ActualizarDatosFiscales(
        string ruc,
        string razonSocial,
        string? nombreComercial,
        string ubigeo,
        string direccion,
        string departamento,
        string provincia,
        string distrito)
    {
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

        var ubigeoNormalizado = NormalizarTexto(ubigeo);
        if (ubigeoNormalizado.Length != 6 || !ubigeoNormalizado.All(char.IsDigit))
        {
            throw new ArgumentException("El ubigeo debe tener 6 digitos.", nameof(ubigeo));
        }

        var direccionNormalizada = ValidarTextoObligatorio(direccion, nameof(direccion), "La direccion es obligatoria.");
        var departamentoNormalizado = ValidarTextoObligatorio(departamento, nameof(departamento), "El departamento es obligatorio.");
        var provinciaNormalizada = ValidarTextoObligatorio(provincia, nameof(provincia), "La provincia es obligatoria.");
        var distritoNormalizado = ValidarTextoObligatorio(distrito, nameof(distrito), "El distrito es obligatorio.");

        Ruc = rucNormalizado;
        RazonSocial = razonSocialNormalizada;
        NombreComercial = NormalizarTexto(nombreComercial);
        Ubigeo = ubigeoNormalizado;
        Direccion = direccionNormalizada;
        Departamento = departamentoNormalizado;
        Provincia = provinciaNormalizada;
        Distrito = distritoNormalizado;
    }

    public void Desactivar()
    {
        Activa = false;
    }

    public void Activar()
    {
        Activa = true;
    }

    private static void ValidarEmpresaId(Guid empresaId)
    {
        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }
    }

    private static DateTimeOffset NormalizarFecha(DateTimeOffset? fechaCreacion)
    {
        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        return fechaCreacionNormalizada;
    }

    private static string ValidarTextoObligatorio(
        string valor,
        string parametro,
        string mensaje)
    {
        var normalizado = NormalizarTexto(valor);
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new ArgumentException(mensaje, parametro);
        }

        return normalizado;
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
