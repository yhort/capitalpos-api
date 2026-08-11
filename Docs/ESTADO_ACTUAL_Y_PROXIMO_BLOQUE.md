# CapitalPOS - Estado actual y próximo bloque (corte: 11 agosto 2026)

Este documento es el contexto de arranque para el agente (Cursor) al retomar el desarrollo. No reemplaza `PLANNING.md`/`TASKS.md`/`Roadmap.md`, pero tiene prioridad sobre ellos si hay conflicto: fue verificado línea por línea contra el código real, mientras que las notas de `TASKS.md` han demostrado no siempre coincidir con el estado real del repo.

**Instrucción para el agente antes de empezar cualquier tarea de este documento:** inspeccionar el estado real del repo (entidades en `src/CapitalPos.Domain`, migraciones aplicadas, `git log`) en vez de asumir desde `TASKS.md`. No marcar una tarea como completada en `TASKS.md` sin que el commit correspondiente esté efectivamente en el repo y las pruebas pasen.

**Nota de proceso (leer antes de continuar):** entre un corte anterior de este documento y el 10 agosto 2026, se construyeron cinco módulos completos (pedidos digitales, notas de crédito SUNAT, compras, kardex, pagos/anulación de venta) sin pasar por revisión intermedia, a pesar de que `Roadmap.md` — las reglas operativas del propio agente — indica explícitamente esperar autorización ante decisiones de arquitectura/riesgo y no hacer commit/push sin autorización explícita. La auditoría de ese día confirmó que el trabajo es técnicamente sólido, pero el tamaño del salto fue mayor al acordado. Ver sección 5 (regla de checkpoint reforzada) sobre cómo evitar que se repita.

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

**No auditado a fondo todavía:** endpoints/autorización de estos módulos nuevos, y las pruebas automatizadas asociadas — pendiente de `AUDIT-001`.

## 4. `REP-001` — completado (11 agosto 2026)

`REP-001` (reporte de ventas por sede y vendedor) **está construido y cerrado**.

- Se mantiene `ReporteVentasPorCanalUseCase` (agrupa por `CanalVenta`) tal cual.
- Se agregó `ReporteVentasPorSedeVendedorUseCase` + `GET /api/reportes/ventas-por-sede-vendedor`.
- Agrupa por `SedeId` y `VendedorId` (incluye vendedor `null`), con rango de fechas `desde`/`hasta`.
- Alcance: totales por sede/vendedor (cantidad de ventas, unidades, soles, precio promedio). Sin gráficos ni exportación.
- Validado con build OK y pruebas del reporte en verde (unitarias + HTTP).

## 5. Próximo bloque de trabajo

### `AUDIT-001` — Confirmar pruebas en verde de los módulos construidos fuera de plan

Antes de seguir sumando alcance nuevo, correr y confirmar (con salida real, no solo el número que se reporte en `TASKS.md`):

```bash
dotnet build CapitalPos.Api.sln -m:1 -nr:false
dotnet test CapitalPos.Api.sln -m:1 -nr:false
```

Pegar el resultado real. Prestar atención especial a pruebas de: `AnularVentaUseCase`, `EmitirNotaCreditoDesdeVentaUseCase`, kardex (`MovimientoInventario`), pedidos digitales.

Notas del entorno local observadas al cerrar `REP-001` (no bloquean el cierre de ese módulo, sí relevantes para `AUDIT-001`):

- Las pruebas E2E de CPE fallan sin `CAPITALPOS_TEST_CONNECTION_STRING` (PostgreSQL de pruebas).
- Existe al menos un test HTTP flaky del dashboard que busca el substring `"999"` en el JSON (puede coincidir con un Guid aleatorio).

### Regla de checkpoint reforzada (ver también `Roadmap.md`, sección "Forma de trabajo")

A partir de ahora, después de **cada módulo individual** (no cada bloque grande), el agente debe:
1. Mostrar el diff (`git diff --stat`) y un resumen de qué se construyó.
2. Esperar confirmación explícita antes de empezar el siguiente módulo — aunque el módulo anterior haya sido autorizado como parte de un plan más amplio.
3. No asumir que la autorización de un bloque cubre módulos adicionales no mencionados explícitamente en ese bloque.

## 6. Explícitamente fuera de este bloque

No construir todavía (esperar confirmación de necesidad real antes de empezar):

- Combinación de variante + presentación como concepto general (`ProductoPresentacion.ProductoVarianteId`) — la necesidad real resultó ser precio por cantidad (`PREC-001`, ya construido), no presentaciones sobre variante.
- Transferencias de stock entre sedes.
- Configuración fiscal por sede más allá del código de establecimiento.
- Roles de plataforma SaaS y onboarding self-service.
- SUNAT producción (pendiente de trámite regulatorio, no solo de código).
- Cualquier módulo adicional no listado en la sección 5, sin importar cuán razonable parezca en el momento — debe pasar primero por esta conversación.
