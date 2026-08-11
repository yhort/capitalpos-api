# CapitalPOS — Guion de demo del MVP retail

Documento para preparar y presentar el MVP actual a un cliente. Los comandos,
variables, migraciones y validaciones técnicas están en
[RUNBOOK_MVP.md](RUNBOOK_MVP.md).

La demo debe enseñar capacidades que funcionan hoy, distinguir claramente la
facturación opcional y evitar comprometer alcance todavía no construido.

## 1. Objetivo de la demo

Mostrar un flujo retail controlado de extremo a extremo:

```text
empresa activa
  -> sede y punto de venta
  -> caja abierta
  -> producto / variante / presentación
  -> stock por sede
  -> venta
  -> descuento de stock
  -> comprobante opcional
  -> cierre de caja
```

La historia principal no es únicamente “registrar una venta”. Es demostrar que
CapitalPOS conserva el contexto de empresa y sede, controla la caja, descuenta
la existencia correcta y puede extender la operación hacia CPE.

## 2. Estado actual del MVP

### Listo para mostrar

- Login y selección de empresa activa.
- Aislamiento multiempresa.
- Sedes y puntos de venta.
- Selección de `Tienda Demo` y `Caja Principal`.
- Categorías y marcas.
- Productos y variantes.
- Unidades y presentaciones comerciales.
- Stock básico por sede y por variante.
- Ajuste y consulta de stock desde Angular.
- Sesión de caja básica.
- Bloqueo de ventas cuando no existe caja abierta.
- Venta base, por variante o por presentación.
- Descuento de stock en unidades base según el factor de presentación.
- Persistencia de venta con empresa, sede y punto de venta.
- Series automáticas por empresa, sede y tipo de comprobante.
- Emisión CPE desde una venta registrada.
- Persistencia del comprobante y artefactos CPE.
- Dashboard y reportes comerciales iniciales.

### Validado técnicamente

- Migraciones aplicables sobre una base PostgreSQL limpia.
- DemoSeed de empresa, sede, punto de venta, producto, stock, unidades y serie.
- Suites automatizadas de backend y frontend.
- Flujo local Angular → API → PostgreSQL.
- Rechazo backend y frontend de venta sin caja abierta.
- Descuento en la sede de la venta, sin afectar otra sede.
- Venta por presentación con consumo `cantidad × factorConversion`.
- Rechazo por stock insuficiente sin venta ni descuento parcial.
- Conservación del carrito cuando el backend rechaza una venta.
- Persistencia de `ProductoPresentacionId` en `ventas_detalles`.
- Incremento de correlativo automático únicamente después de una emisión
  `SIMULADO` o `ACEPTADO`.
- Generación y persistencia de XML, ZIP y CDR cuando se incluye CPE.
- SUNAT beta validado previamente en un entorno controlado.

### Requiere cuidado durante la demo

- CPE es opcional. No debe bloquear una demo retail si el servicio no está
  levantado.
- SUNAT beta validado no equivale a habilitación para producción.
- La caja es básica: no calcula todavía un cierre contable completo.
- El stock es operativo básico, no un Kardex valorizado.
- Una presentación puede crearse y venderse, pero su edición y desactivación
  avanzada todavía no forman parte del flujo terminado.
- En algunas ejecuciones, la pantalla de presentaciones puede no cargar las
  unidades aunque el backend sí las devuelva. Preparar la presentación antes de
  la reunión o seguir el workaround del runbook.
- `ProductoVariante.StockActual` sigue siendo una deuda conceptual; para la
  operación multisede, presentar `stocks_productos` como fuente del stock por
  sede.
- `capitalpos-web/package-lock.json` tiene una modificación previa conocida.
  No atribuirla a la preparación de la demo ni intentar corregirla durante la
  reunión.

## 3. Dos modos de demo

### Modo A — Demo retail operativa sin CPE

Es el modo recomendado para una primera reunión comercial.

Servicios:

- PostgreSQL.
- `capitalpos-api`.
- `capitalpos-web`.

No se levanta `capitalpos-cpe-api`.

Historia:

1. Login y empresa activa.
2. Sede y punto de venta.
3. Control de caja.
4. Catálogo y presentación.
5. Stock por sede.
6. Venta.
7. Descuento de stock.
8. Cierre de caja.

Ventajas:

- Menos dependencias durante la presentación.
- Pone el foco en el valor operativo del comercio.
- Funciona aunque no estén disponibles certificado, credenciales SOL o red
  hacia SUNAT.
- Permite hablar de facturación como módulo integrado sin arriesgar el flujo
  principal.

Frase de transición:

