namespace CapitalPos.Domain;

public sealed class MovimientoInventario
{
    private MovimientoInventario() { }
    public MovimientoInventario(Guid id, Guid empresaId, Guid sedeId, Guid productoId, Guid? productoVarianteId, TipoMovimientoInventario tipoMovimiento, decimal cantidad, decimal stockAnterior, decimal stockPosterior, string? referenciaTipo = null, Guid? referenciaId = null, string? motivo = null, DateTimeOffset? fechaCreacion = null, Guid? usuarioId = null)
    {
        if (id == Guid.Empty || empresaId == Guid.Empty || sedeId == Guid.Empty || productoId == Guid.Empty) throw new ArgumentException("Los identificadores del movimiento son obligatorios.");
        if (cantidad <= 0) throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
        Id=id; EmpresaId=empresaId; SedeId=sedeId; ProductoId=productoId; ProductoVarianteId=productoVarianteId; TipoMovimiento=tipoMovimiento; Cantidad=cantidad; StockAnterior=stockAnterior; StockPosterior=stockPosterior; ReferenciaTipo=referenciaTipo?.Trim(); ReferenciaId=referenciaId; Motivo=motivo?.Trim(); FechaCreacion=fechaCreacion ?? DateTimeOffset.UtcNow; UsuarioId=usuarioId;
    }
    public Guid Id { get; private set; } public Guid EmpresaId { get; private set; } public Guid SedeId { get; private set; } public Guid ProductoId { get; private set; } public Guid? ProductoVarianteId { get; private set; } public TipoMovimientoInventario TipoMovimiento { get; private set; } public decimal Cantidad { get; private set; } public decimal StockAnterior { get; private set; } public decimal StockPosterior { get; private set; } public string? ReferenciaTipo { get; private set; } public Guid? ReferenciaId { get; private set; } public string? Motivo { get; private set; } public DateTimeOffset FechaCreacion { get; private set; } public Guid? UsuarioId { get; private set; }
}
