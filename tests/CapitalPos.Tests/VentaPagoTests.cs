using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class VentaPagoTests
{
    [Theory]
    [InlineData(MetodoPago.EFECTIVO)]
    [InlineData(MetodoPago.YAPE)]
    [InlineData(MetodoPago.TARJETA)]
    [InlineData(MetodoPago.TRANSFERENCIA)]
    [InlineData(MetodoPago.OTRO)]
    public void Crea_pago_con_metodo_valido(MetodoPago metodoPago)
    {
        var pago = new VentaPago(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            metodoPago,
            25m,
            "  OP-001  ",
            "  Pago manual  ");

        Assert.Equal(metodoPago, pago.MetodoPago);
        Assert.Equal(25m, pago.Monto);
        Assert.Equal("OP-001", pago.CodigoOperacion);
        Assert.Equal("Pago manual", pago.Observacion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_monto_no_positivo(decimal monto)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new VentaPago(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                MetodoPago.EFECTIVO,
                monto));

        Assert.Contains("mayor que cero", exception.Message);
    }

    [Fact]
    public void Venta_rechaza_suma_de_pagos_distinta_al_total()
    {
        var empresaId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            Guid.NewGuid(),
            1m,
            20m,
            0m,
            20m);
        var pago = new VentaPago(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            MetodoPago.EFECTIVO,
            19m);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Venta(
                ventaId,
                empresaId,
                DateTimeOffset.UtcNow,
                20m,
                0m,
                20m,
                [detalle],
                Guid.NewGuid(),
                Guid.NewGuid(),
                pagos: [pago]));

        Assert.Contains("suma de los pagos", exception.Message);
    }
}
