# CapitalPOS — Runbook local del MVP retail

Guía operativa para levantar y validar localmente el MVP retail multiempresa.

Este documento no contiene secretos reales. Todo valor escrito como `<...>` debe
obtenerse de una fuente local segura y nunca debe guardarse en Git, capturas,
logs compartidos o documentación.

## 1. Arquitectura local

```text
capitalpos-web (Angular, 127.0.0.1:4200)
    |
    | /api mediante proxy de desarrollo
    v
capitalpos-api (ASP.NET Core, localhost:5096)
    |
    | solo al emitir CPE
    v
capitalpos-cpe-api (ASP.NET Core, localhost:5097, opcional)
    |
    +--> simulación local o SUNAT beta
    +--> capitalpos-cpe-files (XML, ZIP, CDR e historial)

capitalpos-api --> PostgreSQL local (localhost:5432)
```

Responsabilidades:

- `capitalpos-web`: login, empresa activa, productos, variantes,
  presentaciones, inventario por sede, caja, ventas y emisión desde la venta.
- `capitalpos-api`: multiempresa, sedes, puntos de venta, catálogo, stock,
  sesiones de caja, ventas, series automáticas y persistencia de comprobantes.
- `capitalpos-cpe-api`: generación, firma, envío o simulación y almacenamiento
  de artefactos CPE. No es necesario para probar catálogo, stock, caja o ventas.
- PostgreSQL: persistencia del backend principal.
- `capitalpos-cpe-files`: carpeta local de artefactos. No debe versionarse.

## 2. Requisitos y puertos

### Software

- PostgreSQL local y herramientas `createdb`, `dropdb` y `psql`.
- .NET SDK `10.0.301` o compatible para `capitalpos-api`; el repositorio fija
  esa versión en `global.json`.
- .NET SDK 7 compatible con `net7.0` para `capitalpos-cpe-api`.
- `nvm`.
- Node.js `v24.15.0`, definido en `capitalpos-web/.nvmrc`.
- npm provisto por esa versión de Node.
- `curl` y `jq` para diagnósticos o preparación por API.

Comprobación:

```bash
dotnet --list-sdks
psql --version

cd /Users/yhortcruz/Documents/Dev/capitalpos-web
source ~/.nvm/nvm.sh
nvm use
node --version
```

### Puertos

| Componente | Dirección |
| --- | --- |
| PostgreSQL | `localhost:5432` |
| `capitalpos-api` | `http://localhost:5096` |
| `capitalpos-web` | `http://127.0.0.1:4200` |
| `capitalpos-cpe-api` opcional | `http://localhost:5097` |

Usar `--no-launch-profile` al levantar las APIs. De lo contrario,
`launchSettings.json` puede imponer otro puerto.

Preflight:

```bash
for port in 4200 5096 5097; do
  lsof -nP -iTCP:$port -sTCP:LISTEN
done
```

La ausencia de salida indica que el puerto está libre.

## 3. Variables de entorno y secretos

### `capitalpos-api`

```bash
ASPNETCORE_ENVIRONMENT="Development"
ASPNETCORE_URLS="http://localhost:5096"
ConnectionStrings__CapitalPos="Host=localhost;Port=5432;Database=capitalpos_mvp;Username=<usuario-postgres>"
Jwt__SigningKey="<clave-jwt-local-segura>"
DemoSeed__Enabled="true"
DemoSeed__AdminPassword="<password-demo-local>"

# Necesarias para emitir; pueden quedar configuradas aunque CPE no esté levantado.
CpeApi__BaseUrl="http://localhost:5097/"
CpeApi__ApiKey="<api-key-local-cpe>"
```

Consideraciones del seed:

- Solo se ejecuta en `Development`.
- `DemoSeed__AdminPassword` es obligatorio cuando el seed está habilitado.
- En una base existente, el seed no reemplaza una credencial ya creada.
- No reutilizar una contraseña real.

### `capitalpos-cpe-api` opcional

