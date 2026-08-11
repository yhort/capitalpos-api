# CapitalPOS - Planning Tecnico del Ecosistema

Este documento resume el plan de trabajo recomendado para evolucionar CapitalPOS hacia un POS comercial SaaS multiempresa, usando los hallazgos tecnicos actuales. No define implementacion inmediata; ordena prioridades, riesgos, tareas y criterios de aceptacion.

## Arquitectura actual

El ecosistema esta separado en tres proyectos principales:

- `capitalpos-web`: aplicacion Angular para login, dashboard, empresa activa y flujo CPE.
- `capitalpos-api`: API principal del sistema. Maneja autenticacion, usuarios, empresas, permisos, multiempresa y actua como fachada hacia CPE.
- `capitalpos-cpe-api`: servicio especializado de comprobantes electronicos. Maneja validacion, XML, firma, envio SUNAT/simulacion, CDR, archivos y diagnosticos.

La separacion actual es conceptualmente correcta para un SaaS POS, siempre que `capitalpos-cpe-api` se mantenga como servicio interno y no sea consumido directamente desde el frontend.

## Flujo correcto

El flujo objetivo debe mantenerse asi:

```text
capitalpos-web
    |
    | JWT + X-CapitalPos-EmpresaId cuando corresponda
    v
capitalpos-api
    |
    | X-API-KEY interna + contrato CPE normalizado
    v
capitalpos-cpe-api
    |
    | XML / Firma / SUNAT / CDR
    v
SUNAT
```

Responsabilidades esperadas:

- `capitalpos-web` no debe conocer secretos CPE ni comunicarse con `capitalpos-cpe-api`.
- `capitalpos-api` debe actuar como gateway/fachada, aplicar seguridad, permisos, empresa activa, auditoria y normalizacion de errores.
- `capitalpos-cpe-api` debe concentrar la complejidad tecnica de facturacion electronica.
- SUNAT debe quedar aislado detras del servicio CPE.

## Riesgos principales

1. Contrato CPE inconsistente entre `capitalpos-cpe-api`, `capitalpos-api` y `capitalpos-web`.
2. Emisiones funcionalmente exitosas o simuladas pueden mostrarse como `error inesperado` en Angular por clasificacion HTTP/DTO insuficiente.
3. Multiempresa todavia esta probado en identidad y acceso, pero no en datos POS transaccionales.
4. Endpoints administrativos pueden mezclar alcance global SaaS con alcance de empresa.
5. `capitalpos-cpe-api` no debe exponerse publicamente; la API key simple solo es aceptable en red privada.
6. Falta cobertura de pruebas visible en `capitalpos-cpe-api`.
7. Emisor CPE temporal en frontend no es valido para SaaS real; debe salir de la empresa activa.
8. Versiones .NET desalineadas entre API principal y CPE API.
9. Riesgo de alargar demasiado el desarrollo por mejoras arquitectonicas antes de terminar el flujo de venta.

## Decision de enfoque MVP

CapitalPOS debe avanzar como un solo sistema POS retail multiempresa y multi-giro. No se creara otro backend para ropa retail.

La arquitectura queda asi:

- `capitalpos-api`: backend principal de negocio retail, ventas, productos, clientes, caja, inventario simple, comprobantes y persistencia en PostgreSQL.
- `capitalpos-cpe-api`: servicio interno especializado solo en facturacion electronica: XML, firma, SUNAT, CDR y archivos.
- `capitalpos-web`: aplicacion Angular para operar el POS.

`capitalpos-cpe-api` no debe contener logica de productos, tallas, colores, stock, caja ni ventas. Solo recibe un comprobante preparado por `capitalpos-api` y devuelve el resultado CPE.

El MVP se enfocara primero en retail comercial:

- tiendas de ropa;
- markets;
- perfumerias;
- librerias;
- negocios pequenos y medianos con venta directa.

Fabricacion textil queda fuera del MVP. Para una empresa textil que empieza venta retail, el primer alcance sera vender prendas terminadas con variantes, no controlar produccion, insumos, mermas ni costos industriales.

## Donde entra la base de datos

La base de datos entra desde el inicio del MVP de negocio, en `capitalpos-api`.

Ya existe PostgreSQL + EF Core para usuarios, empresas y relaciones usuario-empresa. Para el MVP retail se deben agregar nuevas entidades persistentes mediante migraciones EF Core:

- productos;
- categorias;
- marcas;
- variantes opcionales;
- clientes;
- ventas;
- detalle de venta;
- comprobantes;
- caja simple;
- movimientos basicos de stock si el MVP lo requiere.

Regla: cada entidad transaccional o configurable por empresa debe tener `EmpresaId`.

La base de datos no debe esperar a que SUNAT real este listo. Primero se guardan ventas y comprobantes en modo simulacion; luego se reemplaza el resultado CPE simulado por envio real cuando existan certificado, credenciales y validacion SUNAT beta/produccion.

