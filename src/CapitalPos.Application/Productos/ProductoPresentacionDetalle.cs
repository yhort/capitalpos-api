using CapitalPos.Domain;

namespace CapitalPos.Application.Productos;

public sealed record ProductoPresentacionDetalle(
    ProductoPresentacion Presentacion,
    UnidadMedida UnidadMedida);
