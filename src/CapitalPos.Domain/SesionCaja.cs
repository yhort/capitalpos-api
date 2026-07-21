namespace CapitalPos.Domain;

public sealed class SesionCaja
{
    private SesionCaja()
    {
        ObservacionApertura = string.Empty;
        ObservacionCierre = string.Empty;
    }

    public SesionCaja(
        Guid id,
        Guid empresaId,
        Guid sedeId,
        Guid puntoVentaId,
        decimal montoInicial,
        Guid? usuarioAperturaId = null,
        string? observacionApertura = null,
        DateTimeOffset? fechaApertura = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sesion de caja es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (sedeId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la sede es obligatorio.", nameof(sedeId));
        }

        if (puntoVentaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del punto de venta es obligatorio.", nameof(puntoVentaId));
        }

        if (usuarioAperturaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario de apertura no puede estar vacio.", nameof(usuarioAperturaId));
        }

        if (montoInicial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(montoInicial), "El monto inicial no puede ser negativo.");
        }

        var fechaAperturaNormalizada = fechaApertura ?? DateTimeOffset.UtcNow;
        if (fechaAperturaNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaApertura), "La fecha de apertura no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        SedeId = sedeId;
        PuntoVentaId = puntoVentaId;
        UsuarioAperturaId = usuarioAperturaId;
        Estado = EstadoSesionCaja.Abierta;
        MontoInicial = montoInicial;
        FechaApertura = fechaAperturaNormalizada;
        ObservacionApertura = NormalizarTexto(observacionApertura);
        ObservacionCierre = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid SedeId { get; private set; }

    public Guid PuntoVentaId { get; private set; }

    public Guid? UsuarioAperturaId { get; private set; }

    public Guid? UsuarioCierreId { get; private set; }

    public EstadoSesionCaja Estado { get; private set; }

    public decimal MontoInicial { get; private set; }

    public decimal? MontoDeclaradoCierre { get; private set; }

    public decimal? DiferenciaCierre { get; private set; }

    public DateTimeOffset FechaApertura { get; private set; }

    public DateTimeOffset? FechaCierre { get; private set; }

    public string ObservacionApertura { get; private set; }

    public string ObservacionCierre { get; private set; }

    public void Cerrar(
        decimal montoDeclaradoCierre,
        DateTimeOffset fechaCierre,
        Guid? usuarioCierreId = null,
        string? observacionCierre = null)
    {
        if (Estado != EstadoSesionCaja.Abierta)
        {
            throw new InvalidOperationException("Solo se puede cerrar una sesion de caja abierta.");
        }

        if (usuarioCierreId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario de cierre no puede estar vacio.", nameof(usuarioCierreId));
        }

        if (montoDeclaradoCierre < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(montoDeclaradoCierre), "El monto declarado de cierre no puede ser negativo.");
        }

        if (fechaCierre < FechaApertura)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCierre), "La fecha de cierre no puede ser menor que la fecha de apertura.");
        }

        Estado = EstadoSesionCaja.Cerrada;
        UsuarioCierreId = usuarioCierreId;
        MontoDeclaradoCierre = montoDeclaradoCierre;
        DiferenciaCierre = montoDeclaradoCierre - MontoInicial;
        FechaCierre = fechaCierre;
        ObservacionCierre = NormalizarTexto(observacionCierre);
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