## Prioridades por fase

### Fase 1 - Estabilizar contrato CPE

Objetivo: que el flujo CPE sea predecible de punta a punta.

Estado: cerrado para `capitalpos-api` y `capitalpos-web`; pendiente de seguir endureciendo `capitalpos-cpe-api` con documentacion interna y pruebas si se trabaja ese servicio.

Evidencia frontend:

- `capitalpos-web` commit `88647ce` alinea modelos de emision, nullability, clasificacion visual y manejo de `HttpErrorResponse`.
- Angular muestra `SIMULADO` y `ACEPTADO` como exito, `RECHAZADO` como rechazo funcional y `ERROR_VALIDACION` como error de validacion.
- Las respuestas HTTP 4xx/5xx con cuerpo publico normalizado usan mensaje, errores y `data.estado` del contrato.
- Pruebas Angular: `npm test -- --watch=false`, 38 pruebas aprobadas.

- Definir contrato canonico para `/api/cpe/emitir`.
- Alinear DTOs entre CPE API, gateway del API principal y Angular.
- Decidir politica HTTP para errores funcionales:
  - opcion recomendada: respuestas funcionales como `200` con `ok=false` para rechazos controlados;
  - reservar `5xx` para errores tecnicos reales.
- Corregir clasificacion visual de Angular para `SIMULADO`, `ACEPTADO`, `RECHAZADO`, `ERROR_VALIDACION`, `ERROR_SUNAT` y error tecnico.
- Agregar pruebas contractuales basicas.

### Fase 2 - Blindar multiempresa

Objetivo: evitar fugas de datos antes de construir ventas, productos, inventario y caja.

- Definir regla obligatoria: toda entidad POS transaccional debe tener `EmpresaId`.
- Definir repositorios/use cases siempre filtrados por empresa activa.
- Separar roles de plataforma SaaS de roles dentro de empresa.
- Revisar endpoints globales de empresas/usuarios.
- Crear pruebas anti-fuga entre empresas.

### Fase 3 - MVP Retail Multiempresa

Objetivo: terminar un flujo vendible del sistema POS antes de seguir agregando mejoras tecnicas.

Alcance MVP:

- catalogo de productos multiempresa;
- variantes opcionales para retail de ropa: talla, color, sku/codigo de barras;
- clientes;
- venta y detalle de venta;
- caja simple;
- emision de boleta/factura desde la venta usando `capitalpos-cpe-api` en modo simulacion;
- persistencia del resultado CPE en `capitalpos-api`;
- pantalla Angular POS minima para vender.

No incluir en el MVP:

- fabricacion textil;
- compras avanzadas;
- kardex complejo;
- multiples almacenes avanzados;
- contabilidad;
- dashboards avanzados;
- SUNAT real produccion;
- superadmin SaaS avanzado;
- optimizaciones como circuit breaker, rate limiting o modularizacion profunda.

### Fase 4 - SUNAT real / beta controlada

Objetivo: pasar de simulacion a envio real controlado cuando el flujo de venta ya exista.

- configurar certificado PFX, password, usuario SOL y clave SOL;
- `SimularFirma=false`;
- `SimularEnvioSunat=false`;
- enviar boleta/factura a SUNAT beta;
- leer CDR real;
- persistir `ACEPTADO`, `RECHAZADO` o `ERROR_SUNAT` en el comprobante;
- mantener simulacion disponible para desarrollo.

### Fase 5 - Seguridad y despliegue interno

Objetivo: asegurar el despliegue real del ecosistema.

- Mantener `capitalpos-cpe-api` en red privada.
- Definir CORS solo en `capitalpos-api`.
- Rotar y proteger `CpeApi__ApiKey`.
- Propagar `X-Correlation-Id` entre APIs.
- Agregar rate limiting donde corresponda.
- Revisar almacenamiento de JWT en frontend.

### Fase 6 - Calidad tecnica CPE

Objetivo: reducir riesgo en facturacion electronica.

- Crear proyecto de tests para `capitalpos-cpe-api`.
- Cubrir validacion, XML, firma simulada, envio SUNAT simulado, CDR y errores.
- Modularizar internamente CPE API si empieza a crecer.
- Planificar actualizacion de runtime .NET.

### Fase 7 - Mejora continua POS SaaS

Objetivo: ampliar modulos comerciales despues del MVP.

- Compras.
- Reportes.
- Configuracion fiscal por empresa.
- inventario avanzado;
- multiples almacenes;
- roles de plataforma;
- despliegue productivo endurecido;
- fabricacion textil si el cliente lo requiere despues.

No avanzar en fabricacion ni modulos avanzados hasta cerrar el flujo de venta retail.

## Tareas por proyecto

### `capitalpos-web`