> La operación retail ya queda registrada y controla caja y stock. La
> facturación electrónica se puede añadir al mismo flujo sin volver a capturar
> la venta.

### Modo B — Demo con facturación electrónica/CPE

Usar cuando el cliente haya pedido ver el comprobante o cuando el entorno haya
sido verificado antes de la reunión.

Servicios:

- Los tres componentes del modo retail.
- `capitalpos-cpe-api`.

Elegir antes de presentar:

- Simulación local: opción más estable para demostrar integración y artefactos.
- SUNAT beta: solo en una sesión técnica planificada, con certificado,
  credenciales, hora y conectividad ya validados.

Historia adicional:

1. Revisar configuración fiscal de la empresa.
2. Registrar la venta con caja abierta.
3. Elegir boleta o factura.
4. Explicar que serie y correlativo se asignan automáticamente por sede.
5. Emitir desde la venta.
6. Mostrar estado, comprobante persistido y artefactos.

Mensaje correcto:

> El flujo de facturación está conectado a la venta y ya fue validado contra
> SUNAT beta en un entorno controlado. Esta demostración no representa todavía
> una habilitación productiva del cliente.

No usar `capitalpos-cpe-api` únicamente para “tenerlo encendido”. Si no se
mostrará emisión, dejarlo fuera de la demo.

## 4. Datos demo recomendados

Preparar datos reconocibles, fáciles de explicar y sin información real de
clientes.

| Dato | Valor recomendado |
| --- | --- |
| Usuario | `admin@capitalpos.test` |
| Password | Valor local seguro; nunca mostrarlo ni documentarlo |
| Empresa | `CapitalPOS Demo S.A.C.` / `CapitalPOS Demo` |
| Empresa ID | `10000000-0000-0000-0000-000000000001` |
| RUC demo | `20600000001` |
| Sede | `Tienda Demo` |
| Punto de venta | `Caja Principal` |
| Categoría | `General` |
| Marca | `Demo` |
| Producto | `Producto Demo` |
| SKU producto | `DEMO-001` |
| Variante preparada | talla `M`, color `Negro`, SKU único `DEMO-M-NEG` |
| Presentación preparada | `CAJ`, factor `12`, no base |
| Precio presentación sugerido | `S/ 120.00` |
| Stock base para presentación | `24` unidades |
| Stock de variante sugerido | `10` unidades en `Tienda Demo` |
| Monto inicial de caja | `S/ 100.00` |
| Observación de apertura | `Demo cliente` |
| Canal de venta | `Tienda` |
| Cliente opcional | `Cliente Demo`, DNI ficticio `12345678` |
| Comprobante opcional | Boleta `03`, serie automática `B001` |

Usar un SKU o código de barras único si se crean datos durante la preparación.
Para una demo repetible, es mejor preparar la variante y presentación de
antemano y reservar la creación en vivo para una reunión enfocada en catálogo.

## 5. Preparación de la historia

### Seleccionar qué se demostrará

Antes de levantar servicios, decidir:

- venta base, variante o presentación;
- si se mostrará creación o únicamente consulta;
- si la caja se abrirá en vivo;
- si se incluirá CPE;
- simulación local o SUNAT beta;
- qué dato se mostrará en el dashboard al final.

No improvisar el alcance durante la reunión.

### Estado inicial recomendado

Para demostrar el mayor valor con pocos pasos:

- empresa demo activa;
- `Tienda Demo` y `Caja Principal` disponibles;
- ninguna sesión de caja antigua abierta;
- producto, categoría, marca, variante y presentación ya preparados;
- stock base `24` para vender una caja con factor `12`;
- sin ventas de prueba irrelevantes en la vista;
- CPE apagado en modo retail o completamente verificado en modo CPE.

Si se quiere mostrar el bloqueo de caja, iniciar con caja cerrada. Si el tiempo
es muy corto, abrirla durante el ensayo y dejar el punto exacto de la demo
acordado con el presentador.

## 6. Guion recomendado paso a paso

### 1. Introducción

Explicar en una frase:

> CapitalPOS organiza una operación retail por empresa y sede, controla caja y
> stock, y conecta la venta con facturación electrónica.

No comenzar mostrando terminales, tablas o configuración.

### 2. Login y empresa activa

1. Abrir Angular.
2. Iniciar sesión con el usuario demo.
3. Señalar el usuario y la empresa activa en la cabecera.
4. Explicar que el mismo usuario puede operar dentro del contexto autorizado
   de una empresa.

Resultado esperado: acceso al dashboard con la empresa demo activa.

### 3. Sede y punto de venta