Configuración común:

```bash
ASPNETCORE_ENVIRONMENT="Development"
ASPNETCORE_URLS="http://localhost:5097"
CpeSecuritySettings__ApiKey="<api-key-local-cpe>"
CpeSettings__Modo="BETA"
CpeSettings__RutaArchivos="/Users/yhortcruz/Documents/Dev/capitalpos-cpe-files"
```

El valor de `CpeSecuritySettings__ApiKey` debe ser idéntico a
`CpeApi__ApiKey` en `capitalpos-api`.

Modo simulado recomendado para una validación local:

```bash
CpeSettings__SimularFirma="true"
CpeSettings__SimularEnvioSunat="true"
CpeSettings__GuardarCdrSimulado="true"
```

SUNAT beta real, únicamente cuando esa integración forme parte de la prueba:

```bash
CpeSettings__SimularFirma="false"
CpeSettings__SimularEnvioSunat="false"
CpeSettings__RutaCertificado="<ruta-absoluta-certificado-pfx>"
CpeSettings__PasswordCertificado="<password-certificado>"
CpeSettings__UsuarioSol="<usuario-sol>"
CpeSettings__ClaveSol="<clave-sol>"
```

La URL beta ya tiene un valor de desarrollo en el proyecto. No activar envío
real ni producción durante una validación retail ordinaria.

### Manejo seguro

- Preferir variables exportadas desde una terminal privada o un mecanismo de
  secretos local.
- No escribir valores reales en este archivo, archivos `appsettings*.json`,
  comandos compartidos o tickets.
- Eliminar archivos temporales de credenciales al terminar.
- Nunca copiar PFX, XML, ZIP o CDR a un repositorio.

## 4. Orden de arranque

El orden normal es:

1. PostgreSQL y base local.
2. Migraciones de `capitalpos-api`.
3. `capitalpos-api`.
4. `capitalpos-web`.
5. `capitalpos-cpe-api`, solo si se probará emisión CPE.

### 4.1 Crear una base local

Para una validación destructiva desde cero, usar un nombre temporal:

```bash
dropdb --if-exists capitalpos_mvp
createdb capitalpos_mvp
```

No ejecutar `dropdb` contra una base compartida o con información necesaria.

### 4.2 Aplicar migraciones

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-api

dotnet tool restore

ConnectionStrings__CapitalPos="Host=localhost;Port=5432;Database=capitalpos_mvp;Username=<usuario-postgres>" \
dotnet ef database update \
  --project src/CapitalPos.Infrastructure \
  --startup-project src/CapitalPos.Api
```

Verificar la última migración aplicada:

```bash
psql "host=localhost port=5432 dbname=capitalpos_mvp user=<usuario-postgres>" \
  -P pager=off \
  -c 'select "MigrationId" from "__EFMigrationsHistory" order by "MigrationId";'
```

La migración de caja `AgregarSesionesCaja` debe aparecer junto con las de
sedes, stock por sede, presentaciones y series.

### 4.3 Levantar `capitalpos-api`

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-api

ASPNETCORE_ENVIRONMENT="Development" \
ASPNETCORE_URLS="http://localhost:5096" \
ConnectionStrings__CapitalPos="Host=localhost;Port=5432;Database=capitalpos_mvp;Username=<usuario-postgres>" \
Jwt__SigningKey="<clave-jwt-local-segura>" \
DemoSeed__Enabled="true" \
DemoSeed__AdminPassword="<password-demo-local>" \
CpeApi__BaseUrl="http://localhost:5097/" \
CpeApi__ApiKey="<api-key-local-cpe>" \
dotnet run --no-launch-profile --project src/CapitalPos.Api
```

Health:

```bash
curl --fail --silent --show-error http://localhost:5096/api/health | jq
```

### 4.4 Levantar `capitalpos-web`

En otra terminal:

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-web