- Alinear modelos TypeScript con la respuesta publica real de `capitalpos-api`.
- Mejorar manejo de errores HTTP en emision CPE.
- Mostrar estados funcionales sin caer en `error inesperado`.
- Separar estado de conexion CPE, estado de empresa activa y permiso de emision.
- Reemplazar `CPE_EMISOR_TEMPORAL_CONFIG` por datos obtenidos desde la empresa activa.
- Agregar tests para clasificacion de respuestas CPE.

### `capitalpos-api`

- Mantener rol de gateway/fachada hacia CPE.
- Formalizar DTO publico de emision CPE.
- Normalizar errores CPE de forma estable y documentada.
- Definir politica HTTP para rechazos funcionales vs errores tecnicos.
- Propagar correlation id hacia `capitalpos-cpe-api`.
- Agregar resiliencia en `ICpeGateway`: timeout explicito, retry controlado y circuit breaker si aplica.
- Revisar endpoints de empresas/usuarios para distinguir alcance SaaS global y alcance empresa.
- Definir convencion `EmpresaId` obligatoria para nuevas entidades POS.

### `capitalpos-cpe-api`

- Mantener como servicio independiente e interno.
- Documentar contrato canonico de `ApiResponse<CpeEmisionResponse>`.
- Agregar tests automatizados.
- Evitar exponer Swagger y endpoints internos fuera de desarrollo/red privada.
- Revisar API key: comparacion segura, rotacion y configuracion por ambiente.
- Modularizar servicios CPE si crece el numero de casos de uso.
- Planificar actualizacion de .NET.

## Orden recomendado de trabajo

1. Cerrar contrato CPE end-to-end. Estado: completado.
2. Definir reglas multiempresa minimas. Estado: avanzado.
3. Congelar mejoras no criticas en backlog.
4. Crear modelo minimo de productos retail multiempresa en `capitalpos-api`.
5. Agregar variantes opcionales para ropa: talla/color/SKU/codigo de barras.
6. Crear clientes.
7. Crear venta y detalle de venta.
8. Persistir venta en PostgreSQL con `EmpresaId`.
9. Emitir boleta/factura desde una venta usando `capitalpos-cpe-api` en simulacion.
10. Guardar resultado CPE en `capitalpos-api`.
11. Construir pantalla Angular POS minima.
12. Validar flujo completo: login -> empresa activa -> venta -> comprobante simulado.
13. Recien despues preparar SUNAT beta real.

## Criterios de aceptacion

### Contrato CPE

- Una emision simulada devuelve estado `SIMULADO` y Angular la muestra como exito.
- Una emision aceptada devuelve estado `ACEPTADO` y Angular la muestra como exito.
- Un rechazo funcional devuelve `RECHAZADO` o estado equivalente y Angular lo muestra como rechazo, no como error inesperado.
- Un error tecnico real se muestra como error tecnico con mensaje seguro.
- Ninguna respuesta publica expone `X-API-KEY`, rutas internas, certificados ni credenciales.

### Multiempresa

- Todo endpoint transaccional exige empresa activa.
- Un usuario de empresa A no puede leer ni operar datos de empresa B.
- Los endpoints globales solo estan disponibles para roles de plataforma.
- Las pruebas cubren aislamiento por `EmpresaId`.

### Seguridad

- `capitalpos-cpe-api` no es consumido directamente por `capitalpos-web`.
- `capitalpos-cpe-api` no queda expuesto publicamente sin proteccion de red.
- `capitalpos-api` tiene CORS explicito para dominios permitidos.
- Los secretos se configuran por user-secrets, variables de entorno o gestor de secretos.
- Existe correlation id entre frontend, API principal y CPE API.

### Calidad

- `capitalpos-api` y `capitalpos-web` tienen pruebas para el flujo CPE.
- `capitalpos-cpe-api` tiene pruebas minimas de validacion, XML, firma, SUNAT simulado y CDR.
- La documentacion de ejecucion local indica claramente que API levanta en cada puerto y que secretos requiere.

### MVP Retail

- Se puede crear un producto por empresa.
- Se puede crear una variante opcional por talla/color cuando aplique.
- Se puede registrar un cliente.
- Se puede crear una venta con detalle.
- La venta queda persistida en PostgreSQL con `EmpresaId`.
- Se puede emitir boleta desde la venta en modo simulacion.
- El resultado CPE queda guardado junto al comprobante.
- Angular permite operar una venta minima sin usar endpoints directos de CPE.

## Trabajo recomendado por pestana/chat de Codex

Para evitar mezclar cambios y mantener foco, se recomienda dividir el trabajo en pestanas o chats independientes:

### Hilo actual - Backend CPE `capitalpos-cpe-api`

Este chat queda reservado para trabajar el backend especializado `capitalpos-cpe-api`.

