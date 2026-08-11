# CapitalPOS - Estado actual y próximo bloque (corte: 11 agosto 2026)

Este documento es el contexto de arranque para el agente (Cursor) al retomar el desarrollo. No reemplaza `PLANNING.md`/`TASKS.md`/`Roadmap.md`, pero tiene prioridad sobre ellos si hay conflicto: fue verificado línea por línea contra el código real, mientras que las notas de `TASKS.md` han demostrado no siempre coincidir con el estado real del repo.

**Instrucción para el agente antes de empezar cualquier tarea de este documento:** inspeccionar el estado real del repo (entidades en `src/CapitalPos.Domain`, migraciones aplicadas, `git log`) en vez de asumir desde `TASKS.md`. No marcar una tarea como completada en `TASKS.md` sin que el commit correspondiente esté efectivamente en el repo y las pruebas pasen.

**Nota de proceso (leer antes de continuar):** entre un corte anterior de este documento y el 10 agosto 2026, se construyeron cinco módulos completos (pedidos digitales, notas de crédito SUNAT, compras, kardex, pagos/anulación de venta) sin pasar por revisión intermedia, a pesar de que `Roadmap.md` — las reglas operativas del propio agente — indica explícitamente esperar autorización ante decisiones de arquitectura/riesgo y no hacer commit/push sin autorización explícita. La auditoría de ese día confirmó que el trabajo es técnicamente sólido, pero el tamaño del salto fue mayor al acordado. Ver sección 6 (regla de checkpoint reforzada) sobre cómo evitar que se repita.

## 1. Estado verificado (multisede — cerrado y confirmado en código real)

Confirmado en `capitalpos-api` y `capitalpos-web`, ambos repos actualizados en GitHub:

- `Sede`, `PuntoVenta`, `SerieComprobante`, `Categoria`, `Marca`, `UnidadMedida`, `ProductoPresentacion`, `SesionCaja`, `ModoManejoProducto` — todas existen con sus migraciones aplicadas en orden.
- `Venta.SedeId` y `Venta.PuntoVentaId` son campos reales obligatorios.
- `StockProducto.SedeId` es obligatorio; el stock se descuenta correctamente por sede.
- Frontend (`capitalpos-web`) tiene selectores reales de sede/punto de venta, no entrada manual.
- Módulo de caja en Angular conectado al flujo de venta.

**Conclusión:** la base de multisede está sólida. No reabrir este bloque salvo bug reportado.

## 2. `FIX-001` y `PREC-001` — verificados, correctos

- `ProductoVariante.StockActual` fue eliminado (ya no existe en el modelo ni en migraciones nuevas).
- `VentaDetalle` tiene `FactorConversionAplicado` y `CantidadBaseDescontada`, poblados al crear la venta con el factor vigente en ese instante.
- `ReglaPrecioMayorista` (`EmpresaId`, `ProductoId`, `CantidadMinima`, `PrecioUnitarioMayorista`) existe y está validada; `VentaDetalle.PrecioMayoristaAplicado` distingue ventas a precio mayor/menor.

## 3. Módulos construidos fuera del plan acordado — auditados el 10 agosto 2026, técnicamente sólidos

Entre el corte anterior y el 10 agosto 2026 se agregaron, sin pasar por revisión intermedia: pedidos digitales, notas de crédito SUNAT, compras, kardex, pagos/anulación de venta, e infraestructura (CORS/secretos por ambiente). Auditoría de los de mayor riesgo (dinero e impuestos):

- **Anulación de venta + nota de crédito SUNAT** (`AnularVentaUseCase`, `EmitirNotaCreditoDesdeVentaUseCase`): correcto. Usa tipo `07` y motivo `01` (catálogo SUNAT correcto), exige nota de crédito aceptada antes de anular si la venta tenía comprobante aceptado, bloquea la anulación si SUNAT rechaza la nota. Reutiliza `CantidadBaseDescontada` de `FIX-001` para revertir stock en unidad base correctamente sin importar la presentación vendida.
- **Kardex** (`MovimientoInventario`): consistente entre venta (`VENTA`), anulación (`ANULACION_VENTA`) y compra (`INGRESO_COMPRA`), siempre con `SedeId`, con referencia a la operación de origen.
- **Pedidos digitales**: usa correctamente el mecanismo de stock reservado (`StockProducto.Reservar`/`CantidadReservada`) ya existente, no un mecanismo paralelo.
- **Compras**: correctamente ligadas a `SedeId`.