source ~/.nvm/nvm.sh
nvm use
npm start -- --host 127.0.0.1 --port 4200
```

Abrir `http://127.0.0.1:4200/`.

Angular usa `src/proxy.conf.json` para enviar `/api` a
`http://localhost:5096`; el navegador no debe apuntar directamente a CPE.

### 4.5 Levantar `capitalpos-cpe-api` solo para CPE

En otra terminal:

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-cpe-api/CapitalPos.Cpe

ASPNETCORE_ENVIRONMENT="Development" \
ASPNETCORE_URLS="http://localhost:5097" \
CpeSecuritySettings__ApiKey="<api-key-local-cpe>" \
CpeSettings__Modo="BETA" \
CpeSettings__RutaArchivos="/Users/yhortcruz/Documents/Dev/capitalpos-cpe-files" \
CpeSettings__SimularFirma="true" \
CpeSettings__SimularEnvioSunat="true" \
CpeSettings__GuardarCdrSimulado="true" \
dotnet run --no-launch-profile --project CapitalPos.Cpe.Api/CapitalPos.Cpe.Api.csproj
```

Health:

```bash
curl --fail --silent --show-error http://localhost:5097/api/health | jq
```

Antes de una prueba SUNAT beta, sustituir el modo simulado por las variables
seguras indicadas en la sección 3 y revisar el health/diagnóstico sin imprimir
credenciales.

## 5. Datos creados por `DemoSeed`

Con una base limpia, `Development` y `DemoSeed__Enabled=true`:

| Dato | Valor demo |
| --- | --- |
| Usuario | `admin@capitalpos.test` |
| Empresa ID | `10000000-0000-0000-0000-000000000001` |
| RUC | `20600000001` |
| Sede | `Tienda Demo` |
| Sede ID | `10000000-0000-0000-0000-000000000004` |
| Punto de venta | `Caja Principal` |
| Punto de venta ID | `10000000-0000-0000-0000-000000000005` |
| Producto | `Producto Demo` |
| SKU | `DEMO-001` |
| Stock base inicial | `20` |
| Categoría / marca | `General` / `Demo` |
| Unidades | `UND`, `CAJ`, `PAQ`, `DOC`, `KG` |
| Serie boleta | tipo `03`, serie `B001`, correlativo inicial `0` |

El password es el valor local de `DemoSeed__AdminPassword`; no se documenta.

## 6. Flujo funcional actual

### 6.1 Login, empresa, sede y punto de venta

1. Iniciar sesión con el correo demo y el password local.
2. Confirmar la empresa activa.
3. Abrir `/app/ventas`.
4. Confirmar o seleccionar `Tienda Demo`.
5. Confirmar o seleccionar `Caja Principal`.
6. Si se cambia de sede, volver a seleccionar un punto de venta perteneciente
   a esa sede.

Todos los endpoints protegidos por empresa requieren el header
`X-CapitalPos-EmpresaId`. Angular lo añade a partir de la empresa activa.

### 6.2 Configuración fiscal, solo si se emitirá CPE

Antes de emitir:

- verificar que la empresa tenga configuración fiscal activa;
- verificar RUC, razón social, ubigeo y dirección;
- confirmar la serie activa para la sede y tipo de comprobante;
- confirmar que la API CPE está levantada y usa el mismo API key;
- elegir explícitamente simulación o SUNAT beta.

La serie y el correlativo no se editan en Angular. El backend selecciona
`SerieComprobante` por empresa, sede y tipo; incrementa el correlativo solo
cuando el resultado es `SIMULADO` o `ACEPTADO`.

### 6.3 Abrir caja

En la región `Caja` de `/app/ventas`:

1. Confirmar `Sin caja abierta`.
2. Agregar un producto al carrito y verificar:
   - `Registrar venta` deshabilitado;
   - mensaje “Abre una sesión de caja para registrar ventas.”
3. Ingresar monto inicial y observación.
4. Pulsar `Abrir caja`.
5. Confirmar estado `Abierta`.

El backend también rechaza una venta sin sesión abierta para ese punto de
venta; el bloqueo no depende solamente de Angular.

### 6.4 Producto, variante y presentación

En `/app/productos`:

1. Crear o verificar categoría y marca.
2. Crear o verificar el producto.
3. Crear o verificar una variante, si se probará venta por talla/color/SKU.
4. Crear o verificar una presentación:
   - unidad, por ejemplo `CAJ`;
   - `factorConversion`, por ejemplo `12`;
   - precio de venta;
   - código de barras opcional y único;
   - `esUnidadBase=false` para una presentación comercial.

El producto demo ya existe, pero no trae variantes ni presentaciones.

### 6.5 Ajustar stock por sede

En `/app/inventario`:

1. Seleccionar `Tienda Demo`.
2. Seleccionar producto y, si aplica, variante.
3. Consultar stock.
4. Ajustar `CantidadDisponible`.
5. Confirmar `Stock libre = disponible - reservado`.

El stock pertenece a una sede. No asumir que ajustar una sede modifica otra.

### 6.6 Registrar ventas

Con caja abierta:

- Venta base: seleccionar el producto sin variante ni presentación.
- Venta de variante: seleccionar la variante y comprobar su stock en la sede.
- Venta por presentación: seleccionar la presentación. La cantidad del carrito
  se expresa en presentaciones, pero el stock se consume en unidad base.

Ejemplo:

```text
stock base inicial: 24
presentación: CAJ
factor: 12
venta: 1 CAJ
consumo base: 12
stock base final: 12
```

Para cada venta:

1. Confirmar sede, punto de venta y caja abierta.
2. Agregar el producto al carrito.
3. Revisar cantidad, consumo estimado y total.
4. Registrar.
5. Confirmar mensaje de éxito.
6. Refrescar inventario y validar el descuento en la misma sede.
7. Verificar que una cantidad superior al stock libre sea rechazada sin venta
   parcial ni descuento parcial.

### 6.7 Emitir CPE desde la venta, si aplica

1. Mantener la venta recién registrada visible.
2. Seleccionar boleta `03` o factura `01`.
3. Confirmar el RUC emisor.
4. Pulsar `Emitir comprobante`.
5. Confirmar:
   - serie asignada por backend;
   - correlativo real;
   - estado `SIMULADO` en modo local o `ACEPTADO` según respuesta beta;
   - comprobante persistido;
   - XML, ZIP y CDR generados cuando corresponda.

Artefactos esperados bajo la ruta configurada:

```text
capitalpos-cpe-files/BETA/XML
capitalpos-cpe-files/BETA/ZIP
capitalpos-cpe-files/BETA/CDR
capitalpos-cpe-files/BETA/HISTORIAL
```

### 6.8 Cerrar caja

1. Ingresar monto declarado y observación de cierre.
2. Pulsar `Cerrar caja`.
3. Confirmar estado cerrado, fecha de cierre y diferencia.
4. Agregar otro producto al carrito.
5. Confirmar nuevamente el bloqueo y el mensaje de caja.
6. Para una prueba de consistencia, forzar una venta por API y comprobar que
   responde `400` sin crear venta ni descontar stock.

La diferencia actual se calcula como:

```text
diferenciaCierre = montoDeclaradoCierre - montoInicial
```

## 7. Consultas `curl` útiles

Preparar autenticación sin imprimir la contraseña:

```bash
read -s "DEMO_PASSWORD?Password demo local: "
echo

