using System.Text.Json;
using CapitalPos.Application.Cpe;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationVentaTests
{
    [Fact]
    public async Task Crear_venta_use_case_asigna_empresa_id_desde_contexto_y_calcula_totales()
    {
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        var clienteRepository = new ClienteRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await clienteRepository.AgregarAsync(new Cliente(clienteId, empresaId, "DNI", "12345678", "Juan Perez"));
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await varianteRepository.AgregarAsync(new ProductoVariante(varianteId, empresaId, productoId, talla: "M"));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoId, varianteId, 10m));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoId, null, 10m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            varianteRepository,
            clienteRepository,
            stockRepository,
            empresaId: empresaId);
        var fecha = DateTimeOffset.UtcNow;
        var request = new CrearVentaRequest(
            fecha,
            clienteId,
            [
                new CrearVentaDetalleRequest(productoId, varianteId, 2m, 50m, 18m, 118m),
                new CrearVentaDetalleRequest(productoId, null, 1m, 20m, 3.60m, 23.60m)
            ]);

        var venta = await useCase.EjecutarAsync(request);

        Assert.Equal(empresaId, venta.EmpresaId);
        Assert.Equal(clienteId, venta.ClienteId);
        Assert.Equal(fecha, venta.Fecha);
        Assert.Equal(120m, venta.Subtotal);
        Assert.Equal(21.60m, venta.Igv);
        Assert.Equal(141.60m, venta.Total);
        Assert.Equal(2, venta.Detalles.Count);
        Assert.All(venta.Detalles, detalle =>
        {
            Assert.Equal(empresaId, detalle.EmpresaId);
            Assert.Equal(venta.Id, detalle.VentaId);
        });
        Assert.Same(venta, ventaRepository.Ventas.Single());
        Assert.Equal(8m, stockRepository.Stocks.Single(stock => stock.ProductoVarianteId == varianteId).CantidadDisponible);
        Assert.Equal(9m, stockRepository.Stocks.Single(stock => stock.ProductoVarianteId is null).CantidadDisponible);
    }

    [Fact]
    public async Task Crear_venta_use_case_falla_si_no_hay_empresa_activa()
    {
        var useCase = CrearUseCase(
            new VentaRepositoryFake(),
            new ProductoRepositoryFake(),
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake());
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(Guid.NewGuid(), null, 1m, 10m, 0m, 10m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task Crear_venta_use_case_rechaza_producto_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaBId, "Polo", 59m));
        var useCase = CrearUseCase(
            new VentaRepositoryFake(),
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            empresaId: empresaAId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(productoId, null, 1m, 10m, 0m, 10m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task Crear_venta_use_case_rechaza_cliente_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var clienteRepository = new ClienteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        await clienteRepository.AgregarAsync(new Cliente(clienteId, empresaBId, "DNI", "12345678", "Juan Perez"));
        await productoRepository.AgregarAsync(new Producto(productoId, empresaAId, "Polo", 59m));
        var useCase = CrearUseCase(
            new VentaRepositoryFake(),
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            clienteRepository,
            empresaId: empresaAId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            clienteId,
            [new CrearVentaDetalleRequest(productoId, null, 1m, 10m, 0m, 10m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task Crear_venta_use_case_rechaza_variante_de_otro_producto()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var otroProductoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await varianteRepository.AgregarAsync(new ProductoVariante(varianteId, empresaId, otroProductoId, talla: "M"));
        var useCase = CrearUseCase(
            new VentaRepositoryFake(),
            productoRepository,
            varianteRepository,
            new ClienteRepositoryFake(),
            empresaId: empresaId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(productoId, varianteId, 1m, 10m, 0m, 10m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task Crear_venta_descuenta_stock_de_producto()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoId, null, 5m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);

        await useCase.EjecutarAsync(CrearVentaRequest(productoId, null, 2m));

        Assert.Single(ventaRepository.Ventas);
        Assert.Equal(3m, stockRepository.Stocks.Single().CantidadDisponible);
    }

    [Fact]
    public async Task Crear_venta_falla_si_producto_no_tiene_stock()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(CrearVentaRequest(productoId, null, 1m)));

        Assert.Contains("stock", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(ventaRepository.Ventas);
        Assert.Empty(stockRepository.Stocks);
    }

    [Fact]
    public async Task Crear_venta_falla_si_stock_libre_es_insuficiente()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await stockRepository.GuardarAsync(new StockProducto(
            Guid.NewGuid(),
            empresaId,
            productoId,
            null,
            5m,
            2m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(CrearVentaRequest(productoId, null, 4m)));

        Assert.Contains("Stock insuficiente", exception.Message);
        Assert.Empty(ventaRepository.Ventas);
        Assert.Equal(5m, stockRepository.Stocks.Single().CantidadDisponible);
        Assert.Equal(2m, stockRepository.Stocks.Single().CantidadReservada);
    }

    [Fact]
    public async Task Crear_venta_con_variante_descuenta_stock_de_variante()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var varianteId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await varianteRepository.AgregarAsync(new ProductoVariante(varianteId, empresaId, productoId, talla: "M"));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoId, varianteId, 7m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            varianteRepository,
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);

        await useCase.EjecutarAsync(CrearVentaRequest(productoId, varianteId, 3m));

        Assert.Single(ventaRepository.Ventas);
        Assert.Equal(4m, stockRepository.Stocks.Single().CantidadDisponible);
    }

    [Fact]
    public async Task Crear_venta_multidetalle_descuenta_todos_los_stocks()
    {
        var empresaId = Guid.NewGuid();
        var productoAId = Guid.NewGuid();
        var productoBId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoAId, empresaId, "Polo", 59m));
        await productoRepository.AgregarAsync(new Producto(productoBId, empresaId, "Gorra", 25m));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoAId, null, 8m));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoBId, null, 6m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [
                new CrearVentaDetalleRequest(productoAId, null, 2m, 10m, 0m, 20m),
                new CrearVentaDetalleRequest(productoBId, null, 3m, 10m, 0m, 30m)
            ]);

        await useCase.EjecutarAsync(request);

        Assert.Single(ventaRepository.Ventas);
        Assert.Equal(6m, stockRepository.Stocks.Single(stock => stock.ProductoId == productoAId).CantidadDisponible);
        Assert.Equal(3m, stockRepository.Stocks.Single(stock => stock.ProductoId == productoBId).CantidadDisponible);
    }

    [Fact]
    public async Task Crear_venta_multidetalle_con_un_detalle_sin_stock_no_descuenta_ninguno()
    {
        var empresaId = Guid.NewGuid();
        var productoAId = Guid.NewGuid();
        var productoBId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoAId, empresaId, "Polo", 59m));
        await productoRepository.AgregarAsync(new Producto(productoBId, empresaId, "Gorra", 25m));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoAId, null, 8m));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoBId, null, 2m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);
        var request = new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [
                new CrearVentaDetalleRequest(productoAId, null, 2m, 10m, 0m, 20m),
                new CrearVentaDetalleRequest(productoBId, null, 3m, 10m, 0m, 30m)
            ]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));

        Assert.Empty(ventaRepository.Ventas);
        Assert.Equal(8m, stockRepository.Stocks.Single(stock => stock.ProductoId == productoAId).CantidadDisponible);
        Assert.Equal(2m, stockRepository.Stocks.Single(stock => stock.ProductoId == productoBId).CantidadDisponible);
    }

    [Fact]
    public async Task Crear_venta_si_falla_persistencia_no_deja_stock_descontado()
    {
        var empresaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake
        {
            LanzarExcepcionAlAgregar = true
        };
        var productoRepository = new ProductoRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Polo", 59m));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaId, productoId, null, 5m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(CrearVentaRequest(productoId, null, 2m)));

        Assert.Empty(ventaRepository.Ventas);
        Assert.Equal(5m, stockRepository.Stocks.Single().CantidadDisponible);
    }

    [Fact]
    public async Task Crear_venta_no_descuenta_stock_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var stockRepository = new StockProductoRepositoryFake();
        await productoRepository.AgregarAsync(new Producto(productoId, empresaAId, "Polo", 59m));
        await stockRepository.GuardarAsync(new StockProducto(Guid.NewGuid(), empresaBId, productoId, null, 10m));
        var useCase = CrearUseCase(
            ventaRepository,
            productoRepository,
            new ProductoVarianteRepositoryFake(),
            new ClienteRepositoryFake(),
            stockRepository,
            empresaAId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(CrearVentaRequest(productoId, null, 1m)));

        Assert.Empty(ventaRepository.Ventas);
        Assert.Equal(10m, stockRepository.Stocks.Single().CantidadDisponible);
    }

    [Fact]
    public async Task Emitir_cpe_desde_venta_use_case_construye_payload_desde_venta_activa()
    {
        var empresaId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var configuracionFiscalRepository = new ConfiguracionFiscalEmpresaRepositoryFake();
        var clienteRepository = new ClienteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        var gateway = new CpeGatewayFake();
        await configuracionFiscalRepository.GuardarAsync(new ConfiguracionFiscalEmpresa(
            empresaId,
            "20601234567",
            "CapitalPOS Fiscal SAC",
            "CapitalPOS Fiscal",
            "150102",
            "Calle Fiscal 456",
            "LIMA",
            "LIMA",
            "ANCON"));
        await clienteRepository.AgregarAsync(new Cliente(clienteId, empresaId, "DNI", "12345678", "Cliente Demo"));
        await productoRepository.AgregarAsync(new Producto(productoId, empresaId, "Producto gravado", 59m, "SKU-001"));
        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            productoId,
            2m,
            50m,
            18m,
            118m);
        var venta = new Venta(
            ventaId,
            empresaId,
            new DateTimeOffset(2026, 7, 11, 5, 0, 0, TimeSpan.Zero),
            100m,
            18m,
            118m,
            [detalle],
            clienteId);
        await ventaRepository.AgregarAsync(venta);
        var useCase = new EmitirCpeDesdeVentaUseCase(
            ventaRepository,
            configuracionFiscalRepository,
            clienteRepository,
            productoRepository,
            varianteRepository,
            gateway,
            new EmpresaActivaContextFake(empresaId));
        var request = new EmitirCpeDesdeVentaRequest("03", "B001", 7, "20601234567");

        var response = await useCase.EjecutarAsync(ventaId, request);

        Assert.NotNull(response);
        Assert.NotNull(gateway.UltimoRequest);
        Assert.Equal("03", gateway.UltimoRequest.Value.GetProperty("tipoComprobante").GetString());
        Assert.Equal("B001", gateway.UltimoRequest.Value.GetProperty("serie").GetString());
        Assert.Equal(7, gateway.UltimoRequest.Value.GetProperty("correlativo").GetInt32());
        Assert.Equal("20601234567", gateway.UltimoRequest.Value.GetProperty("rucEmisor").GetString());
        Assert.Equal("PEN", gateway.UltimoRequest.Value.GetProperty("moneda").GetString());
        Assert.Equal("0101", gateway.UltimoRequest.Value.GetProperty("tipoOperacion").GetString());
        Assert.Equal("CONTADO", gateway.UltimoRequest.Value.GetProperty("formaPago").GetString());
        Assert.Equal(ventaId, gateway.UltimoRequest.Value.GetProperty("ventaId").GetGuid());
        Assert.Equal(empresaId, gateway.UltimoRequest.Value.GetProperty("empresaId").GetGuid());
        Assert.Equal(100m, gateway.UltimoRequest.Value.GetProperty("totalGravada").GetDecimal());
        Assert.Equal(0m, gateway.UltimoRequest.Value.GetProperty("totalExonerada").GetDecimal());
        Assert.Equal(0m, gateway.UltimoRequest.Value.GetProperty("totalInafecta").GetDecimal());
        Assert.Equal(18m, gateway.UltimoRequest.Value.GetProperty("totalIgv").GetDecimal());
        Assert.Equal(118m, gateway.UltimoRequest.Value.GetProperty("total").GetDecimal());
        Assert.Equal(
            "2026-07-11T00:00:00",
            gateway.UltimoRequest.Value.GetProperty("fechaEmision").GetString());

        var emisor = gateway.UltimoRequest.Value.GetProperty("emisor");
        Assert.Equal("20601234567", emisor.GetProperty("ruc").GetString());
        Assert.Equal("CapitalPOS Fiscal SAC", emisor.GetProperty("razonSocial").GetString());
        Assert.Equal("CapitalPOS Fiscal", emisor.GetProperty("nombreComercial").GetString());
        Assert.Equal("150102", emisor.GetProperty("ubigeo").GetString());
        Assert.Equal("Calle Fiscal 456", emisor.GetProperty("direccion").GetString());
        Assert.Equal("LIMA", emisor.GetProperty("departamento").GetString());
        Assert.Equal("LIMA", emisor.GetProperty("provincia").GetString());
        Assert.Equal("ANCON", emisor.GetProperty("distrito").GetString());
        Assert.DoesNotContain("AV. DEMO 123", gateway.UltimoRequest.Value.GetRawText());
        Assert.DoesNotContain("150101", gateway.UltimoRequest.Value.GetRawText());

        var cliente = gateway.UltimoRequest.Value.GetProperty("cliente");
        Assert.Equal("1", cliente.GetProperty("tipoDocumento").GetString());
        Assert.Equal("12345678", cliente.GetProperty("numeroDocumento").GetString());
        Assert.Equal("Cliente Demo", cliente.GetProperty("razonSocial").GetString());

        var item = Assert.Single(gateway.UltimoRequest.Value.GetProperty("items").EnumerateArray());
        Assert.Equal("SKU-001", item.GetProperty("codigo").GetString());
        Assert.Equal("Producto gravado", item.GetProperty("descripcion").GetString());
        Assert.Equal("NIU", item.GetProperty("unidadMedida").GetString());
        Assert.Equal(2m, item.GetProperty("cantidad").GetDecimal());
        Assert.Equal(50m, item.GetProperty("valorUnitario").GetDecimal());
        Assert.Equal(59m, item.GetProperty("precioUnitario").GetDecimal());
        Assert.Equal(100m, item.GetProperty("subtotal").GetDecimal());
        Assert.Equal(18m, item.GetProperty("igv").GetDecimal());
        Assert.Equal(118m, item.GetProperty("total").GetDecimal());
        Assert.Equal("10", item.GetProperty("codigoAfectacionIgv").GetString());
    }

    [Fact]
    public async Task Emitir_cpe_desde_venta_use_case_convierte_fecha_emision_a_zona_lima()
    {
        var escenario = await CrearEscenarioEmisionAsync(
            new DateTimeOffset(2026, 7, 13, 1, 30, 0, TimeSpan.Zero));
        var configuracionFiscalRepository = new ConfiguracionFiscalEmpresaRepositoryFake();
        await GuardarConfiguracionFiscalAsync(configuracionFiscalRepository, escenario.EmpresaId);
        var useCase = CrearUseCaseEmision(
            escenario,
            configuracionFiscalRepository);

        await useCase.EjecutarAsync(
            escenario.VentaId,
            new EmitirCpeDesdeVentaRequest("03", "B001", 7, "20601234567"));

        Assert.NotNull(escenario.Gateway.UltimoRequest);
        Assert.Equal(
            "2026-07-12T20:30:00",
            escenario.Gateway.UltimoRequest.Value.GetProperty("fechaEmision").GetString());
    }

    [Fact]
    public async Task Emitir_cpe_desde_venta_use_case_no_envia_fecha_futura_cuando_utc_ya_es_dia_siguiente()
    {
        var escenario = await CrearEscenarioEmisionAsync(
            new DateTimeOffset(2026, 7, 13, 4, 59, 0, TimeSpan.Zero));
        var configuracionFiscalRepository = new ConfiguracionFiscalEmpresaRepositoryFake();
        await GuardarConfiguracionFiscalAsync(configuracionFiscalRepository, escenario.EmpresaId);
        var useCase = CrearUseCaseEmision(
            escenario,
            configuracionFiscalRepository);

        await useCase.EjecutarAsync(
            escenario.VentaId,
            new EmitirCpeDesdeVentaRequest("03", "B001", 7, "20601234567"));

        Assert.NotNull(escenario.Gateway.UltimoRequest);
        var fechaEmision = escenario.Gateway.UltimoRequest.Value
            .GetProperty("fechaEmision")
            .GetDateTime();

        Assert.Equal(new DateTime(2026, 7, 12), fechaEmision.Date);
        Assert.Equal(new TimeSpan(23, 59, 0), fechaEmision.TimeOfDay);
    }

    [Fact]
    public async Task Emitir_cpe_desde_venta_use_case_falla_si_no_existe_configuracion_fiscal()
    {
        var escenario = await CrearEscenarioEmisionAsync();
        var useCase = new EmitirCpeDesdeVentaUseCase(
            escenario.VentaRepository,
            new ConfiguracionFiscalEmpresaRepositoryFake(),
            escenario.ClienteRepository,
            escenario.ProductoRepository,
            escenario.VarianteRepository,
            escenario.Gateway,
            new EmpresaActivaContextFake(escenario.EmpresaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(
                escenario.VentaId,
                new EmitirCpeDesdeVentaRequest("03", "B001", 7, "20601234567")));

        Assert.Contains("no tiene configuracion fiscal", exception.Message);
        Assert.Null(escenario.Gateway.UltimoRequest);
    }

    [Fact]
    public async Task Emitir_cpe_desde_venta_use_case_falla_si_configuracion_fiscal_esta_inactiva()
    {
        var escenario = await CrearEscenarioEmisionAsync();
        var configuracionFiscalRepository = new ConfiguracionFiscalEmpresaRepositoryFake();
        await configuracionFiscalRepository.GuardarAsync(new ConfiguracionFiscalEmpresa(
            escenario.EmpresaId,
            "20601234567",
            "CapitalPOS Fiscal SAC",
            "CapitalPOS Fiscal",
            "150102",
            "Calle Fiscal 456",
            "LIMA",
            "LIMA",
            "ANCON",
            activa: false));
        var useCase = new EmitirCpeDesdeVentaUseCase(
            escenario.VentaRepository,
            configuracionFiscalRepository,
            escenario.ClienteRepository,
            escenario.ProductoRepository,
            escenario.VarianteRepository,
            escenario.Gateway,
            new EmpresaActivaContextFake(escenario.EmpresaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(
                escenario.VentaId,
                new EmitirCpeDesdeVentaRequest("03", "B001", 7, "20601234567")));

        Assert.Contains("esta inactiva", exception.Message);
        Assert.Null(escenario.Gateway.UltimoRequest);
    }

    [Fact]
    public async Task Emitir_cpe_desde_venta_use_case_falla_si_ruc_emisor_no_coincide_con_configuracion_fiscal()
    {
        var escenario = await CrearEscenarioEmisionAsync();
        var configuracionFiscalRepository = new ConfiguracionFiscalEmpresaRepositoryFake();
        await configuracionFiscalRepository.GuardarAsync(new ConfiguracionFiscalEmpresa(
            escenario.EmpresaId,
            "20601234567",
            "CapitalPOS Fiscal SAC",
            "CapitalPOS Fiscal",
            "150102",
            "Calle Fiscal 456",
            "LIMA",
            "LIMA",
            "ANCON"));
        var useCase = new EmitirCpeDesdeVentaUseCase(
            escenario.VentaRepository,
            configuracionFiscalRepository,
            escenario.ClienteRepository,
            escenario.ProductoRepository,
            escenario.VarianteRepository,
            escenario.Gateway,
            new EmpresaActivaContextFake(escenario.EmpresaId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.EjecutarAsync(
                escenario.VentaId,
                new EmitirCpeDesdeVentaRequest("03", "B001", 7, "20600000001")));

        Assert.Contains("no coincide", exception.Message);
        Assert.Null(escenario.Gateway.UltimoRequest);
    }

    [Fact]
    public async Task Emitir_cpe_desde_venta_use_case_no_llama_gateway_si_venta_es_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var configuracionFiscalRepository = new ConfiguracionFiscalEmpresaRepositoryFake();
        var clienteRepository = new ClienteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        var gateway = new CpeGatewayFake();
        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            empresaBId,
            ventaId,
            Guid.NewGuid(),
            1m,
            10m,
            0m,
            10m);
        await ventaRepository.AgregarAsync(new Venta(
            ventaId,
            empresaBId,
            DateTimeOffset.UtcNow,
            10m,
            0m,
            10m,
            [detalle]));
        await configuracionFiscalRepository.GuardarAsync(new ConfiguracionFiscalEmpresa(
            empresaAId,
            "20601234567",
            "CapitalPOS Fiscal SAC",
            "CapitalPOS Fiscal",
            "150102",
            "Calle Fiscal 456",
            "LIMA",
            "LIMA",
            "ANCON"));
        var useCase = new EmitirCpeDesdeVentaUseCase(
            ventaRepository,
            configuracionFiscalRepository,
            clienteRepository,
            productoRepository,
            varianteRepository,
            gateway,
            new EmpresaActivaContextFake(empresaAId));

        var response = await useCase.EjecutarAsync(
            ventaId,
            new EmitirCpeDesdeVentaRequest("03", "B001", 7, "20601234567"));

        Assert.Null(response);
        Assert.Null(gateway.UltimoRequest);
    }

    [Fact]
    public async Task Registrar_comprobante_cpe_use_case_guarda_resultado_para_venta_de_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var comprobanteRepository = new ComprobanteRepositoryFake();
        await ventaRepository.AgregarAsync(CrearVenta(ventaId, empresaId));
        var useCase = new RegistrarComprobanteCpeUseCase(
            comprobanteRepository,
            ventaRepository,
            new EmpresaActivaContextFake(empresaId));
        var request = new RegistrarComprobanteCpeRequest(
            ventaId,
            "03",
            "B001",
            7,
            "SIMULADO",
            "Aceptado en simulacion",
            "hash",
            "xml.xml",
            "zip.zip",
            "cdr.zip");

        var comprobante = await useCase.EjecutarAsync(request);

        Assert.NotNull(comprobante);
        Assert.Equal(empresaId, comprobante.EmpresaId);
        Assert.Equal(ventaId, comprobante.VentaId);
        Assert.Equal("SIMULADO", comprobante.EstadoCpe);
        Assert.Equal("hash", comprobante.Hash);
        Assert.Same(comprobante, comprobanteRepository.Comprobantes.Single());
    }

    [Fact]
    public async Task Registrar_comprobante_cpe_use_case_guarda_resultado_fallido_para_venta_de_empresa_activa()
    {
        var empresaId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var comprobanteRepository = new ComprobanteRepositoryFake();
        await ventaRepository.AgregarAsync(CrearVenta(ventaId, empresaId));
        var useCase = new RegistrarComprobanteCpeUseCase(
            comprobanteRepository,
            ventaRepository,
            new EmpresaActivaContextFake(empresaId));
        var request = new RegistrarComprobanteCpeRequest(
            ventaId,
            "03",
            "B001",
            8,
            "ERROR_VALIDACION",
            "No se puede emitir el comprobante porque tiene errores de validacion.");

        var comprobante = await useCase.EjecutarAsync(request);

        Assert.NotNull(comprobante);
        Assert.Equal(empresaId, comprobante.EmpresaId);
        Assert.Equal(ventaId, comprobante.VentaId);
        Assert.Equal("ERROR_VALIDACION", comprobante.EstadoCpe);
        Assert.Equal("No se puede emitir el comprobante porque tiene errores de validacion.", comprobante.Mensaje);
        Assert.True(string.IsNullOrWhiteSpace(comprobante.Hash));
        Assert.True(string.IsNullOrWhiteSpace(comprobante.NombreXml));
        Assert.Same(comprobante, comprobanteRepository.Comprobantes.Single());
    }

    [Fact]
    public async Task Registrar_comprobante_cpe_use_case_no_guarda_si_venta_es_de_otra_empresa()
    {
        var empresaAId = Guid.NewGuid();
        var empresaBId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var comprobanteRepository = new ComprobanteRepositoryFake();
        await ventaRepository.AgregarAsync(CrearVenta(ventaId, empresaBId));
        var useCase = new RegistrarComprobanteCpeUseCase(
            comprobanteRepository,
            ventaRepository,
            new EmpresaActivaContextFake(empresaAId));

        var comprobante = await useCase.EjecutarAsync(new RegistrarComprobanteCpeRequest(
            ventaId,
            "03",
            "B001",
            7,
            "SIMULADO"));

        Assert.Null(comprobante);
        Assert.Empty(comprobanteRepository.Comprobantes);
    }

    private static Venta CrearVenta(Guid ventaId, Guid empresaId)
    {
        return new Venta(
            ventaId,
            empresaId,
            DateTimeOffset.UtcNow,
            10m,
            0m,
            10m,
            [
                new VentaDetalle(
                    Guid.NewGuid(),
                    empresaId,
                    ventaId,
                    Guid.NewGuid(),
                    1m,
                    10m,
                    0m,
                    10m)
            ]);
    }

    private static async Task<EscenarioEmisionCpe> CrearEscenarioEmisionAsync(
        DateTimeOffset? fechaVenta = null)
    {
        var empresaId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var ventaRepository = new VentaRepositoryFake();
        var clienteRepository = new ClienteRepositoryFake();
        var productoRepository = new ProductoRepositoryFake();
        var varianteRepository = new ProductoVarianteRepositoryFake();
        var gateway = new CpeGatewayFake();

        await clienteRepository.AgregarAsync(new Cliente(
            clienteId,
            empresaId,
            "DNI",
            "12345678",
            "Cliente Demo"));
        await productoRepository.AgregarAsync(new Producto(
            productoId,
            empresaId,
            "Producto gravado",
            59m,
            "SKU-001"));

        var detalle = new VentaDetalle(
            Guid.NewGuid(),
            empresaId,
            ventaId,
            productoId,
            2m,
            50m,
            18m,
            118m);
        await ventaRepository.AgregarAsync(new Venta(
            ventaId,
            empresaId,
            fechaVenta ?? new DateTimeOffset(2026, 7, 11, 5, 0, 0, TimeSpan.Zero),
            100m,
            18m,
            118m,
            [detalle],
            clienteId));

        return new EscenarioEmisionCpe(
            empresaId,
            ventaId,
            ventaRepository,
            clienteRepository,
            productoRepository,
            varianteRepository,
            gateway);
    }

    private static async Task GuardarConfiguracionFiscalAsync(
        ConfiguracionFiscalEmpresaRepositoryFake configuracionFiscalRepository,
        Guid empresaId)
    {
        await configuracionFiscalRepository.GuardarAsync(new ConfiguracionFiscalEmpresa(
            empresaId,
            "20601234567",
            "CapitalPOS Fiscal SAC",
            "CapitalPOS Fiscal",
            "150102",
            "Calle Fiscal 456",
            "LIMA",
            "LIMA",
            "ANCON"));
    }

    private static EmitirCpeDesdeVentaUseCase CrearUseCaseEmision(
        EscenarioEmisionCpe escenario,
        ConfiguracionFiscalEmpresaRepositoryFake configuracionFiscalRepository)
    {
        return new EmitirCpeDesdeVentaUseCase(
            escenario.VentaRepository,
            configuracionFiscalRepository,
            escenario.ClienteRepository,
            escenario.ProductoRepository,
            escenario.VarianteRepository,
            escenario.Gateway,
            new EmpresaActivaContextFake(escenario.EmpresaId));
    }

    private sealed record EscenarioEmisionCpe(
        Guid EmpresaId,
        Guid VentaId,
        VentaRepositoryFake VentaRepository,
        ClienteRepositoryFake ClienteRepository,
        ProductoRepositoryFake ProductoRepository,
        ProductoVarianteRepositoryFake VarianteRepository,
        CpeGatewayFake Gateway);

    private static CrearVentaUseCase CrearUseCase(
        VentaRepositoryFake ventaRepository,
        ProductoRepositoryFake productoRepository,
        ProductoVarianteRepositoryFake varianteRepository,
        ClienteRepositoryFake clienteRepository,
        StockProductoRepositoryFake? stockRepository = null,
        Guid? empresaId = null)
    {
        return new CrearVentaUseCase(
            ventaRepository,
            productoRepository,
            varianteRepository,
            clienteRepository,
            stockRepository ?? new StockProductoRepositoryFake(),
            empresaId.HasValue
                ? new EmpresaActivaContextFake(empresaId.Value)
                : new EmpresaActivaContextFake());
    }

    private static CrearVentaRequest CrearVentaRequest(
        Guid productoId,
        Guid? productoVarianteId,
        decimal cantidad)
    {
        return new CrearVentaRequest(
            DateTimeOffset.UtcNow,
            null,
            [new CrearVentaDetalleRequest(productoId, productoVarianteId, cantidad, 10m, 0m, cantidad * 10m)]);
    }

    private sealed class VentaRepositoryFake : IVentaRepository
    {
        public List<Venta> Ventas { get; } = new();

        public bool LanzarExcepcionAlAgregar { get; set; }

        public Task AgregarAsync(Venta venta, CancellationToken cancellationToken = default)
        {
            if (LanzarExcepcionAlAgregar)
            {
                throw new InvalidOperationException("Fallo simulado al persistir venta.");
            }

            Ventas.Add(venta);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Venta>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Venta> ventas = Ventas
                .Where(venta => venta.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(ventas);
        }

        public Task<Venta?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var venta = Ventas.SingleOrDefault(venta =>
                venta.EmpresaId == empresaId && venta.Id == id);

            return Task.FromResult(venta);
        }
    }

    private sealed class ConfiguracionFiscalEmpresaRepositoryFake : IConfiguracionFiscalEmpresaRepository
    {
        private readonly List<ConfiguracionFiscalEmpresa> _configuraciones = new();

        public Task<ConfiguracionFiscalEmpresa?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            var configuracion = _configuraciones.SingleOrDefault(
                configuracion => configuracion.EmpresaId == empresaId);

            return Task.FromResult(configuracion);
        }

        public Task GuardarAsync(
            ConfiguracionFiscalEmpresa configuracion,
            CancellationToken cancellationToken = default)
        {
            var index = _configuraciones.FindIndex(
                actual => actual.EmpresaId == configuracion.EmpresaId);
            if (index >= 0)
            {
                _configuraciones[index] = configuracion;
            }
            else
            {
                _configuraciones.Add(configuracion);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ProductoRepositoryFake : IProductoRepository
    {
        private readonly List<Producto> _productos = new();

        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            _productos.Add(producto);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Producto>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Producto> productos = _productos
                .Where(producto => producto.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(productos);
        }

        public Task<Producto?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var producto = _productos.SingleOrDefault(producto =>
                producto.EmpresaId == empresaId && producto.Id == id);

            return Task.FromResult(producto);
        }

        public Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ProductoVarianteRepositoryFake : IProductoVarianteRepository
    {
        private readonly List<ProductoVariante> _variantes = new();

        public Task AgregarAsync(ProductoVariante variante, CancellationToken cancellationToken = default)
        {
            _variantes.Add(variante);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoVariante>> ListarPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ProductoVariante> variantes = _variantes
                .Where(variante => variante.EmpresaId == empresaId && variante.ProductoId == productoId)
                .ToArray();

            return Task.FromResult(variantes);
        }

        public Task<ProductoVariante?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var variante = _variantes.SingleOrDefault(variante =>
                variante.EmpresaId == empresaId && variante.Id == id);

            return Task.FromResult(variante);
        }
    }

    private sealed class ClienteRepositoryFake : IClienteRepository
    {
        private readonly List<Cliente> _clientes = new();

        public Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            _clientes.Add(cliente);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Cliente>> ListarPorEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Cliente> clientes = _clientes
                .Where(cliente => cliente.EmpresaId == empresaId)
                .ToArray();

            return Task.FromResult(clientes);
        }

        public Task<Cliente?> ObtenerPorEmpresaAsync(
            Guid empresaId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var cliente = _clientes.SingleOrDefault(cliente =>
                cliente.EmpresaId == empresaId && cliente.Id == id);

            return Task.FromResult(cliente);
        }

        public Task ActualizarAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StockProductoRepositoryFake : IStockProductoRepository
    {
        public List<StockProducto> Stocks { get; } = new();

        public Task<StockProducto?> ObtenerPorProductoAsync(
            Guid empresaId,
            Guid productoId,
            Guid? productoVarianteId = null,
            CancellationToken cancellationToken = default)
        {
            var stock = Stocks.SingleOrDefault(stock =>
                stock.EmpresaId == empresaId &&
                stock.ProductoId == productoId &&
                stock.ProductoVarianteId == productoVarianteId);

            return Task.FromResult(stock);
        }

        public Task GuardarAsync(
            StockProducto stock,
            CancellationToken cancellationToken = default)
        {
            var index = Stocks.FindIndex(actual => actual.Id == stock.Id);
            if (index >= 0)
            {
                Stocks[index] = stock;
            }
            else
            {
                Stocks.Add(stock);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class EmpresaActivaContextFake : IEmpresaActivaContext
    {
        public EmpresaActivaContextFake()
        {
        }

        public EmpresaActivaContextFake(Guid empresaId)
        {
            UsuarioId = Guid.NewGuid();
            EmpresaId = empresaId;
            Rol = RolEmpresa.Administrador;
            TieneEmpresaActiva = true;
        }

        public bool TieneEmpresaActiva { get; }

        public Guid UsuarioId { get; }

        public Guid EmpresaId { get; }

        public RolEmpresa Rol { get; }
    }

    private sealed class ComprobanteRepositoryFake : IComprobanteRepository
    {
        public List<Comprobante> Comprobantes { get; } = new();

        public Task AgregarAsync(Comprobante comprobante, CancellationToken cancellationToken = default)
        {
            Comprobantes.Add(comprobante);

            return Task.CompletedTask;
        }
    }

    private sealed class CpeGatewayFake : ICpeGateway
    {
        public JsonElement? UltimoRequest { get; private set; }

        public Task<CpeGatewayResponse> ObtenerEstadoAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CpeGatewayResponse(
                200,
                true,
                """{"ok":true}""",
                "application/json"));
        }

        public Task<CpeGatewayResponse> EmitirAsync(
            JsonElement request,
            CancellationToken cancellationToken = default)
        {
            UltimoRequest = request.Clone();

            return Task.FromResult(new CpeGatewayResponse(
                200,
                true,
                """
                {
                  "ok": true,
                  "data": {
                    "ok": true,
                    "estado": "SIMULADO",
                    "mensaje": "Comprobante aceptado en modo simulacion."
                  }
                }
                """,
                "application/json"));
        }
    }
}