1. Ir a `/app/ventas`.
2. Mostrar `Tienda Demo`.
3. Mostrar `Caja Principal`.
4. Explicar que ventas, stock, caja y series usan ese contexto.

Resultado esperado: sede y punto de venta seleccionados o autoseleccionados.

### 4. Control de caja

Si la caja comienza cerrada:

1. Agregar temporalmente un producto al carrito.
2. Mostrar `Registrar venta` deshabilitado.
3. Mostrar “Abre una sesión de caja para registrar ventas.”
4. Limpiar el carrito si hace falta.

Después:

1. Ingresar el monto inicial `S/ 100.00`.
2. Usar la observación `Demo cliente`.
3. Abrir la caja.
4. Confirmar estado `Abierta`.

Mensaje sugerido:

> El POS no permite registrar ventas fuera de una sesión de caja. La regla se
> valida tanto en la pantalla como en el backend.

### 5. Producto, categoría y marca

1. Ir a `/app/productos`.
2. Abrir `Producto Demo`.
3. Mostrar categoría `General` y marca `Demo`.
4. Señalar SKU, precio y estado.

Si se crea un producto en vivo, usar un nombre corto y datos únicos. No dedicar
la mayor parte de la reunión a llenar formularios.

### 6. Variante

1. Mostrar la variante preparada: talla `M`, color `Negro`.
2. Señalar su SKU independiente.
3. Explicar que aplica a giros como ropa, calzado o productos con atributos.

No presentar `ProductoVariante.StockActual` como stock consolidado. La
existencia operativa que se muestra al cliente es la registrada por sede.

### 7. Presentación

1. Mostrar `CAJ — Caja`.
2. Señalar factor `12`.
3. Explicar que una unidad comercial puede consumir varias unidades base.
4. Mostrar el precio específico de la presentación.

Mensaje sugerido:

> El cliente vende una caja, pero el inventario descuenta doce unidades base.

### 8. Stock por sede

1. Ir a `/app/inventario`.
2. Seleccionar `Tienda Demo`.
3. Seleccionar el producto o variante.
4. Mostrar disponible, reservado y libre.
5. Para venta por presentación, confirmar stock libre `24`.

Explicar que el ajuste pertenece a esa sede y no modifica automáticamente otra.

### 9. Registrar la venta

1. Volver a `/app/ventas`.
2. Confirmar sede, punto de venta y caja abierta.
3. Seleccionar el producto.
4. Seleccionar la variante o presentación elegida para el guion.
5. Ingresar cantidad.
6. Mostrar consumo estimado y total.
7. Registrar la venta.
8. Mostrar identificador, estado y total.

Para la presentación sugerida:

```text
24 unidades base
- 1 CAJ × factor 12
= 12 unidades base restantes
```

### 10. Mostrar el descuento de stock

1. Refrescar los datos de venta o volver a inventario.
2. Mostrar que el stock libre cambió de `24` a `12`.
3. Recalcar que el descuento ocurrió en `Tienda Demo`.

Si el guion incluye control negativo, intentar dos cajas con solo doce unidades
libres y mostrar el bloqueo sin persistencia parcial.

### 11. Emitir comprobante, opcional

Solo en modo B:

1. Mantener visible la venta registrada.
2. Elegir boleta `03` o factura `01`.
3. Explicar que serie y correlativo no son campos editables.
4. Emitir.
5. Mostrar serie real, correlativo, estado y comprobante persistido.
6. Si aporta valor, mostrar XML, ZIP y CDR sin abrir archivos sensibles.

En simulación, decir `SIMULADO`. En beta, describir exactamente la respuesta
obtenida. No usar la palabra “producción”.

### 12. Cerrar caja

1. Ingresar el monto declarado.
2. Agregar una observación de cierre.
3. Cerrar caja.
4. Mostrar estado cerrado y diferencia.
5. Mostrar que una nueva venta vuelve a quedar bloqueada.

Explicar que esta es una sesión de caja operativa básica, no todavía un cierre
contable con movimientos, medios de pago, retiros y conciliación.

### 13. Cierre comercial

Cerrar con tres ideas:

- contexto por empresa y sede;
- control operativo de caja y stock;
- facturación integrada como extensión del mismo flujo.

## 7. Mensaje comercial sugerido

### Versión breve

> CapitalPOS es una base operativa para comercios de distintos giros. Permite
> trabajar por empresa, sede y punto de venta; organizar productos con
> categorías, marcas, variantes y presentaciones; controlar stock por sede; y
> registrar ventas dentro de una sesión de caja. La facturación electrónica se
> integra directamente a la venta y ya fue validada en SUNAT beta, manteniendo
> producción como una etapa controlada posterior.

### Adaptación por giro