TOKEN=$(
  jq -n \
    --arg correo "admin@capitalpos.test" \
    --arg password "$DEMO_PASSWORD" \
    '{correo:$correo,password:$password}' |
  curl --fail --silent --show-error \
    -H 'Content-Type: application/json' \
    --data-binary @- \
    http://localhost:5096/api/auth/login |
  jq -r '.accessToken'
)

unset DEMO_PASSWORD

EMPRESA_ID="10000000-0000-0000-0000-000000000001"
SEDE_ID="10000000-0000-0000-0000-000000000004"
PUNTO_VENTA_ID="10000000-0000-0000-0000-000000000005"
PRODUCTO_ID="10000000-0000-0000-0000-000000000006"
```

Consultas:

```bash
# Sedes
curl --fail --silent --show-error \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-CapitalPos-EmpresaId: $EMPRESA_ID" \
  http://localhost:5096/api/sedes | jq

# Puntos de venta de la sede
curl --fail --silent --show-error \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-CapitalPos-EmpresaId: $EMPRESA_ID" \
  "http://localhost:5096/api/sedes/$SEDE_ID/puntos-venta" | jq

# Stock base por sede
curl --fail --silent --show-error \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-CapitalPos-EmpresaId: $EMPRESA_ID" \
  "http://localhost:5096/api/stock/productos/$PRODUCTO_ID?sedeId=$SEDE_ID" | jq

