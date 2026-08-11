# CapitalPOS - Demo SUNAT Beta

Guia corta para ejecutar la demo cliente del MVP Retail Multiempresa con emision real en SUNAT beta.

## Estado actual

La demo esta validada end-to-end desde Angular hasta SUNAT beta:

```text
capitalpos-web -> capitalpos-api -> capitalpos-cpe-api -> SUNAT beta
```

El flujo probado permite iniciar sesion, usar empresa activa, verificar configuracion fiscal, crear producto, crear cliente, registrar venta, emitir boleta y recibir respuesta `ACEPTADO` con CDR.

## Commits relevantes

- CPE: `88aef5382d1d1f9ec99b3f8a256281a33e1868d5`
- API fiscal/emision: `4e064c4beefba137803e8ac960d8444c2b31b246`
- API fecha Lima: `7d87aa93e57da5dd7d051305c54921514ce5a0b2`
- Web configuracion fiscal: `aab8ff63f4a46aab3a9b7314944ecb797c1667b9`
- Web fecha Lima: `1e78687416817516e92307acf10d41a5181cb438`

## Pruebas verdes

- `capitalpos-api`: 475 tests
- `capitalpos-cpe-api`: 20 tests
- `capitalpos-web`: 59 tests

## Ultima validacion real

- Validacion: `DEMO-003`
- Fecha Angular por defecto: `2026-07-12`
- Venta: `d7aac196-de81-4b3c-b319-0d37ab1b9a86`
- Boleta: `B001-341017`
- Estado CPE: `ACEPTADO`
- CDR `ResponseCode`: `0`

## Comandos

No colocar secretos reales en archivos del repo. Usar `dotnet user-secrets` o variables de entorno locales.

### CPE API

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-cpe-api/CapitalPos.Cpe

ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://127.0.0.1:5097 \
CpeSettings__Modo=BETA \
CpeSettings__SimularFirma=false \
CpeSettings__SimularEnvioSunat=false \
dotnet run --no-launch-profile --project CapitalPos.Cpe.Api/CapitalPos.Cpe.Api.csproj
```

### API principal

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-api

ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://127.0.0.1:5096 \
CpeApi__BaseUrl=http://127.0.0.1:5097/ \
dotnet run --no-launch-profile --project src/CapitalPos.Api/CapitalPos.Api.csproj
```

### Angular

```bash
cd /Users/yhortcruz/Documents/Dev/capitalpos-web

source ~/.nvm/nvm.sh
nvm use
npm start -- --host 127.0.0.1 --port 4200
```

Abrir:

```text
http://127.0.0.1:4200/
```

## Checklist previo

- PostgreSQL local activo.
- Migraciones de `capitalpos-api` aplicadas.
- `capitalpos-cpe-api` responde `GET /api/health`.
- `capitalpos-api` responde `GET /api/health`.
- Angular responde en `http://127.0.0.1:4200/`.
- Diagnostico CPE beta limpio antes de emitir.
- Certificado PFX configurado fuera del repo.
- Usuario SOL, clave SOL, password de certificado y API key configurados sin exponer secretos.
- Correlativo nuevo y no repetido.
- Fecha visible en `/app/ventas` corresponde a la fecha de Lima.

## Guion de demo

1. Abrir `http://127.0.0.1:4200/`.
2. Iniciar sesion con usuario demo.
3. Confirmar empresa activa.
4. Ir a `Configuracion`.
5. Verificar datos fiscales activos.
6. Ir a `Productos`.
7. Crear producto basico con precio `S/ 118.00`.
8. Ir a `Ventas`.
9. Crear o seleccionar cliente DNI.
10. Agregar producto a la venta.
11. Verificar subtotal `S/ 100.00`, IGV `S/ 18.00` y total `S/ 118.00`.
12. Registrar venta.
13. Emitir boleta:
    - Tipo: Boleta `03`
    - Serie: `B001`
    - Correlativo: nuevo/no repetido
    - RUC emisor: el RUC beta configurado
14. Confirmar en pantalla:
    - Estado visual `exito`
    - Estado CPE `ACEPTADO`
    - XML, ZIP y CDR visibles.
15. Confirmar CDR en carpeta `capitalpos-cpe-files/BETA/CDR`.

## Datos demo recomendados

- Empresa demo: `10000000-0000-0000-0000-000000000001`
- Usuario demo: `admin@capitalpos.test`
- Producto: nombre con sufijo unico, precio `118.00`
- Cliente: DNI demo unico de 8 digitos
- Tipo comprobante: `03`
- Serie: `B001`
- Correlativo: usar uno alto y no repetido
- RUC emisor: usar el RUC beta configurado localmente

## No tocar durante la demo

- No cambiar credenciales.
- No usar correlativo repetido.
- No cambiar RUC, certificado PFX ni credenciales SOL.
- No cambiar `SimularFirma=false` ni `SimularEnvioSunat=false`.
- No commitear XML, ZIP, CDR, PFX, certificados, claves ni historiales.
- No exponer secretos en pantalla, chat, commits o documentos.

## Limitaciones del MVP

- Sin stock real.
- Sin caja ni cierre de caja.
- Sin notas de credito.
- Facturas aun no ampliamente validadas.
- SUNAT validado en beta, no produccion.
- Catalogo retail basico, sin variantes avanzadas de talla/color.

## Recuperacion si falla

- Revisar `GET /api/cpe/diagnostico`.
- Confirmar que la fecha de venta sea fecha Lima.
- Usar correlativo nuevo.
- Verificar puertos `4200`, `5096` y `5097`.
- Confirmar que `capitalpos-api` apunta a `capitalpos-cpe-api`.
- Revisar historial en `capitalpos-cpe-files/BETA/HISTORIAL`.
- Revisar si existe XML, ZIP o CDR para el comprobante.
- Si SUNAT responde error, conservar el XML y el historial local para diagnostico, sin commitearlos.

## Mensaje comercial sugerido

CapitalPOS ya cuenta con un MVP retail multiempresa que permite registrar una venta y emitir una boleta electronica validada en SUNAT beta desde la interfaz web. La demo muestra el flujo operativo completo: configuracion fiscal por empresa, catalogo basico, cliente, venta, envio a SUNAT beta, respuesta aceptada y CDR generado. Las siguientes etapas naturales son robustecer inventario, caja, reportes, notas de credito y validacion ampliada de facturas antes de pasar a produccion.