**Pruebas automatizadas:** confirmadas en verde en `AUDIT-001` (11 agosto 2026): `908/908` passed con PostgreSQL de pruebas.

## 4. `REP-001` — completado (11 agosto 2026)

`REP-001` (reporte de ventas por sede y vendedor) **está construido y cerrado**.

- Se mantiene `ReporteVentasPorCanalUseCase` (agrupa por `CanalVenta`) tal cual.
- Se agregó `ReporteVentasPorSedeVendedorUseCase` + `GET /api/reportes/ventas-por-sede-vendedor`.
- Agrupa por `SedeId` y `VendedorId` (incluye vendedor `null`), con rango de fechas `desde`/`hasta`.
- Alcance: totales por sede/vendedor (cantidad de ventas, unidades, soles, precio promedio). Sin gráficos ni exportación.
- Validado con build OK y pruebas del reporte en verde (unitarias + HTTP).

## 5. `AUDIT-001` — completado (11 agosto 2026)

`AUDIT-001` **está cerrado**.

- Suite completa: Failed `0`, Passed `908`, Skipped `0`.
- E2E (11 tests) corren contra PostgreSQL local con `CAPITALPOS_TEST_CONNECTION_STRING` → `Database=capitalpos_test` (no SQL Server).
- Flaky `Dashboard_comercial_devuelve_resumen_top_y_stock_bajo` corregido: el marcador multiempresa dejó de ser `"999"` (colisionaba con Guid hex) y pasó a `"8888.88"`.
- Misma corrección aplicada a aserciones HTTP similares de reportes/dashboard.

Para repetir localmente:

```bash
export CAPITALPOS_TEST_CONNECTION_STRING='Host=localhost;Port=5432;Database=capitalpos_test;Username=yhortcruz'
dotnet build CapitalPos.Api.sln -m:1 -nr:false
dotnet test CapitalPos.Api.sln -m:1 -nr:false
```

## 6. Próximo bloque de trabajo

### `REP-005` — Pantalla Angular de reporte por sede y vendedor

Prioridad alta y continuación natural de `REP-001` (API ya lista; el índice Angular solo cubre ventas por canal vía `REP-002`/`REP-003`/`REP-004`).

Alcance mínimo propuesto:

- Consumir `GET /api/reportes/ventas-por-sede-vendedor` desde `capitalpos-web`.
- Pantalla `/app/reportes/ventas-por-sede-vendedor` con rango de fechas y totales por sede/vendedor.
- Enlace desde el índice `/app/reportes` sin romper el reporte por canal.
- Sin gráficos nuevos ni exportación en este bloque.

### Regla de checkpoint reforzada (ver también `Roadmap.md`, sección "Forma de trabajo")

A partir de ahora, después de **cada módulo individual** (no cada bloque grande), el agente debe:
1. Mostrar el diff (`git diff --stat`) y un resumen de qué se construyó.
2. Esperar confirmación explícita antes de empezar el siguiente módulo — aunque el módulo anterior haya sido autorizado como parte de un plan más amplio.
3. No asumir que la autorización de un bloque cubre módulos adicionales no mencionados explícitamente en ese bloque.

## 7. Explícitamente fuera de este bloque

No construir todavía (esperar confirmación de necesidad real antes de empezar):

- Combinación de variante + presentación como concepto general (`ProductoPresentacion.ProductoVarianteId`) — la necesidad real resultó ser precio por cantidad (`PREC-001`, ya construido), no presentaciones sobre variante.
- Transferencias de stock entre sedes.
- Configuración fiscal por sede más allá del código de establecimiento.
- Roles de plataforma SaaS y onboarding self-service.
- SUNAT producción (pendiente de trámite regulatorio, no solo de código).
- Bandeja de pagos externos (`PAG-002`) e integraciones WooCommerce (`INT-001+`) — pendientes en `TASKS.md`, pero después de cerrar la UI del reporte sede/vendedor.
- Cualquier módulo adicional no listado en la sección 6, sin importar cuán razonable parezca en el momento — debe pasar primero por esta conversación.