# Caja abierta; 404 es esperado si no existe una sesión abierta
curl --silent --show-error --write-out '\nHTTP %{http_code}\n' \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-CapitalPos-EmpresaId: $EMPRESA_ID" \
  "http://localhost:5096/api/caja/sesiones/abierta?puntoVentaId=$PUNTO_VENTA_ID"

# Configuración fiscal
curl --silent --show-error --write-out '\nHTTP %{http_code}\n' \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-CapitalPos-EmpresaId: $EMPRESA_ID" \
  http://localhost:5096/api/configuracion-fiscal/
```

Al terminar:

```bash
unset TOKEN EMPRESA_ID SEDE_ID PUNTO_VENTA_ID PRODUCTO_ID
```

## 8. Checks SQL útiles

Definir la conexión libpq; no pasar a `psql` la cadena con formato .NET:

```bash
PGURL="host=localhost port=5432 dbname=capitalpos_mvp user=<usuario-postgres>"
```

Sedes y puntos de venta:

```bash
psql "$PGURL" -P pager=off \
  -c 'select "Id", "EmpresaId", "Nombre", "CodigoEstablecimiento", "Activa" from sedes order by "Nombre";' \
  -c 'select "Id", "EmpresaId", "SedeId", "Nombre", "Activo" from puntos_venta order by "Nombre";'
```

Stock por sede:

```bash
psql "$PGURL" -P pager=off -c '
select
  sp."SedeId",
  sp."ProductoId",
  sp."ProductoVarianteId",
  sp."CantidadDisponible",
  sp."CantidadReservada",
  sp."CantidadDisponible" - sp."CantidadReservada" as "StockLibre"
from stocks_productos sp
order by sp."SedeId", sp."ProductoId", sp."ProductoVarianteId";'
```

Sesiones de caja:

```bash
psql "$PGURL" -P pager=off -c '
select
  "Id", "EmpresaId", "SedeId", "PuntoVentaId", "Estado",
  "MontoInicial", "MontoDeclaradoCierre", "DiferenciaCierre",
  "FechaApertura", "FechaCierre",
  "ObservacionApertura", "ObservacionCierre"
from sesiones_caja
order by "FechaApertura" desc;'
```

Ventas y detalles:

```bash
psql "$PGURL" -P pager=off \
  -c '
select
  "Id", "EmpresaId", "SedeId", "PuntoVentaId",
  "Fecha", "FechaCreacion", "Estado", "Total"
from ventas
order by "FechaCreacion" desc
limit 20;' \
  -c '
select
  "Id", "VentaId", "ProductoId", "ProductoVarianteId",
  "ProductoPresentacionId", "Cantidad", "PrecioUnitario", "Total"
from ventas_detalles
order by "VentaId"
limit 50;'
```

La tabla real es `ventas_detalles`, en plural.

Comprobantes y series:

```bash
psql "$PGURL" -P pager=off \
  -c '
select
  "Id", "VentaId", "EmpresaId", "TipoComprobante",
  "Serie", "Correlativo", "EstadoCpe",
  "NombreXml", "NombreZip", "NombreCdr", "FechaCreacion"
