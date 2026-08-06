using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class PedidoDigitalTests
{
    [Fact]
    public void Crear_pedido_digital_valido_calcula_totales_y_estado_inicial()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var detalle = CrearDetalle(empresaId, id, cantidad: 2m, precioUnitario: 59m);

        var pedido = new PedidoDigital(
            id,
            empresaId,
            sedeId,
            CanalPedidoDigital.FACEBOOK_SUBASTA,
            DateTimeOffset.UtcNow,
            [detalle]);

        Assert.Equal(empresaId, pedido.EmpresaId);
        Assert.Equal(sedeId, pedido.SedeId);
        Assert.Null(pedido.ClienteId);
        Assert.Null(pedido.PuntoVentaId);
        Assert.Equal(CanalPedidoDigital.FACEBOOK_SUBASTA, pedido.CanalPedido);
        Assert.Equal(EstadoPedidoDigital.PendientePago, pedido.Estado);
        Assert.Equal(118m, pedido.Total);
        Assert.Equal(100m, pedido.Subtotal);
        Assert.Equal(18m, pedido.Igv);
        Assert.Single(pedido.Detalles);
    }

    [Fact]
    public void Rechaza_empresa_sede_y_canal_invalidos()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var sedeId = Guid.NewGuid();
        var detalle = CrearDetalle(empresaId, id);

        Assert.Throws<ArgumentException>(() =>
            new PedidoDigital(id, Guid.Empty, sedeId, CanalPedidoDigital.WHATSAPP, DateTimeOffset.UtcNow, [detalle]));
        Assert.Throws<ArgumentException>(() =>
            new PedidoDigital(id, empresaId, Guid.Empty, CanalPedidoDigital.WHATSAPP, DateTimeOffset.UtcNow, [detalle]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PedidoDigital(id, empresaId, sedeId, (CanalPedidoDigital)999, DateTimeOffset.UtcNow, [detalle]));
    }

    [Fact]
    public void Rechaza_detalles_vacios_o_de_otra_empresa()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new PedidoDigital(id, empresaId, Guid.NewGuid(), CanalPedidoDigital.WEB, DateTimeOffset.UtcNow, []));

        var detalleOtraEmpresa = CrearDetalle(Guid.NewGuid(), id);
        Assert.Throws<ArgumentException>(() =>
            new PedidoDigital(id, empresaId, Guid.NewGuid(), CanalPedidoDigital.WEB, DateTimeOffset.UtcNow, [detalleOtraEmpresa]));
    }

    [Fact]
    public void Detalle_exige_producto_cantidad_precio_y_guarda_presentacion_snapshot()
    {
        var empresaId = Guid.NewGuid();
        var pedidoId = Guid.NewGuid();
        var presentacionId = Guid.NewGuid();

        var detalle = new PedidoDigitalDetalle(
            Guid.NewGuid(),
            empresaId,
            pedidoId,
            Guid.NewGuid(),
            "Polo caja",
            2m,
            118m,
            productoPresentacionId: presentacionId,
            factorConversionAplicado: 12m);

        Assert.Equal(presentacionId, detalle.ProductoPresentacionId);
        Assert.Equal(12m, detalle.FactorConversionAplicado);
        Assert.Equal(24m, detalle.CantidadBase);
        Assert.Equal(236m, detalle.Total);

        Assert.Throws<ArgumentException>(() =>
            new PedidoDigitalDetalle(Guid.NewGuid(), empresaId, pedidoId, Guid.Empty, "Polo", 1m, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PedidoDigitalDetalle(Guid.NewGuid(), empresaId, pedidoId, Guid.NewGuid(), "Polo", 0m, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PedidoDigitalDetalle(Guid.NewGuid(), empresaId, pedidoId, Guid.NewGuid(), "Polo", 1m, 0m));
    }

    [Fact]
    public void Historial_estado_permite_estado_anterior_opcional_y_usuario_opcional()
    {
        var historial = new PedidoDigitalHistorialEstado(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            EstadoPedidoDigital.PendientePago,
            observacion: " Creado ");

        Assert.Null(historial.EstadoAnterior);
        Assert.Equal(EstadoPedidoDigital.PendientePago, historial.EstadoNuevo);
        Assert.Null(historial.UsuarioId);
        Assert.Equal("Creado", historial.Observacion);
    }

    [Fact]
    public void Cancelar_cambia_estado_y_registra_historial()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var pedido = new PedidoDigital(
            id,
            empresaId,
            Guid.NewGuid(),
            CanalPedidoDigital.WHATSAPP,
            DateTimeOffset.UtcNow,
            [CrearDetalle(empresaId, id)]);

        pedido.Cancelar(usuarioId, "Cliente desiste");

        Assert.Equal(EstadoPedidoDigital.Cancelado, pedido.Estado);
        var historial = Assert.Single(pedido.HistorialEstados);
        Assert.Equal(EstadoPedidoDigital.PendientePago, historial.EstadoAnterior);
        Assert.Equal(EstadoPedidoDigital.Cancelado, historial.EstadoNuevo);
        Assert.Equal(usuarioId, historial.UsuarioId);
        Assert.Equal("Cliente desiste", historial.Observacion);
    }

    [Fact]
    public void Cancelar_rechaza_pedido_ya_cancelado_o_entregado()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var pedido = new PedidoDigital(
            id,
            empresaId,
            Guid.NewGuid(),
            CanalPedidoDigital.WEB,
            DateTimeOffset.UtcNow,
            [CrearDetalle(empresaId, id)]);

        pedido.Cancelar();
        Assert.Throws<InvalidOperationException>(() => pedido.Cancelar());

        var entregadoId = Guid.NewGuid();
        var pedidoEntregado = new PedidoDigital(
            entregadoId,
            empresaId,
            Guid.NewGuid(),
            CanalPedidoDigital.WEB,
            DateTimeOffset.UtcNow,
            [CrearDetalle(empresaId, entregadoId)]);
        pedidoEntregado.CompletarPorConversionAVenta();
        Assert.Throws<InvalidOperationException>(() => pedidoEntregado.Cancelar());
    }

    [Fact]
    public void Completar_por_conversion_marca_entregado_y_rechaza_terminales()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var pedido = new PedidoDigital(
            id,
            empresaId,
            Guid.NewGuid(),
            CanalPedidoDigital.INSTAGRAM,
            DateTimeOffset.UtcNow,
            [CrearDetalle(empresaId, id)]);

        pedido.CompletarPorConversionAVenta(observacion: "Venta POS");

        Assert.Equal(EstadoPedidoDigital.Entregado, pedido.Estado);
        var historial = Assert.Single(pedido.HistorialEstados);
        Assert.Equal(EstadoPedidoDigital.Entregado, historial.EstadoNuevo);
        Assert.Equal("Venta POS", historial.Observacion);
        Assert.Throws<InvalidOperationException>(() => pedido.CompletarPorConversionAVenta());
    }

    private static PedidoDigitalDetalle CrearDetalle(
        Guid empresaId,
        Guid pedidoId,
        decimal cantidad = 1m,
        decimal precioUnitario = 10m)
    {
        return new PedidoDigitalDetalle(
            Guid.NewGuid(),
            empresaId,
            pedidoId,
            Guid.NewGuid(),
            "Polo",
            cantidad,
            precioUnitario);
    }
}
