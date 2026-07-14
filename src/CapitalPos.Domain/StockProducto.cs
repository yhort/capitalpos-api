namespace CapitalPos.Domain;

public sealed class StockProducto
{
    private StockProducto()
    {
    }

    public StockProducto(
        Guid id,
        Guid empresaId,
        Guid productoId,
        Guid? productoVarianteId,
        decimal cantidadDisponible,
        decimal cantidadReservada = 0,
        DateTimeOffset? fechaCreacion = null,
        DateTimeOffset? fechaActualizacion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del stock es obligatorio.", nameof(id));
        }

        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la empresa es obligatorio.", nameof(empresaId));
        }

        if (productoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto es obligatorio.", nameof(productoId));
        }

        if (productoVarianteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la variante no puede estar vacio.", nameof(productoVarianteId));
        }

        ValidarCantidades(cantidadDisponible, cantidadReservada);

        var fechaCreacionNormalizada = fechaCreacion ?? DateTimeOffset.UtcNow;
        if (fechaCreacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "La fecha de creacion no es valida.");
        }

        var fechaActualizacionNormalizada = fechaActualizacion ?? fechaCreacionNormalizada;
        if (fechaActualizacionNormalizada == default)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaActualizacion), "La fecha de actualizacion no es valida.");
        }

        Id = id;
        EmpresaId = empresaId;
        ProductoId = productoId;
        ProductoVarianteId = productoVarianteId;
        CantidadDisponible = cantidadDisponible;
        CantidadReservada = cantidadReservada;
        FechaCreacion = fechaCreacionNormalizada;
        FechaActualizacion = fechaActualizacionNormalizada;
    }

    public Guid Id { get; private set; }

    public Guid EmpresaId { get; private set; }

    public Guid ProductoId { get; private set; }

    public Guid? ProductoVarianteId { get; private set; }

    public decimal CantidadDisponible { get; private set; }

    public decimal CantidadReservada { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public DateTimeOffset FechaActualizacion { get; private set; }

    public decimal CantidadLibre => CantidadDisponible - CantidadReservada;

    public void AjustarCantidadDisponible(decimal cantidadDisponible)
    {
        if (cantidadDisponible < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadDisponible), "La cantidad disponible no puede ser negativa.");
        }

        if (CantidadReservada > cantidadDisponible)
        {
            throw new InvalidOperationException("La cantidad reservada no puede superar la cantidad disponible.");
        }

        CantidadDisponible = cantidadDisponible;
        MarcarActualizado();
    }

    public void Incrementar(decimal cantidad)
    {
        ValidarCantidadPositiva(cantidad, nameof(cantidad));

        CantidadDisponible += cantidad;
        MarcarActualizado();
    }

    public void Descontar(decimal cantidad)
    {
        ValidarCantidadPositiva(cantidad, nameof(cantidad));

        if (cantidad > CantidadLibre)
        {
            throw new InvalidOperationException("No hay stock disponible suficiente.");
        }

        CantidadDisponible -= cantidad;
        MarcarActualizado();
    }

    public void Reservar(decimal cantidad)
    {
        ValidarCantidadPositiva(cantidad, nameof(cantidad));

        if (cantidad > CantidadLibre)
        {
            throw new InvalidOperationException("No hay stock disponible suficiente para reservar.");
        }

        CantidadReservada += cantidad;
        MarcarActualizado();
    }

    public void LiberarReserva(decimal cantidad)
    {
        ValidarCantidadPositiva(cantidad, nameof(cantidad));

        if (cantidad > CantidadReservada)
        {
            throw new InvalidOperationException("No se puede liberar una reserva mayor a la cantidad reservada.");
        }

        CantidadReservada -= cantidad;
        MarcarActualizado();
    }

    private static void ValidarCantidades(decimal cantidadDisponible, decimal cantidadReservada)
    {
        if (cantidadDisponible < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadDisponible), "La cantidad disponible no puede ser negativa.");
        }

        if (cantidadReservada < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadReservada), "La cantidad reservada no puede ser negativa.");
        }

        if (cantidadReservada > cantidadDisponible)
        {
            throw new ArgumentException("La cantidad reservada no puede superar la cantidad disponible.", nameof(cantidadReservada));
        }
    }

    private static void ValidarCantidadPositiva(decimal cantidad, string parametro)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentOutOfRangeException(parametro, "La cantidad debe ser mayor que cero.");
        }
    }

    private void MarcarActualizado()
    {
        FechaActualizacion = DateTimeOffset.UtcNow;
    }
}