from comprobantes
order by "FechaCreacion" desc
limit 20;' \
  -c '
select
  "Id", "EmpresaId", "SedeId", "TipoComprobante",
  "Serie", "CorrelativoActual", "Activa"
from series_comprobante
order by "EmpresaId", "SedeId", "TipoComprobante", "Serie";'
```

Limpiar la variable al terminar:

```bash
unset PGURL
```

## 9. Troubleshooting

### Venta bloqueada por caja cerrada

Síntomas:

- `Registrar venta` deshabilitado;
- “Abre una sesión de caja para registrar ventas.”;
- API `400`: debe abrir una sesión de caja.

Acciones:

1. Confirmar sede y punto de venta.
2. Consultar `/api/caja/sesiones/abierta?puntoVentaId=...`.
3. Abrir caja para ese punto de venta.
4. No insertar sesiones directamente en SQL.

### Stock insuficiente

El stock se valida en unidades base, dentro de la sede de la venta.

- Para una presentación, calcular `cantidad × factorConversion`.
- Verificar `CantidadDisponible - CantidadReservada`.
- Verificar `SedeId` y, si aplica, `ProductoVarianteId`.
- Después de un rechazo, confirmar que stock, ventas y detalles no cambiaron.

### La presentación no muestra unidades

Puede ocurrir que `/app/productos` muestre “No hay unidades de medida
disponibles” aunque el seed haya creado unidades.

1. Confirmar `GET /api/unidades-medida` con token y empresa activa.
2. Confirmar en SQL la tabla `unidades_medida`.
3. Refrescar la página y la sección de presentaciones.
4. Si la pantalla sigue bloqueada, crear la presentación por API como
   workaround documentado y registrar la incidencia frontend:

```bash
UNIDAD_CAJ_ID="10000000-0000-0000-0000-000000000012"

jq -n \
  --arg productoId "$PRODUCTO_ID" \
  --arg unidadMedidaId "$UNIDAD_CAJ_ID" \
  '{
    productoId:$productoId,
    unidadMedidaId:$unidadMedidaId,
    factorConversion:12,
    esUnidadBase:false,
    precioVenta:120,
    codigoBarras:null
  }' |
curl --fail --silent --show-error \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-CapitalPos-EmpresaId: $EMPRESA_ID" \
  -H 'Content-Type: application/json' \
  --data-binary @- \
  "http://localhost:5096/api/productos/$PRODUCTO_ID/presentaciones" | jq

unset UNIDAD_CAJ_ID
```

No repetir el POST si la presentación ya existe.

### Serie CPE no configurada

Síntomas:

- emisión rechazada por serie inexistente o inactiva;
- Angular no puede corregir serie/correlativo manualmente.

Acciones:

1. Consultar `series_comprobante`.
2. Confirmar empresa, `SedeId`, tipo `03` o `01`, serie y `Activa=true`.
3. En una base demo, confirmar `03-B001` para `Tienda Demo`.
4. No enviar `Serie` o `Correlativo` desde Angular como fuente de verdad.

### Fecha Lima / SUNAT

El servicio CPE valida que la fecha de emisión no sea futura usando la fecha
local del proceso.

```bash
date
TZ=America/Lima date
```

- Confirmar que host y procesos usan fecha/hora de Lima.
- No reutilizar una fecha futura o desfasada en payloads.
- En SUNAT beta, revisar fecha de emisión y zona horaria antes de atribuir el
  rechazo a firma, credenciales o conectividad.

### `package-lock.json` ya estaba modificado

Antes de iniciar:

```bash
git -C /Users/yhortcruz/Documents/Dev/capitalpos-web status --short
```

- Registrar si la modificación era previa.
- No ejecutar `npm install` para esta validación.
- No revertir cambios ajenos.
- `nvm use`, `npm test` y `npm start` no autorizan modificar o restaurar el
  lockfile.

### CPE no está levantado

Es esperado si solo se prueban catálogo, inventario, caja y venta.

- No levantar `capitalpos-cpe-api` por rutina.
- La venta debe poder registrarse sin CPE.
- Solo la emisión requiere `5097`, API key y configuración CPE.

### PostgreSQL no conecta

- Confirmar servicio en `5432`.
- Confirmar base y usuario.
- Confirmar `ConnectionStrings__CapitalPos`.
- Aplicar migraciones.
- Recordar que `psql` usa formato libpq:
  `host=... dbname=... user=...`.

### Puerto incorrecto u ocupado

```bash
lsof -nP -iTCP:5096 -sTCP:LISTEN
lsof -nP -iTCP:4200 -sTCP:LISTEN
lsof -nP -iTCP:5097 -sTCP:LISTEN
```

Usar `dotnet run --no-launch-profile`; sin esa opción la API puede abrir el
puerto indicado por `launchSettings.json`.

### Proxy/CORS

- Angular debe abrirse en `127.0.0.1:4200`.
- El proxy debe apuntar a `http://localhost:5096`.
- Angular no consume directamente `capitalpos-cpe-api`.
- Confirmar health de la API antes de cambiar configuración CORS.