- Tienda de ropa: variantes por talla y color.
- Market: unidades, paquetes y cajas con factor de conversión.
- Librería: categorías, marcas y stock por local.
- Perfumería: variantes o presentaciones por tamaño.
- Distribuidor: venta por caja o paquete conservando unidades base.

Hablar de “retail multi-giro” significa que el núcleo se adapta a diferentes
catálogos y formas de venta. No significa que todos los procesos especializados
de cada industria estén terminados.

## 8. Qué no decir ni prometer todavía

No afirmar:

- “Ya está listo para SUNAT producción”.
- “El cliente puede salir a producción inmediatamente”.
- “La caja realiza un cierre contable completo”.
- “Ya tiene arqueos, retiros, ingresos, medios de pago y conciliación”.
- “Ya funciona con lector físico de códigos de barras”.
- “Ya genera notas de crédito o débito”.
- “Ya gestiona compras y proveedores”.
- “Ya tiene Kardex avanzado o valorización de inventario”.
- “Todos los reportes gerenciales están terminados”.
- “La presentación ya tiene edición y desactivación avanzada completa”.
- “El stock de variante está consolidado en `ProductoVariante.StockActual`”.

Formulaciones seguras:

- “SUNAT beta fue validado previamente; falta la habilitación final para cada
  entorno productivo”.
- “La caja cubre apertura, bloqueo de venta y cierre básico”.
- “El stock actual cubre disponibilidad por sede y descuento por venta”.
- “Los reportes actuales son iniciales y evolucionarán con el uso del cliente”.

## 9. Limitaciones actuales

### Caja básica

- Apertura y cierre por punto de venta.
- Monto inicial, monto declarado y diferencia básica.
- Sin movimientos detallados, retiros, ingresos, medios de pago o conciliación.
- La diferencia no representa todavía un cierre contable completo.

### Stock básico

- Disponible, reservado y libre por sede.
- Descuento por venta base, variante o factor de presentación.
- Sin Kardex avanzado, lotes, vencimientos, costos promedio o valorización.

### Catálogo y presentaciones

- Categorías, marcas, variantes, unidades y presentaciones operativas.
- Presentación funcional para venta y descuento por factor.
- Sin edición o desactivación avanzada completa de presentaciones.
- Puede requerir preparación por API si Angular no carga la lista de unidades.

### Variantes

- Venta y stock por variante disponibles.
- `ProductoVariante.StockActual` permanece como deuda conceptual.
- Para multisede, la referencia operativa es `stocks_productos`.

### Reportes

- Dashboard comercial y reportes iniciales.
- Sin cierre gerencial, contable o analítico avanzado.

### Facturación

- Emisión desde venta, serie automática y persistencia disponibles.
- Simulación local estable para demo.
- SUNAT beta validado previamente.
- Producción requiere validación final por empresa, certificado, credenciales,
  series, configuración fiscal y operación.
- Notas de crédito y débito fuera del MVP actual.

### Estado del workspace web

- `capitalpos-web/package-lock.json` tiene una modificación previa conocida.
- No ejecutar `npm install`, revertir ni explicar esa modificación como parte
  de la demo.

## 10. Plan de recuperación durante la demo

La prioridad es conservar la historia comercial. Si falla CPE, continuar con la
venta retail; si falla un dato, usar los datos preparados.

### Caja cerrada

Síntoma: `Registrar venta` deshabilitado.

Recuperación:

1. Confirmar `Tienda Demo` y `Caja Principal`.
2. Abrir caja en ese punto de venta.
3. Conservar o reconstruir el carrito.
4. No insertar una sesión directamente en PostgreSQL.

### Stock insuficiente

Síntoma: no se puede agregar o registrar la cantidad.

Recuperación:

1. Confirmar sede, producto y variante.
2. Para presentación, calcular `cantidad × factor`.
3. Volver a inventario y ajustar stock de la sede.
4. Refrescar ventas y repetir.
5. No cambiar el factor para ocultar el problema.

### Serie CPE no disponible

Síntoma: emisión rechazada por falta de serie activa.

Recuperación:

1. Confirmar empresa, sede y tipo de comprobante.
2. Verificar `03-B001` para boleta demo.
3. No intentar escribir serie o correlativo desde Angular.
4. Si no se puede corregir con seguridad, cerrar la historia en la venta
   registrada y explicar que facturación es el módulo opcional.

### CPE no levantado

Síntoma: la venta funciona, pero la emisión no conecta.

Recuperación:

1. No repetir la venta.
2. Confirmar health en `5097`.
3. Si CPE no estaba previsto, continuar en modo retail sin emisión.
4. Si estaba previsto y no se recupera rápidamente, mostrar la persistencia de
   la venta y pasar a cierre de caja.