Alcance principal:

- Documentar y congelar el contrato canonico interno `ApiResponse<CpeEmisionResponse>`.
- Alinear la respuesta de `/api/cpe/emitir` con el contrato end-to-end consumido por `capitalpos-api`.
- Crear la base de pruebas automatizadas para validacion, XML, firma, SUNAT simulado y CDR.
- Endurecer seguridad interna: API key, Swagger, diagnosticos, secretos y correlation id.
- Evaluar modularizacion interna si el servicio CPE sigue creciendo.

Reglas de coordinacion:

- Mantener `capitalpos-cpe-api` como servicio interno; el frontend no debe consumirlo directamente.
- Tratar `capitalpos-api` como la unica API publica para Angular y como gateway hacia CPE.
- No exponer API keys, certificados, credenciales SOL, rutas internas ni cuerpos sensibles en respuestas o logs.
- Si aparece una diferencia entre el contrato CPE interno y el contrato publico de `capitalpos-api`, documentar la diferencia antes de ajustar codigo.
- Priorizar pruebas contractuales y unitarias antes de ampliar nuevos casos de uso.

Primer bloque recomendado:

1. Completado: documentar `capitalpos-cpe-api/CapitalPos.Cpe/CapitalPos.Cpe.Api/Docs/ContratoApiEmitirCpe.md`.
2. Completado: alinear `POST /api/cpe/emitir` para conservar `data.estado` en errores controlados.
3. Completado: verificar emision de boleta `03/B001` en modo simulacion con estado `SIMULADO` y archivos XML/ZIP/CDR.
4. Siguiente: crear proyecto de tests para `capitalpos-cpe-api`.
5. Siguiente: automatizar cobertura de `SIMULADO`, `ACEPTADO`, `RECHAZADO`, `ERROR_VALIDACION`, `ERROR_SUNAT`, XML, firma y CDR.

### Chat 1 - Contrato CPE end-to-end

Proyecto principal: `capitalpos-api`.

Objetivo:

- Definir contrato publico final para `/api/cpe/emitir`.
- Alinear normalizador y DTO publico.
- Agregar pruebas del contrato.

Dependencias:

- Debe conocer la respuesta actual de `capitalpos-cpe-api`.
- Debe coordinarse con el Chat 2 para el modelo Angular.

### Chat 2 - Frontend CPE y error inesperado

Proyecto principal: `capitalpos-web`.

Estado: pendiente en otro hilo.

Objetivo:

- Corregir clasificacion visual de emision.
- Alinear modelos TypeScript.
- Mostrar estados funcionales correctamente.
- Agregar pruebas de servicio/componente donde aplique.

Dependencias:

- Esperar o seguir el contrato definido en Chat 1.

### Chat 3 - Multiempresa base SaaS

Proyecto principal: `capitalpos-api`.

Objetivo:

- Definir reglas de aislamiento por `EmpresaId`.
- Revisar endpoints globales vs endpoints por empresa.
- Proponer o implementar permisos de plataforma separados.
- Preparar patron para futuras entidades POS.

Dependencias:

- No depende del flujo CPE, puede avanzar en paralelo despues de revisar alcance.

### Chat 4 - Seguridad y despliegue interno

Proyectos: `capitalpos-api` y `capitalpos-cpe-api`.

Objetivo:

- Revisar CORS.
- Proteger exposicion de CPE API.
- Propagar correlation id.
- Revisar API key, secretos y configuracion por ambiente.

Dependencias:

- Puede avanzar despues de estabilizar el contrato CPE o en paralelo si no toca DTOs.

### Chat 5 - Calidad CPE API

Proyecto principal: `capitalpos-cpe-api`.

Objetivo:

- Crear estrategia de tests.
- Cubrir validacion, XML, firma, SUNAT simulado, CDR y errores.
- Evaluar modularizacion interna.
- Planificar actualizacion de .NET.

Dependencias:

- Debe respetar el contrato definido en Chat 1.

### Chat 6 - Configuracion fiscal por empresa

Proyectos: `capitalpos-api` y `capitalpos-web`.

Objetivo:

- Reemplazar emisor temporal del frontend.
- Modelar configuracion fiscal/emisor por empresa.
- Exponer endpoint seguro para datos de emision.

Dependencias:

- Debe esperar la definicion multiempresa de Chat 3.

## Decision arquitectonica recomendada

Mantener la arquitectura actual de tres proyectos:

- `capitalpos-web` como cliente.
- `capitalpos-api` como API principal, gateway, seguridad, multiempresa y negocio POS.
- `capitalpos-cpe-api` como servicio interno especializado en facturacion electronica.

No convertir CPE en modulo interno por ahora. La independencia de CPE aporta aislamiento, escalabilidad y menor acoplamiento con SUNAT, siempre que se controle el contrato y el despliegue interno.