### API key CPE vacía o distinta

- `CpeSecuritySettings__ApiKey`: valida llamadas recibidas por CPE.
- `CpeApi__ApiKey`: valor enviado por la API principal.
- Deben coincidir y no estar vacías.
- `/api/health` de CPE es público; un health verde no prueba el API key de
  emisión.

## 10. Pruebas, cierre y checklist final

### Pruebas backend

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-api
dotnet test CapitalPos.Api.sln -m:1 -nr:false
```

### Pruebas frontend

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-web
source ~/.nvm/nvm.sh
nvm use
npm test -- --watch=false
```

### Detener servicios

Detener con `Ctrl+C` las terminales de Angular, API y CPE opcional. Después:

```bash
for port in 4200 5096 5097; do
  if lsof -nP -iTCP:$port -sTCP:LISTEN >/dev/null 2>&1; then
    echo "$port OCUPADO"
  else
    echo "$port LIBRE"
  fi
done
```

### Checklist de entrega

- [ ] Pruebas de `capitalpos-api` verdes.
- [ ] Pruebas de `capitalpos-web` verdes con Node `v24.15.0`.
- [ ] Login y empresa activa correctos.
- [ ] Sede y punto de venta correctos.
- [ ] Venta bloqueada sin caja.
- [ ] Caja abre, venta registra y stock de la sede disminuye.
- [ ] Venta base, variante o presentación validada según el alcance.
- [ ] La presentación descuenta por factor cuando aplica.
- [ ] Caja cierra y vuelve a bloquear ventas.
- [ ] Serie automática y comprobante persistido si se probó CPE.
- [ ] XML, ZIP y CDR presentes solo si se probó CPE.
- [ ] Puertos `4200`, `5096` y `5097` libres al terminar.
- [ ] No quedan credenciales, tokens ni secretos temporales.
- [ ] Git no contiene XML, ZIP, CDR, historial ni PFX generados.
- [ ] Se comparó el `git status` final con el inicial.

Comprobación de repositorios:

```bash
git -C /Users/yhortcruz/Documents/Dev/capitalpos-api status --short
git -C /Users/yhortcruz/Documents/Dev/capitalpos-web status --short
git -C /Users/yhortcruz/Documents/Dev/capitalpos-cpe-api/CapitalPos.Cpe status --short

git -C /Users/yhortcruz/Documents/Dev/capitalpos-cpe-api/CapitalPos.Cpe \
  ls-files |
  rg -i '(^|/)(capitalpos-cpe-files|BETA/(XML|ZIP|CDR|HISTORIAL))(/|$)|\.pfx$'
```

La última búsqueda debe quedar sin resultados para artefactos o certificados
generados.