### Fecha Lima / SUNAT

Síntoma: beta rechaza fecha futura o inconsistente.

Recuperación:

1. Confirmar hora del host y `America/Lima`.
2. Confirmar fecha de emisión.
3. No reutilizar payloads antiguos o futuros.
4. Si persiste, pasar a simulación únicamente si ese cambio fue acordado antes;
   no cambiar silenciosamente el modo frente al cliente.

### Puerto ocupado

Síntoma: API, Angular o CPE no inicia.

Recuperación:

1. Revisar `4200`, `5096` y `5097`.
2. Detener el proceso previo de forma normal.
3. Levantar APIs con `--no-launch-profile`.
4. No matar procesos desconocidos sin identificar su origen.

### Credenciales o secretos

Síntoma: login o CPE falla por configuración.

Recuperación:

1. Usar la fuente privada preparada para la demo.
2. No pegar secretos en una terminal compartida o visible.
3. No abrir `appsettings`, archivos `.env`, historial de shell ni el PFX.
4. Si faltan credenciales CPE, continuar en modo retail.
5. Si falta el password demo, detener la presentación y recuperarlo en privado.

### Presentación sin unidades visibles

Síntoma: Angular indica que no existen unidades, pero el seed las creó.

Recuperación:

1. Usar una presentación preparada antes de la reunión.
2. Refrescar la sección una sola vez.
3. Si sigue igual, no depurar frente al cliente.
4. Continuar con la presentación ya creada o con una venta base.

## 11. Checklist antes de mostrar al cliente

### Calidad

- [ ] `dotnet test CapitalPos.Api.sln -m:1 -nr:false` está verde.
- [ ] `npm test -- --watch=false` está verde con Node `v24.15.0`.
- [ ] El `git status` inicial está registrado.
- [ ] No se instaló ni actualizó ningún paquete durante la preparación.

### Servicios y base

- [ ] PostgreSQL responde.
- [ ] La base demo tiene todas las migraciones.
- [ ] `capitalpos-api` responde health en `5096`.
- [ ] Angular abre en `127.0.0.1:4200`.
- [ ] El proxy Angular apunta a la API principal.
- [ ] El DemoSeed creó empresa, sede, punto de venta, producto, stock, unidades y
      serie.

### Datos retail

- [ ] Login demo probado.
- [ ] Empresa activa correcta.
- [ ] `Tienda Demo` y `Caja Principal` disponibles.
- [ ] No queda una sesión antigua abierta.
- [ ] La apertura de caja fue ensayada.
- [ ] La caja está abierta si el guion comienza después de la apertura.
- [ ] Producto, categoría y marca preparados.
- [ ] Variante preparada si se mostrará.
- [ ] Presentación preparada si se mostrará.
- [ ] Stock suficiente en la sede correcta.
- [ ] El canal de venta está definido.
- [ ] El intento de prueba anterior no dejó carrito o mensajes confusos.

### CPE opcional

- [ ] Se decidió explícitamente si CPE forma parte de la demo.
- [ ] Si no forma parte, `capitalpos-cpe-api` no es una dependencia.
- [ ] Si forma parte, health de CPE responde en `5097`.
- [ ] API keys coinciden sin mostrarse.
- [ ] Configuración fiscal y serie están verificadas.
- [ ] Se eligió simulación o beta antes de la reunión.
- [ ] Certificado, credenciales, fecha Lima y conectividad están verificados si
      se usará beta.
- [ ] Se conoce el estado exacto que se espera mostrar.

### Seguridad y presentación

- [ ] No hay secretos, tokens, contraseñas o cadenas de conexión visibles.
- [ ] No se abrirán PFX ni archivos de configuración con credenciales.
- [ ] Las notificaciones del equipo están silenciadas.
- [ ] Solo están abiertas las ventanas necesarias.
- [ ] XML, ZIP y CDR de otras pruebas no se confundirán con la demo.
- [ ] Ningún PFX, XML, ZIP o CDR generado está versionado.
- [ ] Existe un punto claro para continuar sin CPE si la emisión falla.

## 12. Cierre después de la reunión

- Cerrar la sesión de caja si quedó abierta.
- Detener Angular, API y CPE opcional.
- Confirmar libres los puertos `4200`, `5096` y `5097`.
- Eliminar tokens y credenciales temporales.
- Conservar únicamente evidencia no sensible acordada.
- Comparar `git status` final contra el inicial.
- Registrar preguntas del cliente como necesidades por validar, no como
  compromisos ya aceptados.
