# CapitalPOS - Tasks

Tareas derivadas de `PLANNING.md`. Todas las tareas inician en estado `Pendiente`.

Prioridades:

- `Alta`: necesaria antes de crecer o para corregir riesgos actuales.
- `Media`: importante para robustez, mantenibilidad o despliegue.
- `Baja`: mejora tecnica o preparacion posterior.

## 1. Backend principal `capitalpos-api`

### API-001 - Definir contrato publico canonico de emision CPE

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: existe un contrato documentado para `/api/cpe/emitir` con campos, estados, errores y politica HTTP esperada.
- Evidencia: `capitalpos-api/Docs/ContratoCpeEmision.md` documenta request, response publico, estados, errores, politica HTTP y datos sensibles prohibidos.

### API-002 - Alinear DTO publico de emision CPE

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: la respuesta publica de `capitalpos-api` coincide con el contrato canonico y con el modelo que consumira Angular.
- Evidencia: DTO publico probado contra `capitalpos-api/Docs/ContratoCpeEmision.md`; normalizador validado con respuestas CPE que conservan `data.estado`; `dotnet test CapitalPos.Api.sln` paso con 312 pruebas.

### API-003 - Definir politica HTTP para CPE

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: rechazos funcionales CPE y errores tecnicos tienen codigos HTTP claramente diferenciados y probados.
- Evidencia: politica documentada en `capitalpos-api/Docs/ContratoCpeEmision.md` y cubierta por pruebas de normalizacion/end-to-end.

### API-004 - Normalizar errores CPE de forma estable

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: errores de `capitalpos-cpe-api` se traducen a mensajes seguros, consistentes y sin datos sensibles.
- Evidencia: normalizador y pruebas evitan cuerpo crudo, `X-API-KEY`, rutas internas y datos sensibles.

### API-005 - Agregar pruebas contractuales para `/api/cpe/emitir`

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: las pruebas cubren `SIMULADO`, `ACEPTADO`, `RECHAZADO`, `ERROR_VALIDACION`, `ERROR_SUNAT` y error tecnico.
- Evidencia: pruebas en `capitalpos-api/tests/CapitalPos.Tests` cubren normalizacion, contrato documentado y escenarios end-to-end con error tecnico.

### API-006 - Propagar correlation id hacia CPE API

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: las solicitudes desde `capitalpos-api` hacia `capitalpos-cpe-api` incluyen un correlation id rastreable en logs.

### API-007 - Agregar resiliencia al gateway CPE

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: `ICpeGateway` tiene timeout explicito y estrategia definida para retry controlado o circuit breaker.

### API-008 - Definir regla obligatoria de `EmpresaId` para entidades POS

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: existe una convencion tecnica documentada para que toda entidad POS transaccional incluya `EmpresaId`.
- Evidencia: `capitalpos-api/Docs/Multiempresa.md` define `EmpresaId` obligatorio para entidades POS; `dotnet test CapitalPos.Api.sln` paso con 316 pruebas.

### API-009 - Preparar patron de filtrado por empresa activa

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: nuevos repositorios/use cases POS tienen un patron claro para filtrar siempre por empresa activa.
- Evidencia: patron endpoint -> `EmpresaActivaEndpointFilter` -> permiso -> use case -> repository -> EF documentado; `dotnet test CapitalPos.Api.sln` paso con 318 pruebas.

### API-010 - Separar roles de plataforma y roles de empresa

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: queda definida la diferencia entre superadmin/plataforma SaaS y roles dentro de una empresa.
- Evidencia: separacion roles de plataforma vs roles de empresa documentada en `capitalpos-api/Docs/Multiempresa.md`; `dotnet test CapitalPos.Api.sln` paso con 320 pruebas.

### API-011 - Revisar endpoints globales de empresas y usuarios

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: endpoints globales quedan restringidos a roles de plataforma o ajustados al alcance de empresa activa.
- Evidencia: endpoints de empresas, usuarios y usuarios-empresas clasificados como plataforma, empresa activa o mixtos; `dotnet test CapitalPos.Api.sln` paso con 323 pruebas.

### API-012 - Agregar pruebas anti-fuga multiempresa

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: una prueba demuestra que un usuario de empresa A no puede leer ni operar datos de empresa B.
- Evidencia: pruebas anti-fuga agregadas para `EmpresaActivaEndpointFilter`, `PermisoEmpresaEndpointFilter`, use cases/repositorios `UsuarioEmpresa` y documentacion multiempresa; `dotnet test CapitalPos.Api.sln` paso con 329 pruebas.
- Riesgo pendiente: endpoints `usuarios-empresas` por id aun requieren correccion funcional de alcance por empresa activa.

### API-013 - Exponer configuracion fiscal por empresa para CPE

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: existe un endpoint seguro para obtener datos fiscales/emisor de la empresa activa.

## 2. Backend CPE `capitalpos-cpe-api`

### CPE-001 - Documentar contrato canonico de `ApiResponse<CpeEmisionResponse>`

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: la documentacion describe request, response, estados, errores y ejemplos de emision simulada, aceptada y rechazada.
- Evidencia: `capitalpos-cpe-api/CapitalPos.Cpe/CapitalPos.Cpe.Api/Docs/ContratoApiEmitirCpe.md` documenta contrato interno, headers, request, response, estados, ejemplos y diferencias contra el contrato publico.

### CPE-002 - Alinear respuesta CPE con contrato end-to-end

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: `capitalpos-cpe-api` responde con una estructura compatible con `capitalpos-api` y Angular.
- Evidencia: `capitalpos-cpe-api` conserva `data.estado` en errores controlados 400/500; `dotnet build CapitalPos.Cpe.sln` paso correctamente, con warnings conocidos de .NET 7 fuera de soporte.

### CPE-003 - Crear proyecto de pruebas automatizadas

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: existe un proyecto de tests ejecutable para `capitalpos-cpe-api`.
- Evidencia: `CapitalPos.Cpe.Tests` (net7.0 + xUnit) referenciado en `CapitalPos.Cpe.sln`; incluye tests UBL/boleta, NC, validacion factura/boleta, firma simulada; `dotnet test CapitalPos.Cpe.Tests` paso con 52 pruebas.

### CPE-004 - Probar validacion de comprobantes

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: las pruebas cubren comprobantes validos e invalidos con errores funcionales claros.
- Evidencia: `CpeValidacionFacturaBoletaTests` (factura/boleta validas, serie incorrecta, cliente sin RUC en factura, sin items, total inconsistente, RUC emisor); `CpeValidacionNotaCreditoTests` (NC valida, serie B/F, motivo Cat.09, referencia obligatoria).

### CPE-005 - Probar generacion XML

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: las pruebas validan generacion XML correcta y manejo de errores de XML invalido.
- Evidencia: `CpeXmlUblGeneratorTests` (Invoice boleta UBL/orden SUNAT + `CpeXmlValidatorService` errores estructurales); `CpeXmlServiceAndFacturaUblTests` (factura 01, boleta 03, modo simulado basico, XML malformado); `CpeXmlUblCreditNoteGeneratorTests` (CreditNote UBL 07).

### CPE-006 - Probar firma simulada

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: las pruebas verifican respuesta exitosa y fallida del flujo de firma simulada.
- Evidencia: `CpeFirmaServiceTests` cubre `SimularFirma=true` exitoso, nombre XML vacio fallido, firma real sin certificado fallida y XML inexistente fallido sin requerir PFX real.

### CPE-007 - Probar envio SUNAT simulado

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: una emision con SUNAT simulado devuelve `SIMULADO` y mensaje funcional aceptado.
- Evidencia: verificacion funcional local de `POST /api/cpe/emitir` con boleta `tipoComprobante=03`, serie `B001`, DNI de 8 digitos y totales coherentes devolvio `200 OK`, `data.estado=SIMULADO`, `errores=[]`, y genero XML/ZIP/CDR en `/Users/yhortcruz/Documents/Dev/capitalpos-cpe-files/BETA`; `dotnet build CapitalPos.Cpe/CapitalPos.Cpe.sln` paso con warnings conocidos de .NET 7 fuera de soporte.

### CPE-008 - Probar generacion y lectura de CDR

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: las pruebas cubren guardado de CDR simulado y lectura/interpretacion basica.

### CPE-009 - Endurecer API key

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: API key tiene comparacion segura, configuracion por ambiente y procedimiento de rotacion documentado.

### CPE-010 - Restringir Swagger y endpoints internos

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: Swagger y endpoints diagnosticos no quedan expuestos fuera de desarrollo o red privada.

### CPE-011 - Evaluar modularizacion interna

- Prioridad: Baja
- Estado: Pendiente
- Criterio de aceptacion: existe una propuesta para separar dominio, aplicacion, infraestructura y API si CPE continua creciendo.

### CPE-012 - Planificar actualizacion de .NET

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: existe una ruta definida para alinear la version .NET de CPE con la version objetivo del ecosistema.

## 3. Frontend Angular `capitalpos-web`

### WEB-001 - Alinear modelos TypeScript CPE

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: los modelos TypeScript representan exactamente la respuesta publica de `capitalpos-api`.
- Evidencia: modelos CPE, errores estructurados y nullability alineados con el contrato publico en commit `88647ce`; `npm test -- --watch=false` paso con 38 pruebas.

### WEB-002 - Corregir clasificacion de emision CPE

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: `SIMULADO` y `ACEPTADO` se muestran como exito; `RECHAZADO` se muestra como rechazo funcional.
- Evidencia: clasificacion visual cubierta por pruebas en commit `88647ce`; `SIMULADO` y `ACEPTADO` van a exito, `RECHAZADO` a rechazo y errores tecnicos no se clasifican como exito.

### WEB-003 - Evitar `error inesperado` para rechazos funcionales

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: errores funcionales CPE muestran mensaje especifico y no caen en estado visual generico.
- Evidencia: `HttpErrorResponse` con cuerpo normalizado usa `data.estado`, mensaje y errores del contrato en commit `88647ce`; `RECHAZADO` y `ERROR_VALIDACION` ya no caen en fallback generico.

### WEB-004 - Leer errores desde respuestas HTTP de error

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: Angular extrae `mensaje`, `errores` y `data` desde respuestas HTTP 4xx/5xx cuando existan.
- Evidencia: `resolverErrorHttpEmisionCpe` reconoce la envoltura publica en `HttpErrorResponse.error`, llena respuesta/mensaje/errores y conserva fallback generico si no hay cuerpo normalizado; cubierto por pruebas en commit `88647ce`.

### WEB-005 - Separar diagnosticos visuales CPE

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: la pantalla distingue conectividad CPE, empresa activa, sesion y permiso de emision.

### WEB-006 - Reemplazar emisor temporal

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: el request CPE usa datos fiscales de la empresa activa y no `CPE_EMISOR_TEMPORAL_CONFIG`.

### WEB-007 - Agregar tests de clasificacion CPE

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: las pruebas cubren respuestas `SIMULADO`, `ACEPTADO`, `RECHAZADO`, `ERROR_VALIDACION` y error tecnico.
- Evidencia: `emitir-cpe-page.component.spec.ts` cubre clasificacion canonica, errores HTTP normalizados y fallback sin cuerpo normalizado; `npm test -- --watch=false` paso con 38 pruebas en commit `88647ce`.

### WEB-008 - Revisar almacenamiento de JWT

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: existe una decision documentada sobre mantener `localStorage` con mitigaciones o migrar a cookies HttpOnly.

### WEB-009 - Mejorar mensajes de permisos y empresa activa

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: cuando falta empresa activa o permiso, el usuario ve un mensaje claro y accionable.

## 4. Deploy / Infraestructura

### INFRA-001 - Definir topologia de despliegue interno

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: queda documentado que `capitalpos-web` solo consume `capitalpos-api` y que `capitalpos-cpe-api` queda en red privada.
- Evidencia: `capitalpos-api/Docs/Topologia.md` define aislamiento web→api y CPE en red privada sin CORS publico; `Docs/Despliegue.md` referencia la topologia; `capitalpos-cpe-api` `appsettings.json` declara `DeploymentTopology.InternalOnly=true`, `AllowedCaller=capitalpos-api` y `ExposePublicCors=false`.

### INFRA-002 - Configurar CORS en `capitalpos-api`

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: `capitalpos-api` permite solo origenes frontend autorizados y `capitalpos-cpe-api` no expone CORS publico.
- Evidencia: `Program.cs` registra politica `CapitalPosWeb` con `Cors:AllowedOrigins` / `WithOrigins` (sin `AllowAnyOrigin`); lista vacia deniega origenes browser; Development permite `http://localhost:4200`; variables `Cors__AllowedOrigins__N` documentadas; CPE no agrega CORS publico.

### INFRA-003 - Gestionar secretos por ambiente

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: JWT signing key, connection string, CPE API key, certificados y credenciales SUNAT se configuran fuera del repositorio.
- Evidencia: `Docs/Secretos.md` exige user-secrets/env para JWT, connection string, `CpeApi__ApiKey`, PFX (`PasswordCertificado`), SOL y `CpeSecuritySettings__ApiKey`; appsettings versionados mantienen secretos vacios; no se versionan `.pfx` ni credenciales reales.

### INFRA-004 - Definir rotacion de `CpeApi__ApiKey`

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: existe procedimiento para rotar la API key sin exponerla ni romper despliegues.

### INFRA-005 - Propagar y registrar correlation id end-to-end

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: una solicitud puede rastrearse desde frontend/API principal hasta CPE API mediante el mismo identificador.

### INFRA-006 - Definir rate limiting

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: existen limites definidos para endpoints sensibles como login, emision CPE y diagnosticos.

### INFRA-007 - Documentar ejecucion local completa

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: la documentacion indica puertos, variables, user-secrets y orden de arranque de los tres proyectos.

### INFRA-008 - Definir estrategia de logs seguros

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: los logs no exponen API keys, certificados, credenciales SUNAT, tokens JWT ni cuerpos sensibles completos.

### INFRA-009 - Definir ambientes y promocion

- Prioridad: Baja
- Estado: Pendiente
- Criterio de aceptacion: existe una descripcion de ambientes local, beta/staging y produccion con sus configuraciones principales.

### INFRA-010 - Preparar checklist de produccion

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: existe un checklist previo a produccion que cubre seguridad, secretos, CORS, red privada, migraciones, backups y monitoreo.

## 5. MVP Retail Multiempresa

Estas tareas tienen prioridad sobre mejoras tecnicas no bloqueantes. El objetivo es terminar un flujo POS vendible: producto -> cliente -> venta -> comprobante CPE simulado -> resultado guardado.

### MVP-001 - Definir modelo minimo de producto retail multiempresa

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: existen entidades persistentes iniciales para producto retail con `EmpresaId`, nombre, codigo/SKU, codigo de barras opcional, precio, costo opcional, activo y relacion basica con categoria/marca si se decide incluirlas en el primer corte.
- Evidencia: `capitalpos-api/src/CapitalPos.Domain/Producto.cs` y `capitalpos-api/src/CapitalPos.Application/Productos` definen producto multiempresa minimo con pruebas de dominio y aplicacion.

### MVP-002 - Agregar variantes opcionales para ropa retail

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: el modelo permite vender productos con o sin variantes; para ropa una variante puede representar talla, color, SKU/codigo de barras y stock propio.
- Evidencia: `capitalpos-api/src/CapitalPos.Domain/ProductoVariante.cs` y casos de aplicacion de variantes permiten variantes opcionales por empresa y producto, con talla/color/codigos y stock no negativo.

### MVP-003 - Crear migracion EF Core para productos y variantes

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: la base PostgreSQL queda preparada con tablas, claves, `EmpresaId`, indices por empresa y restricciones unicas compuestas donde aplique.
- Evidencia: migracion EF `AgregarProductosYVariantes` crea `productos` y `productos_variantes` con FK a empresa, FK compuesta variante-producto por empresa, indices por `EmpresaId` y unicos filtrados para SKU/codigo de barras; `dotnet test CapitalPos.Api.sln` paso con 361 pruebas.

### MVP-004 - Endpoints minimos de productos

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: se puede crear, listar, obtener y activar/desactivar productos filtrados por empresa activa, sin fuga entre empresas.
- Evidencia: `capitalpos-api` expone `/api/productos` con crear, listar, obtener, activar y desactivar usando `EmpresaActivaEndpointFilter`, `PermisoEmpresa.OperarAlmacen`, use cases con `IEmpresaActivaContext` y repositorio filtrado por `EmpresaId`; `dotnet test CapitalPos.Api.sln` paso con 369 pruebas.

### MVP-005 - Modelo y endpoints minimos de clientes

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: se puede registrar y listar clientes por empresa activa con tipo/numero de documento y razon social/nombre.
- Evidencia: `capitalpos-api` define entidad `Cliente`, repositorio/use cases por empresa activa, migracion `AgregarClientes` y endpoints `/api/clientes` para crear/listar/obtener con `EmpresaActivaEndpointFilter` y `PermisoEmpresa.OperarVentas`; `dotnet test CapitalPos.Api.sln` paso con 387 pruebas.

### MVP-006 - Modelo de venta y detalle de venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: existe venta persistida con `EmpresaId`, cliente opcional, fecha, totales, estado y detalles con producto/variante, cantidad, precio, IGV y total.
- Evidencia: `capitalpos-api` define `Venta`, `VentaDetalle`, `EstadoVenta`, repositorio EF y migracion `AgregarVentas` con `EmpresaId`, cliente opcional, totales, estado y detalles relacionados por claves compuestas multiempresa; `dotnet test CapitalPos.Api.sln` paso con 401 pruebas.

### MVP-007 - Crear venta desde API principal

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: endpoint de crear venta valida empresa activa, calcula o valida totales, persiste venta/detalle y no permite operar productos de otra empresa.
- Evidencia: `capitalpos-api` expone `POST /api/ventas` con `EmpresaActivaEndpointFilter` y `PermisoEmpresa.OperarVentas`; `CrearVentaUseCase` calcula totales desde detalles, persiste venta y valida cliente/producto/variante por empresa activa; `dotnet test CapitalPos.Api.sln` paso con 411 pruebas.

### MVP-008 - Emitir boleta/factura desde venta usando CPE API

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: desde una venta persistida se construye `EmitirCpeRequest`, se llama a `capitalpos-cpe-api` y se recibe resultado `SIMULADO` en desarrollo.
- Evidencia: `capitalpos-api` expone `POST /api/ventas/{id}/emitir-cpe`, carga la venta por empresa activa, construye payload CPE desde venta/detalles, llama `ICpeGateway` y normaliza la respuesta publica; `dotnet test CapitalPos.Api.sln` paso con 413 pruebas.

### MVP-009 - Persistir comprobante y resultado CPE

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: el comprobante queda guardado por empresa y venta con tipo, serie, correlativo, estado CPE, mensaje, hash, nombreXml, nombreZip y nombreCdr.
- Evidencia: `capitalpos-api` define `Comprobante`, repositorio/use case de registro, migracion `AgregarComprobantes` y persistencia del resultado normalizado en `POST /api/ventas/{id}/emitir-cpe`; `dotnet test CapitalPos.Api.sln` paso con 422 pruebas.

### MVP-010 - Pantalla Angular POS minima

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: el usuario puede seleccionar/agregar cliente, buscar/agregar productos, ver totales y registrar una venta desde Angular.
- Evidencia: `/app/ventas` reemplaza el placeholder por POS minimo; carga productos/clientes desde `capitalpos-api`, permite buscar/agregar productos, crear cliente rapido, calcular subtotal/IGV/total y registrar venta con `POST /api/ventas/`; `npm test -- --watch=false` paso con 42 pruebas.

### MVP-011 - Emitir comprobante desde Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: desde la pantalla POS se puede emitir boleta/factura de una venta y mostrar estado `SIMULADO`, `ACEPTADO`, `RECHAZADO` o error controlado.
- Evidencia: `/app/ventas` permite emitir comprobante desde la ultima venta registrada con `POST /api/ventas/{id}/emitir-cpe`, reutiliza el contrato CPE publico y muestra mensaje, estado, comprobante, XML, ZIP y CDR; `npm test -- --watch=false` paso con 44 pruebas.

### MVP-012 - Validacion funcional end-to-end del MVP

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: ecosistema completo
- Criterio de aceptacion: flujo completo validado: login -> empresa activa -> producto -> cliente -> venta -> emision CPE simulada -> comprobante guardado -> resultado visible en Angular.
- Evidencia: validacion funcional end-to-end ejecutada desde Angular con login de usuario demo, empresa activa, producto gravado, cliente DNI, venta y emision CPE simulada; Angular mostro `Estado CPE: SIMULADO` como exito con comprobante, XML, ZIP y CDR; `capitalpos-api` persistio el comprobante de la venta con `EstadoCpe=SIMULADO`; `capitalpos-cpe-api` genero XML, ZIP, CDR e historial en `capitalpos-cpe-files/BETA`; los servicios/puertos usados fueron detenidos al final; no hubo cambios de codigo durante la validacion.

## 6. Backlog posterior al MVP

Estas tareas no deben bloquear el primer sistema vendible.

### BACKLOG-001 - SUNAT beta real

- Prioridad: Media
- Estado: Pendiente
- Criterio de aceptacion: con certificado y credenciales beta se envia boleta/factura real a SUNAT beta y se procesa CDR real.

### BACKLOG-002 - Fabricacion textil

- Prioridad: Baja
- Estado: Pendiente
- Criterio de aceptacion: se define alcance separado para produccion, insumos, mermas, costos industriales y ordenes de fabricacion.

### BACKLOG-003 - Inventario avanzado y multiples almacenes

- Prioridad: Baja
- Estado: Pendiente
- Criterio de aceptacion: se disena kardex, movimientos, transferencias y stock por almacen despues del MVP retail.

### BACKLOG-004 - Superadmin SaaS avanzado

- Prioridad: Baja
- Estado: Pendiente
- Criterio de aceptacion: se implementan roles/permisos de plataforma separados cuando el MVP comercial ya este operativo.

## 7. Estabilizacion MVP para demo/cliente

### STAB-001 - Documentar ejecucion local del MVP completo

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: existe una guia operativa para levantar el MVP completo localmente sin depender de memoria o pasos sueltos.
- Evidencia: `RUNBOOK_MVP.md` creado con arquitectura local, requisitos, variables, orden de arranque, comandos, validacion manual y troubleshooting.

### STAB-002 - Validacion limpia desde cero usando `RUNBOOK_MVP.md`

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: el MVP se puede levantar y validar desde cero siguiendo el runbook.
- Evidencia: runbook validado desde cero con base limpia, migraciones, servicios, Angular, venta, CPE `SIMULADO`, comprobante persistido y XML/ZIP/CDR generados.

### STAB-003 - Preparar checklist de demo/cliente y limitaciones del MVP

- Prioridad: Alta
- Estado: Completado
- Criterio de aceptacion: existe una guia de demo que permite presentar el MVP sin improvisar y con limitaciones claras.
- Evidencia: `DEMO_MVP.md` creado con objetivo, alcance, guion de demo, datos demo, riesgos, mensaje comercial y siguientes etapas.

## 8. Mejoras UX para demo

### UX-001 - Crear productos desde Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: desde Angular se puede crear un producto basico, verlo en la lista y usarlo luego en una venta sin recurrir a Postman/curl/API manual.
- Evidencia: `/app/productos` lista productos, crea producto basico, permite busqueda y muestra errores; `npm test -- --watch=false` paso con 48 pruebas.

### UX-002 - Pulir flujo POS de venta para demo

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: una demo de venta es entendible sin explicacion tecnica: el usuario sabe si faltan productos, puede refrescar datos, registra venta una sola vez, ve subtotal/IGV/total y luego emite comprobante claramente.
- Evidencia: `/app/ventas` muestra CTA a productos cuando no hay catalogo, refresco claro de datos, resumen subtotal/IGV/total, bloqueo contra doble registro, venta registrada lista para emitir y mejores mensajes; `npm test -- --watch=false` paso con 50 pruebas.

## 9. Configuracion fiscal por empresa

### CONF-001 - Modelar configuracion fiscal por empresa

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` persiste configuracion fiscal por empresa activa con dominio, application, EF, repositorio, migracion y pruebas.
- Evidencia: `capitalpos-api` tiene `ConfiguracionFiscalEmpresa` con dominio, application, repositorio EF, migracion, DI y pruebas; relacion 1:1 con `Empresa` usando `EmpresaId` como PK/FK; `dotnet test CapitalPos.Api.sln` paso con 445 pruebas.

### CONF-002 - Usar configuracion fiscal real en emision CPE desde venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `EmitirCpeDesdeVentaUseCase` usa la configuracion fiscal activa para construir el emisor CPE y deja de depender de valores temporales hardcodeados.
- Evidencia: `EmitirCpeDesdeVentaUseCase` usa configuracion fiscal activa para emisor CPE, elimino valores temporales hardcodeados, valida configuracion inexistente/inactiva y `rucEmisor` distinto; `dotnet test CapitalPos.Api.sln` paso con 448 pruebas.

### CONF-003A - Exponer endpoints de configuracion fiscal por empresa activa

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` permite consultar y guardar la configuracion fiscal de la empresa activa mediante endpoints seguros.
- Evidencia: `capitalpos-api` expone `GET /api/configuracion-fiscal` y `PUT /api/configuracion-fiscal`; ambos requieren JWT, `X-CapitalPos-EmpresaId`, `EmpresaActivaEndpointFilter` y `PermisoEmpresa.GestionarEmpresas`; `PUT` guarda para la empresa activa e ignora `EmpresaId` libre; `dotnet test CapitalPos.Api.sln` paso con 473 pruebas.

### CONF-003B - Pantalla Angular para configurar datos fiscales de empresa

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: Angular permite cargar, crear y actualizar la configuracion fiscal de la empresa activa desde una pantalla operativa.
- Evidencia: `/app/configuracion` permite cargar, crear y actualizar configuracion fiscal; maneja `404` como configuracion pendiente; valida RUC, ubigeo y campos obligatorios; `npm test -- --watch=false` paso con 57 pruebas.

## 10. SUNAT beta real

### SUNAT-001 - Diagnostico de configuracion SUNAT beta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-cpe-api`
- Criterio de aceptacion: `capitalpos-cpe-api` puede diagnosticar si esta en simulacion o si tiene configuracion suficiente para intentar SUNAT beta, sin exponer secretos.
- Evidencia: `capitalpos-cpe-api` mejoro `GET /api/cpe/diagnostico` para validar modo, simulacion de firma/envio, certificado, password, usuario SOL, clave SOL, URL SUNAT, ruta de archivos, API key y CDR; no revela secretos; `DiagnosticoSunatBeta.md` documenta interpretacion; `dotnet build CapitalPos.Cpe/CapitalPos.Cpe.sln` paso con 0 errores y 2 warnings conocidos por net7.0 fuera de soporte.

### SUNAT-002 - Validar firma real con certificado PFX sin enviar a SUNAT

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-cpe-api`
- Criterio de aceptacion: `capitalpos-cpe-api` firma XML con certificado PFX real manteniendo `SimularEnvioSunat=true`, sin enviar comprobantes reales a SUNAT ni exponer secretos.
- Evidencia: `capitalpos-cpe-api` firmo una boleta con `SimularFirma=false` y `SimularEnvioSunat=true`; certificado PFX encontrado fuera del repo y protegido por `.gitignore`; diagnostico confirmo certificado y password configurados sin revelar secretos; XML firmado contiene `Signature`, `SignatureValue` y `X509Certificate`; emision termino con `data.estado=SIMULADO` y `xmlFirmado=true`; se generaron XML, ZIP y CDR simulado; se confirmo explicitamente que NO se envio a SUNAT; `dotnet build CapitalPos.Cpe/CapitalPos.Cpe.sln` paso con 0 errores y 2 warnings conocidos por net7.0.

### SUNAT-003 - Enviar comprobante a SUNAT beta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-cpe-api`
- Criterio de aceptacion: se intenta envio real a SUNAT beta y se obtiene una respuesta controlada sin exponer secretos.
- Evidencia: diagnostico previo OK con `Modo=BETA`, `SimularFirma=false` y `SimularEnvioSunat=false`; certificado, password, Usuario SOL, Clave SOL, URL beta y API key configurados sin exponer secretos; se genero XML firmado y ZIP; se realizo llamada real a SUNAT beta, no produccion; SUNAT beta respondio de forma controlada con `ERROR_SUNAT` fault `0306`; motivo: XML UBL no parseable por orden/estructura, esperaba `AccountingSupplierParty` y encontro `InvoiceTypeCode`; no se genero CDR porque SUNAT rechazo antes de CDR; `dotnet build CapitalPos.Cpe/CapitalPos.Cpe.sln` paso con 0 errores y 2 warnings conocidos.

### SUNAT-004 - Corregir estructura UBL del XML para SUNAT beta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-cpe-api`
- Criterio de aceptacion: `capitalpos-cpe-api` genera XML UBL con estructura y orden aceptado por SUNAT beta para boleta/factura, corrige el fault `0306` y permite reintentar envio beta.
- Evidencia: se corrigio iterativamente la estructura UBL del XML en `capitalpos-cpe-api`; se superaron los faults de `AccountingSupplierParty`/`InvoiceTypeCode`, `Note`/`LegalMonetaryTotal`, `TaxScheme`/`TaxCategory`, `InvoiceTypeCode` y `DocumentCurrencyCode`; `dotnet test CapitalPos.Cpe.sln` paso con 20/20 tests; `dotnet build CapitalPos.Cpe.sln` paso con 0 errores y 2 warnings conocidos por net7.0; el reintento SUNAT beta con boleta `B001-300009` respondio HTTP 200; `data.estado = ACEPTADO`; mensaje SUNAT: "La Boleta numero B001-300009, ha sido aceptada"; XML firmado generado; ZIP enviado; CDR real generado: `R-20606264004-03-B001-300009.zip`; se confirmo que fue SUNAT beta, no produccion.

### SUNAT-005 - Validar emision SUNAT beta desde venta MVP

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: una venta MVP emitida desde `capitalpos-api` llega a `capitalpos-cpe-api`, se envia a SUNAT beta y queda persistida como comprobante aceptado con CDR real.
- Evidencia: se valido el flujo completo `capitalpos-api` -> `capitalpos-cpe-api` -> SUNAT beta desde una venta MVP; no se modifico codigo durante la validacion; `capitalpos-cpe-api` corrio en BETA con `SimularFirma=false` y `SimularEnvioSunat=false`; `capitalpos-api` apunto a `capitalpos-cpe-api`; se configuro empresa activa con RUC emisor `20606264004`; se creo producto, cliente DNI y venta; boleta emitida: `03-B001-300011`; `POST /api/ventas/{id}/emitir-cpe` respondio HTTP 200; Estado CPE: `ACEPTADO`; mensaje SUNAT: "La Boleta numero B001-300011, ha sido aceptada"; comprobante persistido en `capitalpos-api` con estado `ACEPTADO`; XML, ZIP, CDR e historial generados; CDR verificado con `ResponseCode=0`; servicios detenidos al final; observacion: un primer intento con correlativo `300010` quedo en `ERROR_VALIDACION` por fecha UTC futura para Lima; la validacion final uso fecha correcta y fue aceptada.

### SUNAT-006 - Normalizar fecha de emision CPE a zona Lima

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` envia `fechaEmision` CPE en zona Peru/Lima para evitar rechazos por fecha futura cuando el servidor opera en UTC.
- Evidencia: `EmitirCpeDesdeVentaUseCase` convierte `venta.Fecha` a zona Peru/Lima antes de enviar `fechaEmision` al CPE; usa `TimeZoneInfo` con `America/Lima` y fallback `SA Pacific Standard Time`; serializa `fechaEmision` como `DateTimeKind.Unspecified` para evitar enviar `Z` UTC; cubre el caso en que UTC ya es dia siguiente pero Lima sigue en el dia anterior; evita fecha futura ante SUNAT por desfase UTC/Lima; `dotnet test CapitalPos.Api.sln` paso con 475 pruebas.

## 11. Commits de estabilizacion

### COMMIT-CPE-001 - Commit de UBL SUNAT beta aceptado

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-cpe-api`
- Criterio de aceptacion: los cambios de `capitalpos-cpe-api` para diagnostico, XML UBL aceptado por SUNAT beta, pruebas y documentacion quedan versionados sin secretos ni artefactos generados.
- Evidencia: repo `capitalpos-cpe-api`; hash `88aef5382d1d1f9ec99b3f8a256281a33e1868d5`; mensaje `fix(cpe): align UBL invoice for SUNAT beta`; incluye diagnostico SUNAT beta, correcciones UBL, pruebas `CapitalPos.Cpe.Tests` y documentacion `DiagnosticoSunatBeta.md`; `dotnet test CapitalPos.Cpe.sln` paso con 20/20; `dotnet build CapitalPos.Cpe.sln` paso con 0 errores y 2 warnings conocidos.

### COMMIT-API-001 - Commit de configuracion fiscal y emision CPE desde venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: los cambios de configuracion fiscal por empresa y emision CPE desde venta quedan versionados con pruebas verdes y sin secretos.
- Evidencia: repo `capitalpos-api`; hash `4e064c4beefba137803e8ac960d8444c2b31b246`; incluye `ConfiguracionFiscalEmpresa`, endpoints `GET/PUT /api/configuracion-fiscal`, emision desde venta usando configuracion fiscal real, migracion, DI y pruebas; `dotnet test CapitalPos.Api.sln` paso con 473 pruebas.

### COMMIT-WEB-001 - Commit de configuracion fiscal Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: los cambios Angular de configuracion fiscal quedan versionados con pruebas verdes y sin incluir cambios ajenos de lockfile.
- Evidencia: repo `capitalpos-web`; hash `aab8ff63f4a46aab3a9b7314944ecb797c1667b9`; mensaje `feat(web): add MVP product and fiscal configuration flows`; incluye `/app/configuracion`, servicio, modelo y pruebas; `npm test` paso con 57 pruebas; `package-lock.json` quedo fuera del commit.

### COMMIT-WEB-002 - Commit de fecha de venta por defecto en zona Lima

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: la correccion de fecha por defecto de ventas queda versionada con pruebas verdes y sin incluir cambios ajenos de lockfile.
- Evidencia: repo `capitalpos-web`; hash `1e78687416817516e92307acf10d41a5181cb438`; incluye `ventas-page.component.ts` y `ventas-page.component.spec.ts`; corrige `/app/ventas` para usar fecha por defecto en zona Lima mediante `obtenerFechaActualLima()`; `npm test` paso con 59 pruebas; `package-lock.json` quedo fuera porque no estaba relacionado; no se incluyeron secretos ni artefactos generados.

## 12. Demo cliente SUNAT beta

### DEMO-001 - Validar demo completa desde Angular contra SUNAT beta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: desde Angular se completa el flujo demo `capitalpos-web` -> `capitalpos-api` -> `capitalpos-cpe-api` -> SUNAT beta, con comprobante aceptado, CDR real y resultado visible en UI.
- Evidencia: se valido `capitalpos-web` -> `capitalpos-api` -> `capitalpos-cpe-api` -> SUNAT beta; Angular mostro estado visual `exito` y Estado CPE `ACEPTADO`; boleta aceptada: `B001-319081`; comprobante: `20606264004-03-B001-319081`; venta: `c0c531e7-e2b1-434a-9eae-3ed17e34625c`; XML, ZIP, CDR e historial generados; comprobante persistido en `capitalpos-api` con `EstadoCpe` `ACEPTADO`; CDR verificado con `ResponseCode=0`; servicios detenidos y puertos `4200`/`5096`/`5097` libres; observacion: primer intento `B001-319080` fallo por fecha futura en UI, corregido despues en `DEMO-002`.

### DEMO-002 - Corregir fecha de venta por defecto a zona Lima

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` inicializa la fecha de venta con la fecha actual de Peru/Lima, evitando sugerir una fecha futura ante SUNAT cuando UTC ya cambio de dia pero Lima no.
- Evidencia: `capitalpos-web` reemplazo `new Date().toISOString().slice(0, 10)` por `obtenerFechaActualLima()`; usa `Intl.DateTimeFormat` con `timeZone` `America/Lima`; evita que `/app/ventas` sugiera una fecha futura cuando UTC ya cambio de dia pero Lima no; pruebas cubren UTC `2026-07-13` con Lima `2026-07-12`; `npm test` paso con 59 pruebas.

### DEMO-003 - Revalidar demo Angular SUNAT beta sin corregir fecha manualmente

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: desde Angular se registra venta y se emite boleta aceptada por SUNAT beta sin modificar manualmente el campo `Fecha`.
- Evidencia: se valido desde Angular el flujo completo `capitalpos-web` -> `capitalpos-api` -> `capitalpos-cpe-api` -> SUNAT beta; no se modifico manualmente el campo `Fecha`; `/app/ventas` mostro fecha por defecto `2026-07-12`, correcta para Lima; pruebas previas verdes: `capitalpos-api` 475 passed, `capitalpos-cpe-api` 20 passed, `capitalpos-web` 59 passed; servicios usados: `capitalpos-cpe-api` en `http://127.0.0.1:5097` con `Modo=BETA`, `SimularFirma=false`, `SimularEnvioSunat=false`, `capitalpos-api` en `http://127.0.0.1:5096` y `capitalpos-web` en `http://127.0.0.1:4200`; producto: `Producto DEMO003 SUNAT 1783911017`; cliente DNI: `71011018`; venta: `d7aac196-de81-4b3c-b319-0d37ab1b9a86`; boleta: `B001-341017`; UI mostro estado visual `exito` y Estado CPE `ACEPTADO`; mensaje SUNAT: "La Boleta numero B001-341017, ha sido aceptada"; comprobante persistido con `EstadoCpe` `ACEPTADO`; XML, ZIP, CDR e historial generados; CDR verificado con `ResponseCode=0`; servicios detenidos al final y puertos `4200`/`5096`/`5097` libres; no se modifico codigo ni se hicieron commits durante la validacion.

### DEMO-004 - Preparar guia final de demo SUNAT beta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: documentacion raiz
- Criterio de aceptacion: existe una guia operativa final para ejecutar la demo cliente SUNAT beta sin depender del historial del chat.
- Evidencia: se creo `/Users/yhortcruz/Documents/Dev/DEMO_SUNAT_BETA.md`; documenta estado validado end-to-end desde Angular hasta SUNAT beta; incluye arquitectura `capitalpos-web` -> `capitalpos-api` -> `capitalpos-cpe-api` -> SUNAT beta; incluye commits relevantes, pruebas verdes y ultima validacion real `DEMO-003`; incluye comandos para levantar servicios sin secretos reales; incluye checklist previo, guion de demo, datos recomendados, restricciones, limitaciones MVP, plan de recuperacion y mensaje comercial sugerido; no se modifico codigo ni se hicieron commits.

## 13. Inventario MVP

### INV-001 - Modelar stock basico por producto y empresa

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` cuenta con modelo persistente y multiempresa para stock basico por producto, con dominio, application, EF, repositorio y pruebas.
- Evidencia: `capitalpos-api` creo `StockProducto` como entidad de dominio; `StockProducto` incluye `EmpresaId` obligatorio, `ProductoId` obligatorio, `ProductoVarianteId` opcional, `CantidadDisponible`, `CantidadReservada`, `FechaCreacion` y `FechaActualizacion`; `CantidadReservada` no puede superar `CantidadDisponible`; `Descontar` solo permite descontar stock libre; se agregaron metodos `Incrementar`, `Descontar`, `Reservar` y `LiberarReserva`; se agregaron application use cases `AjustarStockProductoUseCase` y `ObtenerStockProductoUseCase`; se agrego repositorio `IStockProductoRepository` e implementacion EF; se incluyo configuracion EF, `DbSet`, DI y migracion `AgregarStockProducto`; pruebas de dominio, application, EF, DI e integracion actualizadas; `dotnet test CapitalPos.Api.sln` paso con 492 pruebas.

### INV-002 - Crear endpoints API para consultar y ajustar stock

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` expone endpoints seguros y multiempresa para consultar y ajustar stock por producto y variante, sin descontar automaticamente por venta todavia.
- Evidencia: `capitalpos-api` expone endpoints bajo `/api/stock`: `GET /api/stock/productos/{productoId}`, `GET /api/stock/productos/{productoId}/variantes/{productoVarianteId}` y `PUT /api/stock/ajustar`; los endpoints requieren JWT, `X-CapitalPos-EmpresaId`, `EmpresaActivaEndpointFilter` y `PermisoEmpresa.OperarAlmacen`; la consulta usa `ObtenerStockProductoUseCase`; el ajuste usa `AjustarStockProductoUseCase`; se valida pertenencia de producto/variante a la empresa activa antes de ajustar; la respuesta publica incluye `empresaId`, `productoId`, `productoVarianteId`, `cantidadDisponible`, `cantidadReservada`, `stockLibre` y `fechaActualizacion`; no se implemento descuento automatico por venta todavia; pruebas de endpoints, validacion, permisos y anti-fuga multiempresa agregadas; `dotnet test CapitalPos.Api.sln` paso con 512 pruebas.

### INV-004 - Descontar stock al registrar venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: al registrar una venta, `capitalpos-api` valida stock disponible por empresa activa y descuenta de forma atomica, sin registrar ventas ni descuentos parciales cuando falta stock.
- Evidencia: `CrearVentaUseCase` valida stock por empresa activa; agrupa cantidades por `ProductoId` + `ProductoVarianteId`; valida todo antes de descontar; descuenta usando `StockProducto.Descontar`; si falta stock o el stock libre es insuficiente, no registra venta ni descuenta nada; las ventas multi-detalle no dejan descuentos parciales si un detalle falla; se agrego `IUnitOfWork` y `EfUnitOfWork`; venta y stock comparten el mismo `SaveChanges` del `DbContext`; `EfStockProductoRepository.GuardarAsync` prepara cambios sin `SaveChanges` inmediato; `AjustarStockProductoUseCase` confirma explicitamente mediante `IUnitOfWork`; no se toco emision CPE; no se modifico `capitalpos-web` ni `capitalpos-cpe-api`; pruebas de dominio, application, integracion HTTP y DI actualizadas; `dotnet test CapitalPos.Api.sln` paso con 524 pruebas.

### INV-005 - Mostrar stock disponible en POS Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` muestra stock disponible por producto, evita agregar productos sin stock o cantidades mayores al stock libre, refresca stock despues de una venta exitosa y conserva el carrito ante rechazo backend por stock insuficiente.
- Evidencia: validacion funcional acotada aprobada; `/app/ventas` muestra stock como `Disp.`, `Res.` y `Libre`; producto sin stock no permite agregar; cantidad mayor al stock libre muestra error y no agrega al carrito; venta exitosa desde Angular refresca stock libre de 5 a 3; rechazo backend por stock insuficiente muestra mensaje y mantiene el carrito; Angular no descuenta stock localmente; `npm test -- --watch=false` paso con 77 pruebas.

### INV-006 - Seleccionar variante en Inventario Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/inventario` permite consultar y ajustar stock de variantes mediante selector visual, manteniendo el flujo de stock por producto base cuando no hay variantes activas.
- Evidencia: `/app/inventario` carga variantes con `ProductosApiService.listarVariantes(productoId)` al cambiar producto; si hay variantes activas, muestra selector visual; bloquea consultar stock hasta seleccionar variante; bloquea ajustar stock hasta seleccionar variante; las opciones muestran color, talla, SKU y codigo de barras si existe; se quito el flujo de escribir manualmente "ID de variante si aplica"; al consultar variante usa `StockApiService.obtenerStockProductoVariante(productoId, varianteId)`; al ajustar variante envia `productoVarianteId` en `ajustarStock`; despues de ajustar stock, refresca consultando nuevamente al backend; si no hay variantes activas, mantiene stock por producto base con `productoVarianteId` null; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 111 pruebas.

### INV-007 - Mejorar UX de seleccion de producto/variante en Inventario

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/inventario` permite elegir producto y variante con textos legibles, consulta automaticamente el stock de la variante seleccionada y bloquea ajustes incompletos o invalidos.
- Evidencia: producto se elige desde un select con texto legible; ya no se muestra GUID como valor principal del producto; se mantiene internamente `productoId`; al seleccionar producto carga variantes con `ProductosApiService.listarVariantes(productoId)`; si hay variantes activas, muestra selector visual; muestra mensaje "Selecciona una variante para consultar o ajustar stock."; al seleccionar variante consulta automaticamente stock con `StockApiService.obtenerStockProductoVariante`; tarjeta `Stock disponible` muestra contexto de producto y variante seleccionada; `Ajustar stock` queda deshabilitado si falta producto, falta variante requerida o cantidad invalida; producto sin variantes mantiene flujo de stock base; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 114 pruebas.

### INV-008 - Formatear fecha de actualizacion de stock

- Prioridad: Media
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/inventario` muestra `fechaActualizacion` en formato legible para Peru/Lima y maneja valores nulos o invalidos sin mostrar errores visuales.
- Evidencia: `fechaActualizacion` se formatea con `Intl.DateTimeFormat('es-PE')`; se usa `timeZone: 'America/Lima'`; la fecha se muestra como `16/07/2026 12:40 p. m.`; ya no se muestra ISO crudo; fecha null, vacia o invalida muestra "Sin fecha"; no aparece "Invalid Date"; consulta por variante sigue funcionando; consulta por producto base sigue funcionando; ajuste/refresco de stock sigue funcionando; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 121 pruebas.

### INV-WEB-002 - Corregir contexto de variante en tarjeta de stock

- Prioridad: Media
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/inventario` muestra siempre el producto/variante que realmente produjo el stock consultado o ajustado, sin conservar visualmente contexto anterior ante cambios rapidos.
- Evidencia: se inspecciono el estado real del repo antes de modificar; `capitalpos-web` venia solo con `package-lock.json` modificado como cambio previo; `/app/inventario` mostraba la tarjeta con `productoSeleccionado()` y `varianteSeleccionada()` del formulario, no con el contexto que realmente produjo el stock consultado; si una consulta anterior respondia tarde, podia pisar visualmente el stock/contexto de una variante recien seleccionada; se separo seleccion actual de contexto consultado mediante `stockContexto`; al cambiar sede/producto/variante se limpia el stock anterior; al consultar o ajustar, la tarjeta muestra el contexto de la consulta/ajuste activo; las respuestas obsoletas se ignoran con una version de consulta; la tarjeta muestra `Consultando` mientras carga; la tarjeta ya no conserva una variante anterior; producto base sin variantes sigue funcionando igual; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 217 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `3e45430`; mensaje: `Corregir contexto de variante en inventario`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

## 14. Variantes retail multi-giro

### VAR-002 - Exponer y estabilizar contrato API de variantes retail multi-giro

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` expone un contrato seguro y multiempresa para listar, crear, activar y desactivar variantes de producto, dejando listo el consumo posterior desde Angular.
- Evidencia: `GET /api/productos/{productoId}/variantes` implementado; `POST /api/productos/{productoId}/variantes` implementado; `PATCH /api/productos/{productoId}/variantes/{varianteId}/activar` implementado; `PATCH /api/productos/{productoId}/variantes/{varianteId}/desactivar` implementado; endpoints protegidos con JWT, `X-CapitalPos-EmpresaId`, `EmpresaActivaEndpointFilter` y `PermisoEmpresa.OperarAlmacen`; response incluye `id`, `empresaId`, `productoId`, `talla`, `color`, `codigoSku`, `codigoBarras`, `activo` y `fechaCreacion`; validacion de producto/variante por empresa activa; SKU y codigo de barras unicos por empresa; sin fuga multiempresa; se mantiene modelo actual `talla`/`color`/`SKU`/`codigoBarras` para evitar migracion grande; queda deuda para modelo multi-giro real de atributos/variantes; queda deuda conceptual sobre `ProductoVariante.StockActual` frente a `StockProducto`; `dotnet test CapitalPos.Api.sln` paso con 535 pruebas.

### VAR-003 - Gestionar variantes retail desde Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/productos` permite consultar, crear, activar y desactivar variantes retail usando el contrato de `capitalpos-api`, sin avanzar aun a venta por variante ni atributos dinamicos.
- Evidencia: se agregaron modelos `ProductoVarianteResponse` y `CrearProductoVarianteRequest`; `productos-api.service` consume `GET /api/productos/{productoId}/variantes`, `POST /api/productos/{productoId}/variantes`, `PATCH /api/productos/{productoId}/variantes/{varianteId}/activar` y `PATCH /api/productos/{productoId}/variantes/{varianteId}/desactivar`; `/app/productos` permite expandir producto y listar variantes; `/app/productos` permite crear variante; `/app/productos` permite activar/desactivar variante; se muestran talla, color, SKU, codigo de barras y estado; se muestran errores del backend claramente; no se implemento venta por variante todavia; no se implemento matriz talla/color ni atributos dinamicos; no hay llamadas productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 84 pruebas.

### POS-001 - Vender seleccionando variante en POS

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` permite vender productos con variantes activas seleccionando la variante correcta, enviando `productoVarianteId` al backend y respetando el stock libre de la variante sin romper la venta de productos simples.
- Evidencia: commit `capitalpos-web` `c0850c0` (`Vender seleccionando variante en POS`); validacion funcional acotada aprobada; producto con variantes obliga seleccionar variante; al seleccionar `Negro/M` mostro `Disp. 5`, `Res. 0` y `Libre 5`; cantidad mayor al `stockLibre` fue bloqueada; carrito muestra producto + color/talla/SKU/codigo de barras; `POST /api/ventas` envia `productoVarianteId`; venta valida desconto stock de variante de 5 a 3; producto base sin variante no tuvo stock descontado incorrectamente; venta con stock insuficiente fue rechazada por backend; Angular mostro mensaje y mantuvo carrito; no hubo emision CPE en esta validacion; `capitalpos-api` paso 535 pruebas; `capitalpos-web` paso 91 pruebas; `capitalpos-cpe-api` build OK.

## 15. Ventas y reportes comerciales

### VTA-001 - Capturar dimensiones comerciales minimas en Venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `Venta` captura dimensiones comerciales minimas para habilitar reportes por canal, punto de venta y vendedor sin romper compatibilidad con el POS Angular actual.
- Evidencia: `Venta` ahora tiene `CanalVenta`, `PuntoVentaId` y `VendedorId`; `CanalVenta` quedo como enum de dominio con valores `TIENDA`, `PROVINCIA`, `MARKETING`, `MAYORISTA`, `MAQUILA` y `OFERTAS`; `CanalVenta` tiene default `TIENDA` para compatibilidad con POS/Angular actual; `PuntoVentaId` quedo como `Guid?` opcional; `VendedorId` quedo como `Guid?` opcional; no se asigna vendedor automaticamente todavia, queda como deuda para usuario/contexto; `CrearVentaRequest`, `CrearVentaUseCase` y `VentaResponse` fueron actualizados; migracion `20260716030517_AgregarDimensionesComercialesVenta` creada; la migracion agrega `CanalVenta`, `PuntoVentaId` y `VendedorId` en `ventas`; `CanalVenta` usa default DB `TIENDA`; venta sin canal registra `TIENDA`; venta con `PROVINCIA` y `MARKETING` se persiste correctamente; canal invalido falla con error claro; descuento de stock sigue funcionando; no se toco emision CPE; no se modifico `capitalpos-web` ni `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 545 pruebas.

### VTA-002 - Capturar canal comercial en POS Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` permite seleccionar el canal comercial de la venta, enviarlo a `capitalpos-api` y mantener compatibilidad con venta simple, venta por variante y stock.
- Evidencia: `CanalVenta` agregado en modelos Angular; `CrearVentaRequest` soporta `canalVenta`, `puntoVentaId` y `vendedorId`; `VentaResponse` soporta esas dimensiones como opcionales/nullables; `/app/ventas` muestra selector "Canal comercial"; default Angular: `TIENDA`; canales disponibles: `TIENDA`, `PROVINCIA`, `MARKETING`, `MAYORISTA`, `MAQUILA` y `OFERTAS`; `POST /api/ventas` envia `canalVenta`; `puntoVentaId` y `vendedorId` se envian `null` por ahora; `PROVINCIA` y `MARKETING` se envian correctamente en pruebas; error backend por canal invalido se muestra y no limpia carrito; se mantiene venta sin variante; se mantiene venta con variante y `productoVarianteId`; se mantiene refresco de stock; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 96 pruebas.

### REP-002 - Pantalla Angular de reporte comercial por canal

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/reportes/ventas-por-canal` permite consultar ventas por canal en un rango de fechas, mostrando totales comerciales utiles para demo y analisis MVP.
- Evidencia: ruta `/app/reportes/ventas-por-canal` creada; modelos `ReporteVentasPorCanalResponse`, `ReporteVentasPorCanalItem` y `ReporteVentasPorCanalTotal` agregados; `ReportesApiService.obtenerVentasPorCanal(desde, hasta)` creado; consume `GET /api/reportes/ventas-por-canal?desde=YYYY-MM-DD&hasta=YYYY-MM-DD`; pantalla con filtros `Desde` y `Hasta`; fechas por defecto: primer dia del mes actual y fecha actual Lima; muestra total general; muestra tabla por canal; muestra `cantidadVentas`, `unidades`, `soles` y `precioPromedio`; soles y precio promedio se formatean como moneda PEN; incluye estados cargando, error backend, error permiso y sin datos; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 106 pruebas.

### REP-003 - Corregir navegacion del modulo Reportes

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/reportes` funciona como indice navegable de reportes y permite llegar al reporte de ventas por canal sin romper la pantalla `REP-002`.
- Evidencia: `/app/reportes` ahora muestra indice de reportes; ya no muestra solo placeholder "En construccion"; muestra titulo "Reportes"; muestra subtitulo "Reportes comerciales y operativos"; incluye tarjeta "Ventas por canal"; la tarjeta muestra metricas: cantidad ventas, unidades, soles y precio promedio; el enlace "Ver reporte" navega a `/app/reportes/ventas-por-canal`; `/app/reportes/ventas-por-canal` sigue funcionando con la pantalla `REP-002`; sidebar mantiene "Reportes" activo en `/app/reportes` y `/app/reportes/ventas-por-canal`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 118 pruebas.

### REP-004 - Grafico y resumen visual de ventas por canal

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/reportes/ventas-por-canal` muestra una distribucion visual por canal y resumen ejecutivo sin agregar librerias externas ni romper filtros/tabla existentes.
- Evidencia: se agrego seccion "Distribucion por canal"; se agregaron barras horizontales HTML/CSS por canal, sin librerias externas; cada canal muestra `canalVenta`, soles y porcentaje; porcentaje se calcula como `item.soles / totalGeneral.soles * 100`; porcentaje se redondea a 1 decimal; si `totalGeneral.soles` es 0, no divide entre cero; si no hay ventas, muestra "Aun no hay ventas para graficar en este rango."; se muestra Canal lider por soles; se muestra Canal con mayor precio promedio; mayor precio promedio considera solo canales con `unidades > 0`; se muestra Total de canales con ventas; la tabla por canal se mantiene; filtros `Desde`/`Hasta` se mantienen; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 125 pruebas.

### DASH-001 - Endpoint de dashboard comercial inicial

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` expone un dashboard comercial inicial seguro y multiempresa con ventas del dia, canal lider, productos vendidos y stock bajo para alimentar una pantalla ejecutiva.
- Evidencia: se implemento `GET /api/dashboard/comercial`; devuelve fecha y ultima actualizacion; devuelve importe total vendido del dia; devuelve cantidad de operaciones; devuelve unidades vendidas; devuelve canal lider por importe; devuelve top 5 productos o variantes vendidos; devuelve hasta 5 productos o variantes con stock bajo; considera unicamente `EstadoVenta.Registrada`; excluye `EstadoVenta.Anulada`; calcula el dia usando `America/Lima`; usa `IDashboardComercialClock` para hacer testeable la fecha; respeta `EmpresaId` mediante `IEmpresaActivaContext`; protege el endpoint con JWT; exige `X-CapitalPos-EmpresaId`; usa `EmpresaActivaEndpointFilter`; exige `PermisoEmpresa.OperarVentas`; stock bajo usa exclusivamente `StockProducto.CantidadLibre`; no usa `ProductoVariante.StockActual`; umbral provisional de stock bajo centralizado en 5; canal lider y listados tienen desempates deterministas; no hay uso productivo de `X-API-KEY`; no hay referencias a `capitalpos-cpe-api`; build correcto con 0 errores y 0 warnings; pruebas correctas con 580 passing; commit `0e6809e4035610c4e5a3b74e3ea614ef80cfb659`; mensaje: `Agregar dashboard comercial inicial`; `main` sincronizada con `origin/main`.
- Deudas tecnicas: configurar posteriormente el umbral de stock bajo por empresa, producto o variante; incorporar `AlmacenId` para filtrar por tienda o almacen; optimizar las consultas cuando el catalogo tenga gran volumen; retirar o redefinir conceptualmente `ProductoVariante.StockActual`.

### DASH-002 - Interfaz visual del dashboard comercial

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/dashboard` consume el dashboard comercial de `capitalpos-api` y muestra una vista ejecutiva con ventas de hoy, operaciones, unidades, canal lider, top productos, stock bajo y accesos rapidos.
- Evidencia: se reemplazo el dashboard estatico de `/app/dashboard`; se consume `GET /api/dashboard/comercial`; se agrego `DashboardApiService`; se agregaron modelos TypeScript alineados con el backend; se muestran ventas de hoy en PEN; se muestran operaciones; se muestran unidades vendidas; se muestra canal lider; `canalLider` null muestra "Sin ventas"; se muestra Top 5 productos; se agregaron barras horizontales HTML/CSS sin librerias externas; las barras usan `item.unidades / maxUnidades * 100`; se evita division entre cero; `NaN`, `Infinity` y valores invalidos generan 0%; el ancho se limita entre 0% y 100%; se muestra stock bajo usando directamente la coleccion del backend; Angular no recalcula el criterio de stock bajo; no se muestra ni inventa el umbral 5; se agregaron accesos rapidos a ventas, productos, inventario y reporte por canal; se manejan estados cargando, error, resultado y listas vacias; una recarga fallida conserva los datos anteriores; se usa `DestroyRef` + `takeUntilDestroyed`; fechas null o invalidas muestran "Sin fecha"; una fecha `yyyy-MM-dd` no se desplaza al dia anterior; no hay uso productivo de `X-API-KEY`; no hay referencias productivas a `capitalpos-cpe-api`; no se instalaron paquetes; build correcto; pruebas correctas con 143 passing; commit `b13d5fb`; mensaje: `Agregar dashboard comercial web`; `main` sincronizada con `origin/main`.
- Deudas tecnicas: extraer mas adelante helpers compartidos de moneda y fecha; realizar revision visual autenticada con datos reales; mantener `package-lock.json` fuera de futuros commits hasta revisar su modificacion previa.

## 16. Multisede y modelo multi-giro

Nota de ejecucion:
- No implementar todo en una sola tanda.
- Primero cerrar checkpoint: `SEDE-001` + `SEDE-002` + `INV-009`.
- No avanzar a presentaciones/caja hasta validar venta + stock + dashboard por sede.

### SEDE-001 - Modelar Sede y PuntoVenta en capitalpos-api

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` cuenta con entidades, configuracion EF, migraciones y seed demo minimo para `Sede` y `PuntoVenta`, sin tocar todavia venta, stock, caja, series ni frontend.
- Evidencia: se crearon entidades de dominio `Sede`, `PuntoVenta` y `TipoSede`; `Sede` tiene `EmpresaId` obligatorio; `PuntoVenta` tiene `EmpresaId` obligatorio; `PuntoVenta` referencia `Sede` con FK compuesta `SedeId` + `EmpresaId`; `TipoSede` soporta `TIENDA` y `ALMACEN`; `CodigoEstablecimiento` queda opcional funcionalmente como string normalizado vacio; se agregaron `ISedeRepository` e `IPuntoVentaRepository`; se agregaron repositorios EF; se agregaron configuraciones EF; se actualizaron `CapitalPosDbContext` y `DependencyInjection`; se crearon migraciones `20260718184542_AgregarSedes` y `20260718184630_AgregarPuntosVenta`; `DemoSeed` crea Sede demo tipo `TIENDA` "Tienda Demo"; `DemoSeed` crea PuntoVenta demo "Caja Principal"; se agregaron pruebas `SedeTests` y `PuntoVentaTests`; se actualizaron pruebas EF, DI, `DemoSeed` y estructura de repositorios; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 597 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`.

### SEDE-002 - Conectar Venta a Sede/PuntoVenta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `Venta` queda asociada a `SedeId` y `PuntoVentaId` reales, con FK a `PuntoVenta`, resolviendo/copiando `SedeId` desde el punto de venta al registrar ventas.
- Evidencia: `Venta` ahora exige `SedeId` y `PuntoVentaId`; `PuntoVentaId` quedo obligatorio desde backend; `SedeId` no se acepta desde frontend; `SedeId` se resuelve desde el `PuntoVenta` validado contra la empresa activa; `CrearVentaRequest` incluye `PuntoVentaId`; `CrearVentaUseCase` valida empresa activa, `PuntoVenta` existente, `PuntoVenta` de la empresa activa y `PuntoVenta` activo; `VentaResponse` incluye `SedeId` y `PuntoVentaId`; `VentaConfiguration` configura `SedeId` y `PuntoVentaId` obligatorios; migracion `20260718190852_AgregarSedeAVentaYPuntoVenta` creada; la migracion agrega `SedeId` requerido en `ventas`; convierte `PuntoVentaId` en requerido; agrega FKs compuestas contra `sedes` y `puntos_venta` usando `EmpresaId`; incluye backfill seguro para ventas existentes usando punto de venta activo por empresa; descuento de stock actual sigue funcionando; no se toco emision CPE; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 605 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `785e08d5bdec9435745303683c79be776a03ad22`; mensaje: `Conectar ventas a sede y punto de venta`; git status final limpio.

### INV-009 - StockProducto por Sede

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `StockProducto` queda asociado a `SedeId`, el stock se descuenta por sede correcta y `ProductoVariante.StockActual` deja de ser fuente de verdad.
- Evidencia: `StockProducto` ahora exige `SedeId`; stock operativo queda por `EmpresaId` + `SedeId`; `IStockProductoRepository` consulta por empresa+sede+producto+variante; endpoints `/api/stock` reciben `sedeId` explicito; `GET /api/stock/productos/{productoId}?sedeId=...`; `GET /api/stock/productos/{productoId}/variantes/{productoVarianteId}?sedeId=...`; `PUT /api/stock/ajustar` recibe `sedeId` en body; `AjustarStockProductoUseCase` valida sede activa de empresa activa; `ObtenerStockProductoUseCase` valida sede activa de empresa activa; `CrearVentaUseCase` descuenta stock usando la `SedeId` resuelta desde `PuntoVentaId`; venta no recibe `SedeId` libre; venta falla si el stock existe en otra sede pero no en la sede de la venta; `/api/stock` devuelve `sedeId` en la respuesta publica; `DemoSeed` crea producto demo y stock demo asociado a la sede demo; migracion `20260718194714_AgregarSedeAStockProducto` creada; la migracion agrega `SedeId` a `stocks_productos`; ajusta indices unicos por sede; agrega FK compuesta hacia `sedes`; incluye backfill por primera sede activa de la empresa; `ProductoVariante.StockActual` no se elimino todavia, queda como deuda pero no se usa en logica de stock operativo; dashboard stock bajo sigue usando `StockProducto.CantidadLibre`; no se toco `capitalpos-web`; no se toco `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 611 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `27f2c7bdcc6a07da8661985fa88130381e83be4e`; mensaje: `Conectar stock a sede`; git status final limpio.

### SEDE-WEB-001 - Usar sede/punto de venta en Angular POS e Inventario

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` y `/app/inventario` operan contra el backend multisede enviando `puntoVentaId` para ventas y `sedeId` para consultas/ajustes de stock, con entrada manual temporal hasta contar con endpoints de catalogo de sedes/puntos de venta.
- Evidencia: `capitalpos-api` todavia no expone endpoints HTTP para listar sedes ni puntos de venta; `SEDE-WEB-001` se implemento con entrada manual temporal, sin inventar IDs hardcodeados; se agregaron modelos TypeScript para sede/punto de venta; `CrearVentaRequest` incluye `puntoVentaId`; Stock request/response soporta `sedeId`; `StockApiService` envia `sedeId` como query param/body segun corresponda; `/app/ventas` tiene campos obligatorios `sedeId` y `puntoVentaId`; `/app/ventas` usa `sedeId` para consultar stock base; `/app/ventas` usa `sedeId` para consultar stock de variante; `/app/ventas` envia `puntoVentaId` en `POST /api/ventas`; `/app/ventas` bloquea venta si falta `puntoVentaId`; `/app/ventas` bloquea agregar producto si falta `sedeId`; `/app/inventario` tiene campo obligatorio `sedeId`; `/app/inventario` usa `sedeId` al consultar stock base; `/app/inventario` usa `sedeId` al consultar stock variante; `/app/inventario` envia `sedeId` al ajustar stock; `/app/inventario` bloquea consultar/ajustar si falta sede; se mantiene venta sin variante; se mantiene venta con variante y `productoVarianteId`; se mantiene seleccion visual producto/variante en inventario; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 148 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; queda pendiente `SEDE-003` para reemplazar campos manuales por selectores reales cuando existan endpoints backend; `package-lock.json` sigue como cambio previo no relacionado.

### SEDE-003 - Exponer endpoints de Sede/PuntoVenta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` expone endpoints seguros de lectura para listar sedes activas y puntos de venta activos de la empresa activa, permitiendo que Angular reemplace los campos manuales por selectores reales.
- Evidencia: se crearon endpoints `GET /api/sedes` y `GET /api/sedes/{sedeId}/puntos-venta`; ambos requieren JWT; ambos requieren `X-CapitalPos-EmpresaId`; ambos usan `EmpresaActivaEndpointFilter`; ambos exigen `PermisoEmpresa.OperarVentas`; `PermisoEmpresa.OperarVentas` fue elegido porque son endpoints de lectura operativa para registrar ventas con sede/punto de venta reales; se crearon `ListarSedesUseCase` y `ListarPuntosVentaUseCase`; `GET /api/sedes` lista solo sedes activas de empresa activa; `GET /api/sedes/{sedeId}/puntos-venta` lista solo puntos activos de esa sede y empresa activa; sedes/puntos de otra empresa no se filtran; sede ajena devuelve 404; puntos inactivos quedan fuera; pruebas HTTP cubren auth requerida, empresa activa requerida y usuario sin permiso recibe 403; pruebas estructurales incluyen `SedeEndpoints`; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 622 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `19a759e551f3563b7ac04dabddf5b398dc74cd78`; mensaje: `Exponer sedes y puntos de venta`; git status final limpio.

### SEDE-WEB-002 - Reemplazar campos manuales por selectores de sede/punto de venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` e `/app/inventario` usan selectores reales de sede y punto de venta consumiendo los endpoints de `capitalpos-api`, eliminando la entrada manual temporal de `SEDE-WEB-001`.
- Evidencia: se confirmo uso de endpoints `GET /api/sedes` y `GET /api/sedes/{sedeId}/puntos-venta`; se creo `SedesApiService`; `SedesApiService` lista sedes; `SedesApiService` lista puntos de venta por sede; `/app/ventas` reemplaza campos manuales por selectores reales de sede y punto de venta; `/app/ventas` autoselecciona sede cuando hay una unica opcion activa; `/app/ventas` autoselecciona punto de venta cuando hay una unica opcion activa; `/app/ventas` carga puntos de venta al cambiar sede; `/app/ventas` usa sede seleccionada para consultar stock base; `/app/ventas` usa sede seleccionada para consultar stock de variante; `/app/ventas` envia `puntoVentaId` seleccionado en `POST /api/ventas`; `/app/ventas` bloquea venta si falta punto de venta; `/app/ventas` bloquea agregar producto si falta sede; `/app/inventario` reemplaza campo manual por selector real de sede; `/app/inventario` autoselecciona sede cuando hay una unica opcion activa; `/app/inventario` usa sede seleccionada para consultar stock base; `/app/inventario` usa sede seleccionada para consultar stock variante; `/app/inventario` envia `sedeId` al ajustar stock; `/app/inventario` bloquea consultar/ajustar si falta sede; se mantiene seleccion visual producto/variante; se mantiene venta sin variante; se mantiene venta con variante; no se agrego logica de descuento local de stock; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 153 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `999b44f369265833d8f08051ffae4b99e06550e5`; mensaje: `Usar selectores de sede y punto de venta`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### SEDE-004 - Validacion E2E multisede del MVP

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: ecosistema completo
- Criterio de aceptacion: el flujo multisede funciona de extremo a extremo desde Angular y backend, validando sede, punto de venta, stock por sede, venta, aislamiento de stock y dashboard sin modificar codigo.
- Evidencia: se valido el flujo completo multisede sin modificar codigo; no se levanto `capitalpos-cpe-api`; empresa activa: `10000000-0000-0000-0000-000000000001`; sede usada: `Tienda Demo`; `sedeId`: `10000000-0000-0000-0000-000000000004`; punto de venta usado: `Caja Principal`; `puntoVentaId`: `10000000-0000-0000-0000-000000000005`; producto usado: `Producto Demo`; `productoId`: `10000000-0000-0000-0000-000000000006`; stock inicial ajustado en `Tienda Demo`: 5 libres; venta principal: `29d80daa-ecd0-4e59-bf1c-a57357f69d4f`; cantidad vendida: 2; stock posterior en `Tienda Demo`: 3 libres; se creo `Sede Control` temporal solo en base de validacion para probar aislamiento; stock de `Sede Control` quedo en 7 sin cambios; segunda venta controlada confirmo otra vez `Tienda Demo` 3 y `Sede Control` 7; ambas ventas persistieron con `SedeId` y `PuntoVentaId` correctos; dashboard cargo correctamente; dashboard mostro ventas de hoy S/ 40.00, operaciones 2, unidades 4, canal lider `TIENDA`, `Producto Demo` y stock bajo 3 libres; backend `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 622 pruebas; Angular `npm test -- --watch=false` paso con 153 pruebas; puertos 4200, 5096 y 5097 quedaron libres al final; git status `capitalpos-api` limpio; git status `capitalpos-web` solo mantiene `package-lock.json` como cambio previo; no hubo bloqueos funcionales.

### SERIE-001 - Series de comprobante por Sede

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: existe `SerieComprobante` por `Sede`, con contador vivo por tipo/serie, manteniendo `Comprobante` como historico de lo emitido.
- Evidencia: se creo entidad de dominio `SerieComprobante`; `SerieComprobante` queda asociada a `EmpresaId` y `SedeId`; se uso `TipoComprobante` con codigos SUNAT, por ejemplo `03` y `01`; la serie demo queda como tipo `03`, serie `B001` y `CorrelativoActual = 0`; `SerieComprobante` valida `Id`, `EmpresaId`, `SedeId`, `TipoComprobante`, `Serie` y correlativo no negativo; `Serie` se normaliza en mayusculas; se agrego application de series; se agrego `ISerieComprobanteRepository`; se agrego repositorio EF; se agrego configuracion EF; se actualizo `CapitalPosDbContext`; se actualizo `DependencyInjection`; se actualizo `Program.cs`; se actualizo `DemoSeed` para crear la serie demo por sede; se creo migracion `20260718212456_AgregarSeriesComprobante`; la migracion crea la tabla de series por sede; se agrego indice unico por `EmpresaId` + `SedeId` + `TipoComprobante` + `Serie`; se agrego FK compuesta hacia `Sede` usando `SedeId` + `EmpresaId`; se agregaron pruebas de dominio, application, EF/modelo, DI, `DemoSeed`, estructura e integracion HTTP; no se integro todavia con emision CPE; no se reemplazo todavia el correlativo manual en Angular; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 642 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `b2ffb644d30cc0e639cbb025804481094160fe45`; mensaje: `Agregar series de comprobante por sede`; git status final limpio.

### SERIE-002 - Usar SerieComprobante en emision CPE desde venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: la emision CPE desde venta usa `SerieComprobante` activa de la sede de la venta como fuente de verdad para serie/correlativo, manteniendo compatibilidad temporal con Angular.
- Evidencia: `Serie` y `Correlativo` se mantienen en `EmitirCpeDesdeVentaRequest` por compatibilidad con Angular; backend ignora `Serie` y `Correlativo` del request como fuente de verdad; emision usa `SerieComprobante` activa por `EmpresaId` + `Venta.SedeId` + `TipoComprobante`; si hay mas de una serie activa para el mismo tipo en la sede, se elige deterministicamente la primera por `Serie` ascendente; correlativo se incrementa solo cuando CPE responde exito funcional con estado `SIMULADO` o `ACEPTADO`; `RECHAZADO` no incrementa correlativo; errores tecnicos no incrementan correlativo; si no existe serie activa para la sede/tipo, falla antes de llamar a CPE; si la serie pertenece a otra sede, no se usa; `Comprobante` se registra con serie/correlativo reales usados por backend; se actualizo `EmitirCpeDesdeVentaUseCase`; se actualizo `VentaEndpoints`; se actualizo `ISerieComprobanteRepository`; se actualizo `EfSerieComprobanteRepository`; se actualizaron pruebas de series, ventas e integracion HTTP; deuda tecnica: concurrencia avanzada/lock transaccional de correlativos queda pendiente; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 647 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `cdc79e85ee6c2e3286455333725e2c9fb47b07e8`; mensaje: `Usar series por sede al emitir CPE`; git status final limpio.

### SERIE-WEB-001 - Ajustar Angular para emision CPE con serie/correlativo backend

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` deja de presentar serie/correlativo como datos editables por el usuario y comunica que se asignan automaticamente segun la sede, manteniendo compatibilidad con el request actual.
- Evidencia: `/app/ventas` ya no muestra Serie y Correlativo como campos editables; se muestra el mensaje "La serie y correlativo se asignan automaticamente segun la sede seleccionada."; `tipoComprobante` y `rucEmisor` siguen visibles/validados; Angular mantiene compatibilidad con `EmitirCpeDesdeVentaRequest`; request sigue enviando serie y correlativo por compatibilidad; valores de compatibilidad enviados: serie `B001` y correlativo `1`; si internamente estuvieran vacios/no validos, se normalizan antes del request; emision funciona sin intervencion del usuario en serie/correlativo; error backend por falta de serie activa se muestra con mensaje del contrato; emision exitosa sigue mostrando estado CPE y archivos; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 156 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `137892db58d4d7e147a17b42131aefcc87fc1ef6`; mensaje: `Ajustar emision CPE a series automaticas`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### SERIE-003 - Validar emision desde Angular con serie automatica

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: ecosistema completo
- Criterio de aceptacion: la emision CPE desde Angular usa la serie/correlativo automaticos del backend, incrementa `SerieComprobante` correctamente y no consume correlativo ante fallos controlados.
- Evidencia: se valido desde Angular hasta `capitalpos-api` y `capitalpos-cpe-api` en modo simulado; no se modifico codigo; no se hicieron commits; Angular no muestra campo editable Serie; Angular no muestra campo editable Correlativo; Angular muestra el mensaje "La serie y correlativo se asignan automaticamente segun la sede seleccionada."; Angular siguio enviando `B001/1` por compatibilidad; backend no uso el correlativo `1` enviado como fuente de verdad; empresa: `CapitalPOS Demo`; `empresaId`: `10000000-0000-0000-0000-000000000001`; sede: `Tienda Demo`; `sedeId`: `10000000-0000-0000-0000-000000000004`; punto de venta: `Caja Principal`; `puntoVentaId`: `10000000-0000-0000-0000-000000000005`; producto: `Producto Demo`; SKU: `DEMO-001`; `SerieComprobante` antes: tipo `03`, serie `B001`, `CorrelativoActual` 0, activa true; despues de dos emisiones exitosas: `CorrelativoActual` 2; venta fallida sin cliente no persistio comprobante ni incremento serie; primera emision exitosa: comprobante `20600000001-03-B001-1`, estado `SIMULADO`; segunda emision exitosa: comprobante `20600000001-03-B001-2`, estado `SIMULADO`; ambas ventas conservaron `SedeId` y `PuntoVentaId` correctos; comprobantes persistidos con `TipoComprobante` `03`, `Serie` `B001`, correlativos 1 y 2; XML, ZIP, CDR e historial generados para ambos comprobantes; CPE usado en BETA con `SimularFirma=true`, `SimularEnvioSunat=true` y `GuardarCdrSimulado=true`; backend `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 647 pruebas; Angular `npm test -- --watch=false` paso con 156 pruebas; puertos 4200, 5096 y 5097 quedaron libres al final; git status `capitalpos-api` limpio; git status `capitalpos-web` solo mantiene `package-lock.json` como cambio previo; bloqueo resuelto: base limpia no tenia configuracion fiscal y se configuro via `PUT /api/configuracion-fiscal`; bloqueo resuelto: primera venta sin cliente fue rechazada correctamente sin incrementar correlativo.

### CAT-001 - Categoria y Marca de productos

- Prioridad: Media
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: el catalogo permite clasificar productos por `Categoria` opcional y `Marca` opcional, ambas aisladas por empresa.
- Evidencia: se creo entidad de dominio `Categoria`; `Categoria` es por empresa; `Categoria` soporta `CategoriaPadreId` opcional; regla MVP de maximo un nivel validada en `CrearCategoriaUseCase`; se creo entidad de dominio `Marca`; `Marca` es por empresa y plana; `Producto` ahora tiene `CategoriaId` opcional; `Producto` ahora tiene `MarcaId` opcional; `Producto` mantiene compatibilidad con clasificacion no obligatoria; al crear producto, application valida que `CategoriaId`/`MarcaId` pertenezcan a la empresa activa si se informan; se agrego application de catalogo; se agregaron `ICategoriaRepository` e `IMarcaRepository`; se agregaron repositorios EF; se agregaron configuraciones EF; se actualizo `CapitalPosDbContext`; se actualizo `DependencyInjection`; se actualizo `Program.cs`; se actualizo `DemoSeed` para crear categoria `General` y marca `Demo`; `Producto Demo` queda clasificado con categoria y marca demo; se creo migracion `20260720223708_AgregarCategoriasYMarcas`; la migracion crea tablas `categorias` y `marcas`; agrega `CategoriaId` y `MarcaId` nullable a `productos`; configura FKs compuestas con `EmpresaId`; configura indices unicos `EmpresaId` + `Nombre`; se agregaron pruebas de dominio, application, EF/modelo, DI, `DemoSeed`, estructura e integracion HTTP; no se crearon endpoints de categorias/marcas todavia; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 675 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `2e4a981cb2afc0d2000ec3e8e04efa7a7bd2a00f`; mensaje: `Agregar categorias y marcas de productos`; git status final limpio.

### CAT-002 - Exponer endpoints de Categoria y Marca

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` expone endpoints seguros y multiempresa para listar y crear categorias/marcas operativas del catalogo de productos.
- Evidencia: se crearon endpoints `GET /api/categorias` y `POST /api/categorias`; se crearon endpoints `GET /api/marcas` y `POST /api/marcas`; los endpoints requieren JWT; los endpoints requieren `X-CapitalPos-EmpresaId`; los endpoints usan `EmpresaActivaEndpointFilter`; los endpoints exigen `PermisoEmpresa.OperarAlmacen`; `PermisoEmpresa.OperarAlmacen` fue elegido porque categorias y marcas son catalogo operativo de inventario/productos; `GET /api/categorias` lista solo categorias activas de empresa activa; `GET /api/marcas` lista solo marcas activas de empresa activa; `POST` crea usando empresa activa; se ignora/impide `EmpresaId` libre; se valida nombre obligatorio; categoria padre de otra empresa falla; categoria de segundo nivel falla; duplicados por empresa fallan; se agregaron pruebas de auth, empresa activa, permisos, anti-fuga multiempresa, validaciones y duplicados; pruebas estructurales de proteccion/permisos incluyen `CatalogoEndpoints`; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; no se agregaron migraciones; no se instalaron paquetes; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 698 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `d632aec24cba6e6caf5d87c2b50e9b9b62b00787`; mensaje: `Exponer categorias y marcas`; git status final limpio.

### CAT-WEB-001 - Usar categorias y marcas en productos Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/productos` permite usar categorias y marcas reales del backend al crear productos, manteniendo el flujo usable si el catalogo falla.
- Evidencia: se agregaron modelos de catalogo en Angular; se creo `CatalogoApiService`; `CatalogoApiService` lista categorias; `CatalogoApiService` crea categorias; `CatalogoApiService` lista marcas; `CatalogoApiService` crea marcas; `Producto` model soporta `categoriaId` y `marcaId`; `/app/productos` carga categorias y marcas al iniciar; `/app/productos` muestra selectores opcionales de Categoria y Marca; permite `Sin categoria` y `Sin marca`; permite creacion rapida de categoria desde la misma pantalla; permite creacion rapida de marca desde la misma pantalla; al crear categoria/marca rapida, se agrega a la lista y queda seleccionada; crear producto envia `categoriaId`/`marcaId` cuando se seleccionan; crear producto envia null sin seleccion; si falla cargar catalogo, productos sigue usable y puede crear sin categoria/marca; errores backend de categoria/marca se muestran; variantes existentes siguen funcionando; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 164 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `8ed95d116c68210eff28bfeb7a2b02aa28292466`; mensaje: `Usar categorias y marcas en productos`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### PRES-001 - Presentaciones y unidades de medida

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: el catalogo soporta `UnidadMedida` y `ProductoPresentacion`, con factor de conversion, precio editable y datos historicos suficientes para venta por presentacion.
- Evidencia: se creo `UnidadMedida` como catalogo global; se agrego `ModoManejoProducto` en `Producto` con valores `SIMPLE`, `VARIANTE` y `PRESENTACION`; Producto Demo mantiene `ModoManejoProducto.SIMPLE`; se creo `ProductoPresentacion` por empresa/producto/unidad; `ProductoPresentacion` mantiene `EmpresaId` para aislamiento multiempresa; `FactorConversion` debe ser mayor que 0; `PrecioVenta` debe ser mayor que 0; `CodigoBarras` es opcional y unico por empresa cuando existe; se agregaron repositorios application y EF para unidades y presentaciones; se agregaron configuraciones EF; se actualizo `CapitalPosDbContext`; se actualizo `DependencyInjection`; se actualizo `DemoSeed` para crear unidades basicas `UND`, `CAJ`, `PAQ`, `DOC` y `KG`; `DemoSeed` mantiene idempotencia; se creo migracion `20260721023609_AgregarPresentacionesYUnidadesMedida`; se agregaron pruebas de dominio, EF/modelo, DI, DemoSeed y HTTP factory; no se integro venta por presentacion; no se crearon endpoints todavia; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 715 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `aea1e00d2e28d3a3ca34bec8c1c610c212b616aa`; mensaje: `Agregar presentaciones y unidades de medida`; git status final limpio.

### PRES-002 - Exponer endpoints de unidades y presentaciones

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` expone endpoints seguros para listar unidades de medida y gestionar presentaciones por producto dentro de la empresa activa.
- Evidencia: se creo `GET /api/unidades-medida`; se creo `GET /api/productos/{productoId}/presentaciones`; se creo `POST /api/productos/{productoId}/presentaciones`; los endpoints requieren JWT; los endpoints requieren `X-CapitalPos-EmpresaId`; los endpoints usan `EmpresaActivaEndpointFilter`; los endpoints exigen `PermisoEmpresa.OperarAlmacen`; `PermisoEmpresa.OperarAlmacen` fue elegido porque unidades/presentaciones pertenecen al catalogo operativo de inventario/productos; `GET /api/unidades-medida` lista unidades activas globales; `GET /api/productos/{productoId}/presentaciones` lista presentaciones activas del producto para la empresa activa; `POST` crea presentacion usando empresa activa; `POST` ignora/impide `EmpresaId` libre; valida producto de empresa activa; valida unidad de medida activa; valida `factorConversion > 0`; valida `precioVenta > 0`; valida `codigoBarras` unico por empresa si se informa; response de presentacion incluye `id`, `empresaId`, `productoId`, `productoVarianteId`, `unidadMedidaId`, `unidadCodigo`, `unidadNombre`, `factorConversion`, `esUnidadBase`, `precioVenta`, `codigoBarras`, `activo` y `fechaCreacion`; se agregaron use cases/DTOs application de unidades y presentaciones; se ajusto `IProductoPresentacionRepository`; se ajusto `EfProductoPresentacionRepository`; se agregaron pruebas Application, HTTP y estructurales; no se creo migracion nueva; no se instalaron paquetes; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 743 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `e489c154a1cdf158919b830cf1c60b9c551ab22b`; mensaje: `Exponer unidades y presentaciones de producto`; git status final limpio.

### PRES-WEB-001 - Gestionar presentaciones desde Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/productos` permite listar unidades, mostrar presentaciones por producto y crear una presentacion basica sin romper variantes ni el flujo actual.
- Evidencia: se agregaron modelos Angular de unidades y presentaciones; `ProductosApiService` consume `GET /api/unidades-medida`; `ProductosApiService` consume `GET /api/productos/{productoId}/presentaciones`; `ProductosApiService` consume `POST /api/productos/{productoId}/presentaciones`; `/app/productos` carga unidades de medida al iniciar; si falla cargar unidades, productos/variantes siguen usables; al expandir producto se muestran variantes y presentaciones; presentaciones reutiliza el bloque visual de variantes para mantener UI simple; `/app/productos` permite crear presentacion con unidad, `factorConversion`, `esUnidadBase`, `precioVenta` y `codigoBarras` opcional; `factorConversion` invalido bloquea envio; `precioVenta` invalido bloquea envio; errores backend se muestran claramente; variantes existentes siguen funcionando; no se implemento venta por presentacion; no se implemento stock por presentacion; no se implemento edicion/desactivacion; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no se instalaron paquetes; `npm test -- --watch=false` paso con 171 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `5476018434e3af27806fbeb4d65c76871cf0282c`; mensaje: `Gestionar presentaciones en productos Angular`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### PRES-POS-001A - Soportar venta por presentacion en backend

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` permite registrar ventas por presentacion, usando precio/factor del backend y descontando stock operativo sin romper venta base ni venta por variante.
- Evidencia: `VentaDetalle` ahora soporta `ProductoPresentacionId` opcional; se mantiene compatibilidad con venta por producto base; se mantiene compatibilidad con venta por variante; `CrearVentaRequest` soporta `ProductoPresentacionId`; `VentaResponse` devuelve `ProductoPresentacionId`; si se vende por presentacion, backend usa `ProductoPresentacion.PrecioVenta` como fuente de verdad; si se vende por presentacion, descuenta stock operativo por `cantidad * FactorConversion`; stock operativo sigue siendo `StockProducto` por `EmpresaId + SedeId + ProductoId + ProductoVarianteId`; presentacion sin variante descuenta stock base con `productoVarianteId` null; presentacion con variante descuenta stock de variante cuando el detalle trae `ProductoVarianteId`; valida presentacion existente; valida presentacion de empresa activa; valida presentacion del producto del detalle; valida presentacion activa; stock insuficiente por factor falla sin persistir venta; no permite descuentos parciales si un detalle falla; `EmitirCpeDesdeVentaUseCase` soporta venta con presentacion sin romper payload CPE; se creo migracion `20260721031110_AgregarPresentacionAVentaDetalle`; se actualizaron configuraciones EF de `ProductoPresentacion` y `VentaDetalle`; se actualizaron `EndpointInputValidator` y `VentaEndpoints`; se agregaron pruebas de dominio, application, EF e integracion HTTP; deuda tecnica: presentacion todavia no tiene vinculo propio a variante; deuda tecnica: revisar/mantener mapeo formal de codigos de unidad contra catalogo SUNAT para emision real; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; no se instalaron paquetes; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 750 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `197230c21ed9b1f2ba69ab2a440bcea83047fd01`; mensaje: `Soportar venta por presentacion en backend`; git status final limpio.

### PRES-POS-001B - Seleccionar presentacion en POS Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` permite seleccionar presentacion, enviar `productoPresentacionId` al backend y mantener venta base/variante, stock y errores bajo control.
- Evidencia: `/app/ventas` carga presentaciones con `ProductosApiService.listarPresentaciones(productoId)`; si hay presentaciones activas, muestra selector con unidad, factor, precio y codigo de barras; el precio visible de presentacion se muestra como definido por backend; el carrito muestra producto + variante si aplica + presentacion si aplica; `POST /api/ventas` envia `productoPresentacionId` cuando corresponde; venta base envia `productoPresentacionId` null/undefined; venta variante + presentacion envia `productoVarianteId` y `productoPresentacionId`; bloqueo preventivo usa `cantidad * factorConversion` contra `stockLibre`; backend sigue siendo fuente de verdad; error backend por stock conserva carrito; no se implemento stock por presentacion; no se implemento lector de barras; no se implemento matriz talla/color; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 177 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `5bdaa84`; mensaje: `Seleccionar presentacion en POS Angular`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### PRES-003 - Validacion funcional venta por presentacion

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: una venta por presentacion descuenta stock por factor, bloquea stock insuficiente y conserva consistencia de venta, detalle, sede y punto de venta.
- Evidencia: se valido el flujo completo de venta por presentacion; no se modifico codigo; no se hicieron commits; no se levanto `capitalpos-cpe-api`; base temporal usada: `capitalpos_pres003`; empresa: Empresa Demo; sede: Tienda Demo; punto de venta: Caja Principal; producto: Producto Demo; SKU: `DEMO-001`; presentacion usada: CAJ - Caja; `factorConversion`: 12; `precioVenta`: S/ 120.00; `codigoBarras`: `PRES003-CAJ-001`; stock inicial ajustado: 24 unidades base; venta valida: 1 caja; consumo calculado: 12 unidades base; stock posterior a venta valida: 12 unidades base; venta registrada: `b0104586-278e-473f-8971-4dce30581452`; `VentaDetalle` persistio `ProductoPresentacionId` `e6668e5d-29d9-4332-a66f-3213afe66748`; `VentaDetalle` persistio cantidad 1.000; `VentaDetalle` persistio precio unitario 120.00; venta conservo `SedeId` y `PuntoVentaId` correctos; con stock visible 12, Angular bloqueo agregar 2 cajas; en prueba controlada de concurrencia, backend rechazo 2 cajas cuando el stock real bajo a 12; backend mostro mensaje de stock insuficiente; carrito conservo las 2 cajas tras rechazo backend; no se persistio venta parcial; stock permanecio en 12 tras rechazo; base termino con exactamente 1 venta y 1 detalle; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 750 pruebas; `capitalpos-web` `npm test -- --watch=false` paso con 177 pruebas; puertos 4200, 5096 y 5097 quedaron libres; `capitalpos-api` git status limpio; `capitalpos-web` solo mantiene `package-lock.json` como cambio previo; `capitalpos-cpe-api` conserva cambio previo ajeno en `ArchivosController.cs`; observacion no bloqueante: `/app/productos` mostro presentaciones, pero el selector de creacion indico incorrectamente que no habia unidades disponibles aunque `/api/unidades-medida` devolvia CAJ, DOC, KG, PAQ y UND; registrar como incidencia frontend posterior.

### PRES-WEB-002 - Corregir carga de unidades al crear presentaciones

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/productos` muestra correctamente las unidades activas disponibles al crear presentaciones y mantiene el flujo usable ante errores de carga.
- Evidencia: causa encontrada: `GET /api/unidades-medida` devuelve `Activa` en el DTO publico, serializado como `activa`, pero Angular filtraba solo `unidad.activo`; `UnidadMedidaResponse` ahora acepta `activo` y `activa`; `unidadesMedidaActivas()` soporta ambas formas; si `GET /api/unidades-medida` devuelve CAJ, DOC, KG, PAQ y UND, el selector las muestra; no aparece mensaje de `sin unidades` cuando hay unidades activas; si falla cargar unidades, se muestra mensaje claro; productos/variantes siguen usables si falla cargar unidades; crear presentacion sigue enviando `unidadMedidaId` seleccionado; listado/creacion de presentaciones sigue funcionando; variantes existentes siguen funcionando; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 179 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `f6751de`; mensaje: `Corregir carga de unidades en presentaciones`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### CAJA-001 - Sesion de caja

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: existe `SesionCaja` para apertura/cierre de turno sobre un `PuntoVenta`, sin bloquear el avance del MVP multisede.
- Evidencia: se creo entidad de dominio `SesionCaja`; se creo enum `EstadoSesionCaja` con `Abierta` y `Cerrada`; `SesionCaja` queda por `EmpresaId + SedeId + PuntoVentaId`; `DiferenciaCierre` se persiste al cierre como `MontoDeclaradoCierre - MontoInicial`; se impide doble apertura con validacion en use case; se agrego indice unico filtrado PostgreSQL para `EmpresaId + PuntoVentaId + Estado` cuando `Estado = 'Abierta'`; no se sembro caja demo abierta; se creo `ISesionCajaRepository`; se creo `AbrirSesionCajaUseCase`; se creo `CerrarSesionCajaUseCase`; se creo `ObtenerSesionCajaAbiertaUseCase`; se agregaron requests de caja; se agrego `EfSesionCajaRepository`; se agrego `SesionCajaConfiguration`; se actualizo `CapitalPosDbContext`; se actualizo `DependencyInjection`; se actualizo `Program.cs`; se creo migracion `20260721051112_AgregarSesionesCaja`; se actualizo snapshot EF; se agregaron pruebas de dominio, application, EF/modelo, DI, estructura y factory HTTP; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; no se instalaron paquetes; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 764 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `1c36e6b5b354e6a166f450496d3bf63bb9b2c88f`; mensaje: `Agregar sesion de caja`; git status final limpio; deuda CAJA-002: exponer endpoints de abrir/cerrar/consultar caja; deuda CAJA-003: asociar ventas a sesion de caja activa; deuda posterior: movimientos de caja, metodos de pago y arqueo detallado.

### CAJA-002 - Exponer endpoints de sesion de caja

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` expone endpoints seguros para consultar, abrir y cerrar sesiones de caja por punto de venta dentro de la empresa activa.
- Evidencia: se creo `GET /api/caja/sesiones/abierta?puntoVentaId={guid}`; se creo `POST /api/caja/sesiones/abrir`; se creo `POST /api/caja/sesiones/{sesionCajaId}/cerrar`; los endpoints requieren JWT; los endpoints requieren `X-CapitalPos-EmpresaId`; los endpoints usan `EmpresaActivaEndpointFilter`; los endpoints exigen `PermisoEmpresa.OperarVentas`; `PermisoEmpresa.OperarVentas` fue elegido porque abrir/cerrar caja queda dentro de la operacion diaria del punto de venta y todavia no existe permiso especifico de caja; `GET` abierta devuelve caja abierta de empresa activa por `puntoVentaId`; `GET` abierta no fuga caja de otra empresa; `GET` abierta sin caja devuelve respuesta clara; `POST` abrir crea caja para punto de venta de empresa activa; `POST` abrir ignora/impide `EmpresaId` libre; `POST` abrir falla con `montoInicial` negativo; `POST` abrir falla para punto de venta de otra empresa; `POST` abrir falla si ya hay caja abierta; `POST` cerrar cierra caja abierta; `POST` cerrar calcula diferencia; `POST` cerrar falla con monto negativo; `POST` cerrar falla para caja de otra empresa; `POST` cerrar falla si ya esta cerrada; se agregaron pruebas HTTP de auth, empresa activa, permisos, anti-fuga, validaciones y reglas de apertura/cierre; pruebas estructurales de proteccion/permisos incluyen `CajaEndpoints`; no se creo migracion; no se instalaron paquetes; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 779 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `9b58591201c7b8e756f276b73a0319438742e4b8`; mensaje: `Exponer endpoints de sesion de caja`; git status final limpio.

### CAJA-WEB-001 - Operar sesion de caja desde Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` permite consultar, abrir y cerrar caja para el punto de venta seleccionado, sin bloquear todavia el flujo de venta.
- Evidencia: se crearon modelos Angular de caja; se creo `CajaApiService`; `CajaApiService` consume `GET /api/caja/sesiones/abierta?puntoVentaId={guid}`; `CajaApiService` consume `POST /api/caja/sesiones/abrir`; `CajaApiService` consume `POST /api/caja/sesiones/{sesionCajaId}/cerrar`; `/app/ventas` consulta caja al seleccionar/autoseleccionar punto de venta; si no hay caja abierta, muestra formulario para abrir caja; si hay caja abierta, muestra fecha/monto inicial y formulario para cerrar caja; al cerrar, muestra diferencia de cierre si backend la devuelve; errores backend de caja se muestran claramente; cambio de punto de venta refresca estado de caja; no bloquea venta todavia por falta de caja; se mantienen sede/punto de venta, stock, variantes, presentaciones, canal y emision CPE; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 190 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `86948a7`; mensaje: `Operar sesion de caja desde ventas`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### CAJA-003 - Exigir caja abierta para registrar venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` impide registrar ventas si el punto de venta no tiene una sesion de caja abierta para la empresa activa.
- Evidencia: `CrearVentaUseCase` exige sesion de caja abierta por `EmpresaId + PuntoVentaId` antes de registrar venta; si no hay caja abierta, falla con mensaje claro; si solo hay caja cerrada, falla; si la caja abierta pertenece a otra empresa, falla; si falta caja, no persiste venta; si falta caja, no descuenta stock; venta normal sigue funcionando con caja abierta; venta con variante sigue funcionando con caja abierta; venta por presentacion sigue funcionando con caja abierta; `POST /api/ventas` devuelve error claro cuando no hay caja abierta; no se agrego `SesionCajaId` a `Venta`; no se creo migracion; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; no se instalaron paquetes; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 783 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `36575d5e162d214dee584cf98389fb2ec2de79df`; mensaje: `Exigir caja abierta para registrar ventas`; git status final limpio.

### CAJA-WEB-002 - Bloquear venta en Angular si no hay caja abierta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` bloquea el registro de venta si el punto de venta seleccionado no tiene caja abierta y conserva el carrito ante rechazos del backend por caja.
- Evidencia: Registrar venta queda deshabilitado si no hay caja abierta; se muestra `Abre una sesión de caja para registrar ventas.`; abrir caja habilita venta sin recargar; cerrar caja vuelve a bloquear venta; si backend rechaza por caja cerrada/sin caja, muestra mensaje backend; si backend rechaza por caja cerrada/sin caja, conserva carrito; si backend rechaza por caja cerrada/sin caja, refresca estado de caja; venta base con caja abierta sigue funcionando; venta con variante con caja abierta sigue funcionando; venta por presentacion con caja abierta sigue funcionando; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 194 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `a554524`; mensaje: `Bloquear venta sin caja abierta`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### CAJA-004 - Validacion funcional caja + venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: el flujo caja + venta funciona desde Angular hasta backend y PostgreSQL: sin caja bloquea venta, con caja abierta vende y descuenta stock, al cerrar caja vuelve a bloquear venta.
- Evidencia: se valido flujo caja + venta desde Angular, backend y PostgreSQL; no se modifico codigo; no se hicieron commits; no se levanto `capitalpos-cpe-api`; base temporal usada: `capitalpos_caja004`; empresa: Empresa Demo; sede: Tienda Demo; punto de venta: Caja Principal; producto: Producto Demo; SKU: `DEMO-001`; stock inicial: 20; venta valida: 1 unidad por S/ 10.00; stock posterior: 19; sesion de caja: `d0a07ab3-8acb-481d-912b-31c8475134ac`; estado final de caja: Cerrada; monto inicial: S/ 100.00; monto declarado cierre: S/ 120.00; diferencia cierre: S/ 20.00; observacion apertura: Validacion CAJA-004; observacion cierre: Cierre validacion CAJA-004; fechas de apertura y cierre persistidas; usuario de apertura y cierre persistido; venta registrada: `b4c99387-f242-4743-b3f2-14f0f9a14787`; venta quedo en estado Registrada; venta total: S/ 10.00; cantidad vendida: 1; venta persistio `SedeId` y `PuntoVentaId` correctos; fecha de creacion de venta esta entre apertura y cierre de caja; antes de abrir caja, Angular mostro Sin caja abierta; antes de abrir caja, Registrar venta quedo deshabilitado; antes de abrir caja, mostro `Abre una sesión de caja para registrar ventas.`; despues de cerrar caja, Angular volvio a deshabilitar Registrar venta; despues de cerrar caja, volvio a mostrar el mismo mensaje; intento forzado por API despues del cierre respondio HTTP 400; mensaje backend: `Debe abrir una sesion de caja antes de registrar ventas.`; ventas antes/despues del rechazo: 1 -> 1; stock antes/despues del rechazo: 19 -> 19; no hubo venta parcial; no hubo descuento parcial; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 783 pruebas; `capitalpos-web` `npm test -- --watch=false` paso con 194 pruebas; puertos 4200, 5096 y 5097 quedaron libres; `capitalpos-api` git status limpio; `capitalpos-web` solo mantiene `package-lock.json` como cambio previo; `capitalpos-cpe-api` conserva cambio previo ajeno en `ArchivosController.cs`; no hubo bloqueos funcionales.

## 17. Documentacion y validacion demo MVP retail

### DOC-001 - Actualizar RUNBOOK_MVP.md al MVP retail actual

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: documentacion raiz
- Criterio de aceptacion: `RUNBOOK_MVP.md` describe como levantar, operar y validar el MVP retail actual con caja, multisede, catalogo, stock, presentaciones, venta y CPE opcional.
- Evidencia: `RUNBOOK_MVP.md` actualizado; documenta arquitectura `capitalpos-web -> capitalpos-api -> capitalpos-cpe-api` opcional; incluye requisitos, versiones, puertos y secretos con placeholders; incluye arranque ordenado con base, migraciones y `--no-launch-profile`; incluye datos actuales del `DemoSeed`; incluye flujo completo: empresa, sede, caja, catalogo, variantes, presentaciones, stock, venta, CPE y cierre; incluye consultas curl autenticadas; incluye checks SQL para `sedes`, `puntos_venta`, `stocks_productos`, `sesiones_caja`, `ventas`, `ventas_detalles`, `comprobantes` y `series_comprobante`; incluye troubleshooting operativo; documenta que CPE no es obligatorio para validar caja y ventas; separa CPE simulado y SUNAT beta real; documenta caja abierta como precondicion backend/frontend; documenta series/correlativos como responsabilidad automatica del backend; aclara que la tabla correcta es `ventas_detalles`; documenta workaround por API cuando Angular no carga unidades; incluye control de hora Lima y prevencion de XML/ZIP/CDR/PFX en Git; indica no ejecutar `npm install` ni revertir `package-lock.json` previamente modificado; no se modifico codigo, `TASKS.md` ni `PLANNING.md`; no se hicieron commits.

### DOC-002 - Actualizar DEMO_MVP.md al MVP retail actual

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: documentacion raiz
- Criterio de aceptacion: `DEMO_MVP.md` queda alineado al MVP retail actual y sirve como guion comercial/operativo para demo cliente.
- Evidencia: `DEMO_MVP.md` actualizado; incluye estado funcional y validacion tecnica del MVP; define dos modalidades: retail sin CPE y facturacion electronica; incluye datos demo recomendados; incluye guion completo desde login hasta cierre de caja; incluye mensaje comercial para distintos giros retail; incluye promesas y expresiones que deben evitarse; incluye limitaciones actuales; incluye plan de recuperacion ante fallos; incluye checklist tecnico, operativo y de seguridad; la demo retail sin CPE queda como recorrido comercial recomendado; CPE se presenta como extension opcional; SUNAT beta se reconoce como validado previamente, sin prometer produccion; caja, stock y reportes se describen como capacidades basicas; `stocks_productos` se identifica como referencia operativa multisede frente a la deuda de `ProductoVariante.StockActual`; se documentan el problema eventual de unidades en presentaciones y `package-lock.json` previamente modificado; se prioriza continuar la historia retail si CPE falla; no se modifico codigo, `RUNBOOK_MVP.md`, `TASKS.md` ni `PLANNING.md`; no se hicieron commits.

### DEMO-005 - Validacion demo completa retail actual

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: la demo retail operativa sin CPE se ejecuta de extremo a extremo desde Angular, con caja, catalogo, variantes, presentaciones, stock, ventas, dashboard y reportes coherentes.
- Evidencia: se valido demo retail operativa sin CPE de extremo a extremo; no se modifico codigo ni documentacion; no se hicieron commits ni push; base temporal usada: `capitalpos_demo005`; usuario demo: `admin@capitalpos.test`; empresa: CapitalPOS Demo S.A.C.; `empresaId`: `10000000-0000-0000-0000-000000000001`; sede: Tienda Demo; punto de venta: Caja Principal; canal: TIENDA; `capitalpos-cpe-api` no fue levantado; venta bloqueada antes de abrir caja; caja abierta con monto inicial S/ 100.00; observacion apertura: Validacion DEMO-005; caja cerrada con monto declarado S/ 255.00; diferencia cierre S/ 155.00; caja quedo persistida como Cerrada; venta volvio a bloquearse despues del cierre; producto seed usado: Producto Demo, SKU `DEMO-001`; producto simple creado desde Angular: Producto Base DEMO005, SKU `DEMO005-BASE`; categoria: General; marca: Demo; variante creada: talla M, color Negro, SKU `DEMO005-M-NEG`; presentacion creada: CAJ/Caja, factor 12, precio S/ 120.00; unidades visibles correctamente: CAJ, DOC, KG, PAQ y UND; venta producto base simple: cantidad 1, stock 10 -> 9, total S/ 15.00, venta `32103616-208f-47cc-92fd-8eea8956ae60`; venta variante M/Negro: cantidad 2, stock 5 -> 3, total S/ 20.00, venta `6dc9f90e-1239-4100-bb0f-7727a5005a23`; venta presentacion CAJ x12: cantidad 1 caja, stock base 30 -> 18, total S/ 120.00, venta `cda873ac-5603-4362-89d8-dccb5d23b4e3`; las tres ventas fueron exitosas unicamente con caja abierta; el carrito se limpio despues del exito; se conservaron Tienda Demo y Caja Principal; descuento de stock correcto en producto base, variante y presentacion; venta por caja desconto exactamente 12 unidades base; dashboard mostro ventas de hoy S/ 155.00, operaciones 3, unidades comerciales 4, canal lider TIENDA; dashboard mostro stock bajo de variante M/Negro con 3 libres; reporte ventas por canal mostro 3 ventas, 4 unidades comerciales, S/ 155.00, precio promedio S/ 38.75 y TIENDA 100%; PostgreSQL verifico `sedes`, `puntos_venta`, `sesiones_caja`, `productos`, `productos_variantes`, `productos_presentaciones`, `stocks_productos`, `ventas`, `ventas_detalles`, `series_comprobante` y `comprobantes`; persistencia: 1 sesion cerrada y ninguna abierta; persistencia: 3 ventas y 3 detalles; las ventas fueron creadas dentro del intervalo apertura/cierre; variante y presentacion quedaron referenciadas en sus detalles; stocks finales: base simple 9, variante 3, base producto seed 18; serie `03-B001` activa con correlativo 0 porque no se emitio CPE; 0 comprobantes, esperado; migraciones aplicadas hasta `20260721051112_AgregarSesionesCaja`; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 783 pruebas; `capitalpos-web` `npm test -- --watch=false` paso con 194 pruebas usando Node v24.15.0; puertos 4200, 5096 y 5097 quedaron libres; archivo temporal de credenciales eliminado; `capitalpos-api` git status limpio; `capitalpos-web` solo mantiene `package-lock.json` como cambio previo; `capitalpos-cpe-api` conserva cambio previo ajeno en `ArchivosController.cs`; `RUNBOOK`, `DEMO`, `TASKS` y `PLANNING` no fueron modificados durante la validacion; recomendaciones: usar producto simple separado para venta base; preparar stocks 10/5/30 y presentacion CAJ x12; explicar unidades comerciales vs consumo fisico; no usar `ProductoVariante.StockActual` como fuente de stock; mantener CPE fuera de demo retail salvo validacion simulada independiente.

## 18. Correcciones tecnicas y precios retail

### FIX-001 - Cerrar deuda tecnica de presentaciones

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: se elimina el stock duplicado de variantes y `VentaDetalle` conserva snapshot historico de conversion aplicado al vender por presentacion.
- Evidencia: se inspecciono el estado real del repo antes de modificar; `ProductoVariante` todavia tenia `StockActual` y `ActualizarStock`; `VentaDetalle` ya tenia `ProductoPresentacionId`, pero no snapshots de conversion; se elimino `ProductoVariante.StockActual`; se elimino `ProductoVariante.ActualizarStock`; se limpio `StockActual` de `CrearProductoVarianteRequest`, validator, EF config y response publico; se agrego `VentaDetalle.FactorConversionAplicado`; se agrego `VentaDetalle.CantidadBaseDescontada`; `CrearVentaUseCase` descuenta stock usando `VentaDetalle.CantidadBaseDescontada`; venta base guarda `FactorConversionAplicado = 1` y `CantidadBaseDescontada = Cantidad`; venta por variante guarda `FactorConversionAplicado = 1` y `CantidadBaseDescontada = Cantidad`; venta por presentacion guarda el factor vigente y `CantidadBaseDescontada = Cantidad * FactorConversion`; cambiar el factor despues no altera el snapshot del detalle ya creado; reportes/dashboard siguen pasando dentro de la suite completa; migracion creada: `20260724172014_CerrarDeudaPresentaciones`; la migracion elimina `StockActual` de `productos_variantes`; la migracion agrega snapshots a `ventas_detalles`; la migracion hace backfill de ventas historicas con factor 1 y cantidad base igual a `Cantidad`; `rg "StockActual|ActualizarStock"` solo queda en tests de ausencia y migraciones historica/nueva; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; no se instalaron paquetes; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 784 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `ab0fcfe336b3ee843bda5eee8208d477530978cd`; mensaje: `Cerrar deuda tecnica de presentaciones`; git status final limpio.

### PREC-001 - Precio mayorista por cantidad acumulada

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: soportar reglas de precio mayorista por producto; aplicar precio mayorista cuando la cantidad acumulada de variantes del mismo producto alcance la cantidad minima; mantener lineas independientes para SUNAT; guardar `PrecioMayoristaAplicado` en `VentaDetalle`; no tocar CPE en esta fase.
- Evidencia: se inspecciono el estado real del repo antes de modificar; se creo entidad `ReglaPrecioMayorista`; `ReglaPrecioMayorista` queda por `EmpresaId` y `ProductoId`; se creo `IReglaPrecioMayoristaRepository`; se creo `EfReglaPrecioMayoristaRepository`; se agrego configuracion EF; se agrego `DbSet`; se actualizo `DependencyInjection`; se creo migracion `20260724185001_AgregarReglasPreciosMayoristas`; se agrego `VentaDetalle.PrecioMayoristaAplicado`; `CrearVentaUseCase` agrupa detalles unitarios por `ProductoId`; `CrearVentaUseCase` suma cantidades comerciales, no `CantidadBaseDescontada`; si el total acumulado alcanza `CantidadMinima`, aplica `PrecioUnitarioMayorista`; si hay varias reglas activas alcanzadas, gana la de mayor `CantidadMinima`; mantiene lineas independientes por variante; precio mayorista aplica solo a producto base/variantes unitarias del mismo `ProductoId`; presentaciones no aplican mayorista y siguen usando `ProductoPresentacion.PrecioVenta`; variantes surtidas del mismo producto llegan a 12 y aplican precio mayorista; 11 unidades no aplican precio mayorista; productos distintos no mezclan cantidades; con reglas 12 y 24, gana la de 24; `PrecioMayoristaAplicado` queda true cuando aplica; `PrecioMayoristaAplicado` queda false cuando no aplica; stock se descuenta igual que antes; caja abierta sigue siendo requerida; `VentaDetalleResponse` devuelve `precioMayoristaAplicado`; CPE no fue modificado; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; no se instalaron paquetes; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 801 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `34725d2`; mensaje: `Agregar precio mayorista por cantidad acumulada`; git status final limpio.

### PREC-002 - Exponer endpoints de reglas de precio mayorista

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` expone endpoints seguros para administrar reglas de precio mayorista por producto dentro de la empresa activa.
- Evidencia: se inspecciono el estado real del repo antes de modificar; ultimo commit previo verificado: `34725d2 Agregar precio mayorista por cantidad acumulada`; `ReglaPrecioMayorista` y logica de venta ya existian desde PREC-001; no habia endpoints ni use cases administrativos para reglas mayoristas; se creo `GET /api/productos/{productoId}/precios-mayoristas`; se creo `POST /api/productos/{productoId}/precios-mayoristas`; se creo `PATCH /api/productos/{productoId}/precios-mayoristas/{reglaId}/activar`; se creo `PATCH /api/productos/{productoId}/precios-mayoristas/{reglaId}/desactivar`; los endpoints requieren JWT; los endpoints requieren `X-CapitalPos-EmpresaId`; los endpoints usan `EmpresaActivaEndpointFilter`; los endpoints exigen `PermisoEmpresa.OperarAlmacen`; `PermisoEmpresa.OperarAlmacen` fue elegido porque las reglas mayoristas son configuracion comercial del catalogo/producto; `GET` no fuga reglas de otra empresa/producto; `POST` crea con empresa activa; `POST` valida `cantidadMinima > 0`; `POST` valida `precioUnitarioMayorista > 0`; `POST` falla si producto es de otra empresa; `POST` falla si duplica `CantidadMinima` activa; `PATCH` activar/desactivar valida regla de producto/empresa; `PATCH` activar falla si genera duplicado activo; se agregaron use cases/request de reglas mayoristas; se ajusto `IReglaPrecioMayoristaRepository`; se ajusto `EfReglaPrecioMayoristaRepository`; se agregaron pruebas Application, HTTP e integracion; tests existentes de venta mayorista siguen pasando; no se creo migracion; no se modifico `capitalpos-web`; no se modifico `capitalpos-cpe-api`; no se instalaron paquetes; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 818 pruebas; `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `git diff --check` correcto; commit `8289911`; mensaje: `Exponer reglas de precio mayorista`; git status final limpio.

### PREC-WEB-001 - Gestionar reglas mayoristas desde Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/productos` permite crear, listar, activar y desactivar reglas de precio mayorista del producto sin romper variantes ni presentaciones.
- Evidencia: se inspecciono el estado real del repo antes de modificar; git status inicial solo mostraba `package-lock.json` modificado previamente; `/app/productos` ya gestionaba categorias, marcas, variantes y presentaciones; `ProductosApiService` no tenia metodos de precios mayoristas; se agregaron modelos `ReglaPrecioMayoristaResponse` y `CrearReglaPrecioMayoristaRequest`; `ProductosApiService` consume `GET /api/productos/{productoId}/precios-mayoristas`; `ProductosApiService` consume `POST /api/productos/{productoId}/precios-mayoristas`; `ProductosApiService` consume `PATCH /api/productos/{productoId}/precios-mayoristas/{reglaId}/activar`; `ProductosApiService` consume `PATCH /api/productos/{productoId}/precios-mayoristas/{reglaId}/desactivar`; `/app/productos` muestra seccion `Precio mayorista` dentro del producto expandido; `/app/productos` crea regla con cantidad minima y precio mayorista; bloquea cantidad/precio invalidos; lista cantidad minima, precio, estado y fecha; permite activar/desactivar reglas; refresca desde backend al crear/activar/desactivar; muestra ayuda `Aplica a unidades y variantes del mismo producto. No aplica a presentaciones.`; errores backend se muestran; falla al cargar reglas sin romper variantes/presentaciones; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 203 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `2862517`; mensaje: `Gestionar precios mayoristas en productos`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### PREC-POS-001 - Mostrar precio mayorista aplicado en POS Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` comunica cuando el carrito alcanza precio mayorista por cantidad acumulada sin reemplazar la decision final del backend.
- Evidencia: se inspecciono el estado real del repo antes de modificar; ultimo commit web previo: `2862517 Gestionar precios mayoristas en productos`; `ProductosApiService.listarPreciosMayoristas(productoId)` ya existia y se reutilizo; POS carga y cachea reglas mayoristas activas por `productoId`; carrito muestra `Mayorista aplicado` estimado cuando cantidad acumulada sin presentacion alcanza la regla; si falta cantidad, muestra `Faltan N unidades para precio mayorista`; productos distintos no mezclan cantidades; si existen reglas 12 y 24, con 24 muestra la regla de 24; presentaciones no cuentan para mayorista; presentaciones muestran que no aplican a mayorista; error cargando reglas no bloquea venta; no cambia el precio enviado como fuente de verdad; texto indica que backend confirma el precio final; `VentaDetalleResponse` soporta `precioMayoristaAplicado?: boolean | null`; se muestra confirmacion si backend devuelve `precioMayoristaAplicado`; venta base/variante/presentacion siguen funcionando; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 211 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `395bc6b`; mensaje: `Mostrar precio mayorista en POS`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### PREC-003 - Validacion funcional precio mayorista Brooklyn

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: una venta de 12 unidades surtidas del mismo modelo aplica precio mayorista a todas las lineas, mantiene variantes separadas y descuenta stock por variante correctamente.
- Evidencia: se valido precio mayorista Brooklyn en backend, persistencia, stocks, dashboard y reportes; no se modifico codigo ni documentacion; no se hicieron commits ni push; no se levanto `capitalpos-cpe-api`; base temporal usada: `capitalpos_prec003`; API usada: `http://localhost:5096`; Web usada: `http://127.0.0.1:4200`; usuario: `admin@capitalpos.test`; empresa: CapitalPOS Demo S.A.C.; RUC empresa: `20600000001`; `empresaId`: `10000000-0000-0000-0000-000000000001`; sede: Tienda Demo; punto de venta: Caja Principal; canal: TIENDA; producto creado desde Angular: Polo Brooklyn DEMO003; SKU producto: `BROOK-DEMO003`; precio normal: S/ 35.00; categoria: General; marca: Demo; variantes activas: S / Negro / `BROOK-S-N`, M / Negro / `BROOK-M-N`, L / Blanco / `BROOK-L-B`; regla mayorista activa: cantidad minima 12, precio unitario mayorista S/ 25.00; regla aplica acumulando variantes del mismo `ProductoId`; stock inicial/final S Negro: 10 -> 6; stock inicial/final M Negro: 10 -> 5; stock inicial/final L Blanco: 10 -> 7; venta positiva: `db9c86fa-de5a-43f0-98aa-3388130d5744`; venta 4 + 5 + 3 acumulo 12 unidades del mismo producto; total esperado: 12 x S/ 25.00 = S/ 300.00; total real backend: S/ 300.00; subtotal: S/ 254.24; IGV: S/ 45.76; estado venta: Registrada; las tres lineas permanecieron separadas por `ProductoVarianteId`; linea S Negro: cantidad 4, precio unitario S/ 25.00, `PrecioMayoristaAplicado` true, total S/ 100.00; linea M Negro: cantidad 5, precio unitario S/ 25.00, `PrecioMayoristaAplicado` true, total S/ 125.00; linea L Blanco: cantidad 3, precio unitario S/ 25.00, `PrecioMayoristaAplicado` true, total S/ 75.00; UI mostro `Mayorista estimado` en las tres lineas; UI mostro `Mayorista aplicado: 12+ unidades a S/ 25.00.`; despues del registro, UI mostro `Mayorista confirmado por backend en 3 línea(s).`; el carrito quedo limpio despues del exito; caso negativo: carrito 4 + 4 + 3 acumulo 11 unidades; caso negativo mostro `Faltan 1 unidades para precio mayorista.`; caso negativo no se registro; PostgreSQL confirmo que permanecen 1 venta y 3 detalles; caja abierta con S/ 100.00; caja cerrada con S/ 400.00; diferencia de caja S/ 300.00; estado final caja Cerrada; cajas abiertas al finalizar: 0; dashboard mostro ventas del dia S/ 300.00, operaciones 1, unidades 12, canal lider TIENDA; dashboard mostro top de productos separado por tres variantes con importes S/ 125, S/ 100 y S/ 75; reporte por canal mostro TIENDA con 1 venta, 12 unidades, S/ 300.00, precio promedio S/ 25.00 y participacion 100%; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 818 pruebas; `capitalpos-web` `npm test -- --watch=false` paso con 211 pruebas usando Node v24.15.0; puertos 4200, 5096 y 5097 quedaron libres; git status final igual al inicial: `capitalpos-api` limpio, main adelantado 3 commits; `capitalpos-web` solo mantiene `package-lock.json` previo, main adelantado 2 commits; `capitalpos-cpe-api` conserva `ArchivosController.cs` previo, main adelantado 2 commits; observacion UX: antes del registro, POS muestra mayorista estimado pero conserva total visual normal S/ 420.00, conviene recalcular total estimado; observacion UX: corregir texto singular `Faltan 1 unidades`; observacion UX: en Inventario, al cambiar rapido de variante, tarjeta puede mostrar temporalmente variante anterior tras ajuste; recomendacion tecnica: usar `DemoSeed__AdminPassword` como variable efectiva del seed.

### PREC-POS-002 - Pulir total estimado mayorista en POS

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` muestra subtotal, IGV y total estimados con precio mayorista cuando aplica, y usa singular/plural correcto en unidades faltantes.
- Evidencia: se inspecciono el estado real del repo antes de modificar; ultimo commit previo: `395bc6b Mostrar precio mayorista en POS`; `capitalpos-web` tenia solo `package-lock.json` modificado antes del bloque; PREC-POS-001 ya cargaba/cacheaba reglas mayoristas y marcaba `Mayorista estimado`; antes del bloque el total visual seguia usando precio normal; el carrito ahora muestra subtotal, IGV y total estimados con precio mayorista cuando aplica; con 12 unidades, precio normal S/35 y mayorista S/25, total estimado muestra S/300; lineas unitarias usan precio mayorista estimado solo visualmente; presentaciones no alteran total mayorista; productos distintos no mezclan total mayorista; si hay regla 24 alcanzada, usa esa regla visualmente; request `POST /api/ventas` sigue enviando el precio normal/local existente; backend sigue confirmando precio final; texto singular: `Falta 1 unidad para precio mayorista.`; texto plural: `Faltan N unidades para precio mayorista.`; no se modifico `capitalpos-api`; no se modifico `capitalpos-cpe-api`; no hay referencias productivas a `capitalpos-cpe-api`; no hay uso productivo de `X-API-KEY`; `npm test -- --watch=false` paso con 213 pruebas; `npm run build` paso correctamente; warnings SCSS existentes no bloquean; commit `43d5833`; mensaje: `Pulir total estimado mayorista en POS`; `package-lock.json` quedo fuera del commit como cambio previo no relacionado.

### PREC-004 - Validacion rapida UX precio mayorista

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: `/app/ventas` valida visualmente el total estimado mayorista correcto antes de registrar, usa singular/plural correcto y no mezcla productos ni presentaciones para el umbral.
- Evidencia: se validaron visualmente desde Angular las correcciones de UX mayorista; no se registraron ventas; no se modifico codigo ni documentacion; no se hicieron commits ni push; no se levanto `capitalpos-cpe-api` ni SUNAT; base limpia usada: `capitalpos_prec004`; API usada: `http://localhost:5096`; Web usada: `http://127.0.0.1:4200`; Node usado: v24.15.0; usuario: `admin@capitalpos.test`; empresa: CapitalPOS Demo S.A.C.; sede: Tienda Demo; punto de venta: Caja Principal; caja abierta con S/100.00; caja cerrada con S/100.00; diferencia de caja S/0.00; producto: Polo Brooklyn PREC004; SKU: `BROOK-PREC004`; precio normal: S/35.00; categoria: General; marca: Demo; variantes: S / Negro / `PREC004-S-N`, M / Negro / `PREC004-M-N`, L / Blanco / `PREC004-L-B`; regla activa: cantidad minima 12, precio mayorista S/25.00; stock disponible por variante: 20; presentacion de control: CAJ / Caja, factor 2, precio S/70.00, codigo `PREC004-CAJ-2`; con 12 unidades 4 + 5 + 3, las tres lineas permanecieron separadas; con 12 unidades, cada linea mostro `Mayorista estimado`; importes estimados: S/100.00, S/125.00 y S/75.00; mensaje: `Mayorista aplicado: 12+ unidades a S/ 25.00.`; subtotal estimado: S/254.24; IGV estimado: S/45.76; total estimado principal: S/300.00; S/420.00 no aparecio como total principal del carrito; con 11 unidades 4 + 4 + 3, total normal mostrado S/385.00; con 11 unidades, subtotal S/326.26 e IGV S/58.74; mensaje exacto: `Falta 1 unidad para precio mayorista.`; producto distinto no completo el umbral de Brooklyn; Producto Demo aparecio separado con `Sin regla mayorista activa para este producto.`; total combinado con producto distinto: S/395.00; presentacion CAJ mostro `Presentaciones no aplican a mayorista.`; presentacion CAJ no completo el umbral; consumo estimado de presentacion: 2 unidades base; importe presentacion: S/70.00; persistencia: ventas 0; persistencia: detalles 0; stocks sin descuento; caja persistida como cerrada; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 818 pruebas; `capitalpos-web` `npm test -- --watch=false` paso con 213 pruebas; puertos 4200, 5096 y 5097 quedaron libres; git status final igual al inicial: `capitalpos-api` limpio, main adelantado 3 commits; `capitalpos-web` solo mantiene `package-lock.json` previo, main adelantado 3 commits; `capitalpos-cpe-api` conserva `ArchivosController.cs` previo, main adelantado 2 commits; observacion no relacionada: Inventario puede conservar temporalmente en la tarjeta el nombre de la variante consultada anteriormente despues de cambiar y ajustar otra variante, stocks persistidos correctos.

### SYNC-001 - Push controlado de commits pendientes

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web` / `capitalpos-cpe-api`
- Criterio de aceptacion: los commits pendientes quedan sincronizados con `origin/main` en los repos correspondientes, sin incluir cambios locales ajenos ni crear commits nuevos.
- Evidencia: se sincronizaron controladamente los commits pendientes con `origin/main`; no se modificaron archivos; no se stagearon archivos; no se hicieron commits nuevos; no hubo pull, rebase ni merge; no hubo divergencias ni repos bloqueados; `capitalpos-api` push: `36575d5..8289911`; `capitalpos-api` commits enviados: `ab0fcfe Cerrar deuda tecnica de presentaciones`, `34725d2 Agregar precio mayorista por cantidad acumulada`, `8289911 Exponer reglas de precio mayorista`; `capitalpos-api` quedo `main = origin/main` en `82899118e07f`; `capitalpos-api` quedo ahead/behind 0/0; `capitalpos-api` working tree limpio; `capitalpos-api` sin staged; `capitalpos-web` push: `a554524..3e45430`; `capitalpos-web` commits enviados: `2862517 Gestionar precios mayoristas en productos`, `395bc6b Mostrar precio mayorista en POS`, `43d5833 Pulir total estimado mayorista en POS`, `3e45430 Corregir contexto de variante en inventario`; `capitalpos-web` quedo `main = origin/main` en `3e4543074414`; `capitalpos-web` quedo ahead/behind 0/0; `capitalpos-web` mantiene `package-lock.json` modificado localmente y unstaged; `package-lock.json` no estuvo incluido en el rango enviado; `capitalpos-web` sin staged; `capitalpos-cpe-api/CapitalPos.Cpe` push: `6a1b44d..88aef53`; `capitalpos-cpe-api/CapitalPos.Cpe` commits enviados: `8555d86 fix(cpe): preserve emission data in error responses`, `88aef53 fix(cpe): align UBL invoice for SUNAT beta`; `capitalpos-cpe-api/CapitalPos.Cpe` quedo `main = origin/main` en `88aef5382d1d`; `capitalpos-cpe-api/CapitalPos.Cpe` quedo ahead/behind 0/0; `CapitalPos.Cpe.Api/Controllers/ArchivosController.cs` continua modificado localmente y unstaged; `ArchivosController.cs` no estuvo incluido en el rango enviado; `capitalpos-cpe-api/CapitalPos.Cpe` sin staged; comandos usados: `git status --short`, `git status --branch --short`, `git log --oneline -5`, `git remote -v`, `git diff --cached --name-status`, `git fetch origin main`, `git rev-list --left-right --count origin/main...main`, `git log --oneline --reverse origin/main..main` y `git push origin main`; no se expusieron secretos.

## 19. Roadmap producto listo y canales digitales

Nota:
- Esta seccion funciona como cola viva de trabajo para avanzar de demo validada a sistema listo.
- Cada bloque debe implementarse en tandas pequenas, con inspeccion real del repo antes de modificar.
- No marcar como Completado sin evidencia de pruebas, build, commit cuando aplique y estado Git.
- Prioridad sugerida: primero cerrar ventas/pagos/caja base; luego pedidos digitales; luego integraciones externas.

### CMP-001 - Entidad y endpoints de compras en capitalpos-api

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: `capitalpos-api` modela Compra/CompraDetalle, incrementa stock por sede, registra kardex de ingreso y expone endpoints multi-tenant de compras.
- Evidencia: commit `0d35a02`; mensaje: `feat: modulo core de compras e ingreso de stock con movimiento de kardex`; se crearon dominio `Compra` y `CompraDetalle`; se creo `POST /api/compras`, `GET /api/compras` y `GET /api/compras/{id}`; endpoints requieren JWT, `X-CapitalPos-EmpresaId`, `EmpresaActivaEndpointFilter` y `PermisoEmpresa.OperarAlmacen`; ingreso incrementa stock operativo por sede y registra movimiento de inventario de compra; migracion `20260807155402_AgregarCompras`; no se modifico `capitalpos-cpe-api`.

### CMP-002 - Pantalla Angular de ingreso de stock

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/compras` deja de ser placeholder y permite registrar ingreso de mercaderia con formulario reactivo, detalles de producto/variante y listado de compras.
- Evidencia: commit `69214ef`; mensaje: `feat: interfaz de ingreso de stock con formulario reactivo y servicio de compras`; se creo `ComprasApiService` y modelos TypeScript; se reemplazo el placeholder de `/app/compras` por `ComprasPageComponent`; sidebar mantiene la entrada operativa; no hay uso productivo de `X-API-KEY`; no hay referencias productivas a `capitalpos-cpe-api`.

### DASH-003 - Dashboard de reportes por canales

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: `GET /api/dashboard/reporte-canales` agrupa monto facturado y cantidad de transacciones del dia por `CanalVenta` (incluyendo canales en cero), y `/app/dashboard` muestra la comparativa visual con barras HTML/CSS nativas.
- Evidencia: se implemento `DashboardReporteCanalesUseCase`; endpoint `GET /api/dashboard/reporte-canales` protegido con JWT, empresa activa y `PermisoEmpresa.OperarVentas`; considera solo `EstadoVenta.Registrada`; calcula el dia con `America/Lima` reutilizando `IDashboardComercialClock`; incluye todos los valores de `CanalVenta` aunque esten en cero; Angular agrega `obtenerReporteCanales`, modelo alineado y seccion visual en `/app/dashboard` con barras HTML/CSS; sin librerias de graficos externas; `capitalpos-api` `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores; `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 894 pruebas; `capitalpos-web` `npm run build` paso correctamente; `npm test -- --watch=false` paso con 235 pruebas; warnings SCSS de presupuesto conocidos no bloquean.

### VTA-003 - Historial y detalle de ventas

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: consultar ventas registradas por fecha/filtros y ver detalle completo con sede, punto de venta, canal, productos, variantes, presentaciones, mayorista aplicado, totales y estado.
- Evidencia: se implemento historial y detalle de ventas sin migraciones; se inspecciono el estado real antes de modificar; `capitalpos-api` estaba limpio y solo exponia creacion de venta y emision CPE; `Venta` y `VentaDetalle` ya contenian los campos requeridos; `IVentaRepository` ya aplicaba aislamiento por `EmpresaId` e incluia detalles; `capitalpos-web` solo tenia el POS en `/app/ventas`; `package-lock.json` ya estaba modificado antes de la tarea y no fue tocado; `capitalpos-cpe-api` conservo su modificacion previa en `ArchivosController.cs`; backend creo `GET /api/ventas`; `GET /api/ventas` soporta filtros `desde`, `hasta`, `canalVenta`, `sedeId` y `puntoVentaId`; fechas de filtros se interpretan en zona Lima; `GET /api/ventas` devuelve resumen, cantidad de lineas y unidades comerciales; backend creo `GET /api/ventas/{ventaId}`; `GET /api/ventas/{ventaId}` devuelve cabecera, totales y lineas separadas; detalle incluye variante, presentacion, factor, cantidad base y mayorista; ambos endpoints requieren JWT; ambos endpoints requieren empresa activa; ambos endpoints exigen `PermisoEmpresa.OperarVentas`; venta de otra empresa devuelve 404; se agregaron `ListarVentasUseCase` y `ObtenerVentaDetalleUseCase`; se actualizaron `VentaEndpoints` y `Program.cs`; se agregaron pruebas `ApplicationHistorialVentasTests`; se actualizaron pruebas HTTP y estructura de permisos; frontend creo `/app/ventas/historial`; `/app/ventas/historial` es accesible desde `Ver historial` en POS; filtros Desde/Hasta usan fecha Lima; canal funciona como filtro principal; sede y punto de venta quedan como filtros avanzados; tabla muestra fecha, canal, sede/punto, estado, total y unidades; detalle se despliega en la misma pantalla; detalle muestra variante, presentacion, factor, cantidad base y mayorista; maneja estados cargando, error y sin datos; no se implemento anulacion; no se implementaron metodos de pago; no se modifico `capitalpos-cpe-api`; no se modifico `package-lock.json`; no se crearon migraciones; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 823 pruebas; `capitalpos-api` `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `capitalpos-web` `npm test -- --watch=false` paso con 222 pruebas; `capitalpos-web` `npm run build` paso correctamente; warnings SCSS conocidos no bloquean; `git diff --check` limpio en API y web; commit API: `0f4bcc13fe3d0f4a661e4fc2e0a2b935b944495e`; mensaje API: `Agregar historial y detalle de ventas`; commit web: `71f2192a3bb838f8ed84a06fb28eee21d4237968`; mensaje web: `Agregar historial de ventas en Angular`; `capitalpos-api` quedo main ahead 1; `capitalpos-web` quedo main ahead 1 y `package-lock.json` sin stage; `capitalpos-cpe-api` no fue tocado y conserva `ArchivosController.cs` previo sin stage; no se hizo push, pull, rebase ni merge.

### PAG-001 - Metodos de pago manuales en venta

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: registrar metodo(s) de pago manuales en la venta, como efectivo, Yape, tarjeta, transferencia u otro, sin integracion automatica todavia.
- Evidencia: se implemento registro manual de pagos sin cambios en CPE; se eligio `VentaPago` porque sigue la convencion de `VentaDetalle` como entidad hija del agregado `Venta`; `VentaPago` incluye `Id`, `EmpresaId`, `VentaId`, `MetodoPago`, `Monto`, `CodigoOperacion`, `Observacion` y `FechaCreacion`; `MetodoPago` soporta EFECTIVO, YAPE, TARJETA, TRANSFERENCIA y OTRO; compatibilidad: si pagos se omite o llega vacio, se crea automaticamente un pago EFECTIVO por el total; `POST /api/ventas` acepta pagos; cada monto debe ser mayor que cero; la suma de pagos debe coincidir exactamente con el total confirmado por backend; metodo invalido devuelve error claro; validacion de pagos ocurre antes de modificar stock; si falla validacion de pagos, no persiste venta ni descuenta stock; venta, detalles, pagos y stock se guardan mediante el mismo `DbContext` y `SaveChanges`; pagos estan incluidos en respuesta de creacion, historial y detalle; consultas siguen filtradas por `EmpresaId`; se creo migracion `20260725172008_AgregarPagosVenta`; se actualizaron snapshot EF, `Venta`, configuracion EF, repositorio y casos de uso de creacion/historial; `/app/ventas` agrega pago principal EFECTIVO por el total estimado; `/app/ventas` actualiza automaticamente el monto cuando cambia el carrito; `/app/ventas` permite seleccionar metodo de pago; `/app/ventas` permite monto, codigo de operacion y observacion; `/app/ventas` soporta pagos mixtos simples; `/app/ventas` muestra total pagado y diferencia; errores backend de pagos conservan carrito y formulario de pagos; `/app/ventas/historial` muestra metodo, monto, codigo de operacion y observacion; no se agrego integracion automatica con Yape, Izipay ni tarjetas; backend cubre compatibilidad sin pagos, efectivo, Yape, tarjeta, efectivo + Yape, suma incorrecta, monto cero/negativo, metodo invalido, stock intacto ante rechazo, pagos en historial/detalle y regresiones de variante/presentacion/mayorista; frontend cubre envio y visualizacion de pagos manuales; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 840 pruebas; `capitalpos-api` `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `capitalpos-web` `npm test -- --watch=false` paso con 225 pruebas; `capitalpos-web` `npm run build` paso correctamente; warnings SCSS conocidos no bloquean; `git diff --check` limpio en ambos repos; sin referencias productivas nuevas a `capitalpos-cpe-api`; sin uso de `X-API-KEY` desde Angular; primera ejecucion backend tuvo falso positivo preexistente por GUID aleatorio con `999`; repeticion completa paso 840/840; commit API: `1807dc332d1213fb2c141dff2bbbce61ef0ec34a`; mensaje API: `Agregar pagos manuales a ventas`; commit web: `bcef1b999c75a6990640a302bd89faa5d3c518d7`; mensaje web: `Agregar pagos manuales en ventas Angular`; `capitalpos-api` quedo main ahead 2, working tree e indice limpios; `capitalpos-web` quedo main ahead 2 y `package-lock.json` sin stage; `capitalpos-cpe-api` no fue tocado y conserva `ArchivosController.cs` previo sin stage; no se hizo push, pull, rebase ni merge.

### CAJA-005 - Resumen operativo de caja con pagos

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: caja muestra resumen operativo por metodo de pago, total esperado, total declarado y diferencia, usando ventas/pagos de la sesion.
- Evidencia: se implemento resumen operativo de caja con pagos sin migraciones; `Venta` no tiene `SesionCajaId`; `Venta` conserva `PuntoVentaId`, `FechaCreacion`, estado y pagos; `SesionCaja` conserva apertura, cierre y punto de venta; resumen se deriva por empresa activa, mismo punto de venta e intervalo de apertura/cierre; para caja abierta usa `FechaCreacion >= FechaApertura`; para caja cerrada usa `FechaCreacion >= FechaApertura` y `FechaCreacion <= FechaCierre`; considera unicamente `EstadoVenta.Registrada`; excluye ventas anuladas, ajenas, de otros puntos y fuera del intervalo; `DiferenciaOperativa` se calcula como `MontoDeclaradoCierre - MontoInicial - TotalPagado`; para cajas abiertas `DiferenciaOperativa` devuelve null; se creo `GET /api/caja/sesiones/{sesionCajaId}/resumen`; endpoint requiere JWT; endpoint requiere empresa activa mediante header; endpoint usa `EmpresaActivaEndpointFilter`; endpoint exige `PermisoEmpresa.OperarVentas`; caja ajena o inexistente devuelve 404; response incluye datos de sesion y estado; response incluye montos de apertura/cierre; response incluye `totalVentas`; response incluye `cantidadVentas`; response incluye `totalPagado`; response incluye `diferenciaOperativa`; response incluye `pagosPorMetodo` con metodo, total y cantidad; se creo `ObtenerResumenSesionCajaUseCase`; se actualizaron `CajaEndpoints` y `Program.cs`; se agregaron pruebas `ApplicationSesionCajaTests`; se actualizaron pruebas HTTP y estructura de permisos; `/app/ventas` bloque Caja muestra total y cantidad de ventas; `/app/ventas` bloque Caja muestra total pagado; `/app/ventas` bloque Caja muestra pagos agrupados por metodo; `/app/ventas` bloque Caja muestra monto inicial y declarado; `/app/ventas` bloque Caja muestra diferencia de cierre; `/app/ventas` bloque Caja muestra diferencia operativa; `/app/ventas` maneja estados cargando, error y sin ventas para resumen; `/app/ventas` permite actualizacion manual del resumen; resumen se refresca al detectar caja abierta, despues de abrir caja, despues de registrar venta y despues de cerrar caja; errores del resumen se manejan independientemente y no interrumpen venta; no se modifico `capitalpos-cpe-api`; no se modifico `package-lock.json`; no se crearon migraciones; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 846 pruebas; `capitalpos-api` `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores y 0 warnings; `capitalpos-web` `npm test -- --watch=false` paso con 229 pruebas; `capitalpos-web` `npm run build` paso correctamente; warnings SCSS conocidos no bloquean; `git diff --check` limpio; sin referencias productivas Angular a `capitalpos-cpe-api`; sin uso de `X-API-KEY`; commit API: `c0119fdd6bbd6f486dc64e7af061c26d83120e15`; mensaje API: `Agregar resumen operativo de caja`; commit web: `fa94199cdd84e835ab676ab92d8b3c5fc53e1ee2`; mensaje web: `Mostrar resumen operativo de caja en ventas`; `capitalpos-api` quedo main ahead 3, working tree e indice limpios; `capitalpos-web` quedo main ahead 3 y `package-lock.json` sin stage; `capitalpos-cpe-api` no fue tocado y conserva `ArchivosController.cs` previo sin stage; no se hizo push, pull, rebase ni merge.

### VTA-004 - Anular venta con reversa de stock

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: anular una venta registrada revierte stock de forma atomica, respeta empresa/sede/caja y deja trazabilidad sin borrar la venta original.
- Evidencia: se implemento anulacion de venta con reversa de stock; `Venta` ahora persiste `FechaAnulacion`; `Venta` ahora persiste `ObservacionAnulacion`; se creo migracion `20260726191823_AgregarDatosAnulacionVenta`; se creo `POST /api/ventas/{id}/anular`; endpoint protegido con JWT, empresa activa, `EmpresaActivaEndpointFilter` y `PermisoEmpresa.OperarVentas`; venta ajena o inexistente devuelve 404; venta ya anulada falla con error claro; anulacion cambia `EstadoVenta` a `Anulada`; anulacion conserva venta, detalles y pagos; reversa usa `SedeId` de la venta; reversa usa `ProductoVarianteId` si existe; reversa devuelve stock base cuando `ProductoVarianteId` es null; reversa usa `VentaDetalle.CantidadBaseDescontada`; reversa no recalcula factor desde presentacion; venta con comprobante registrado queda bloqueada y requiere nota de credito; historial/detalle devuelven estado y datos de anulacion; CAJA-005 continua excluyendo explicitamente `EstadoVenta.Anulada`; Angular permite anular solo ventas registradas desde `/app/ventas/historial`; Angular permite ingresar observacion opcional; Angular muestra resultado/error de anulacion; Angular refresca lista y detalle despues de anular; pagos permanecen visibles como historico; no se implemento devolucion parcial; no se implemento nota de credito; no se modifico `capitalpos-cpe-api`; no se modifico `package-lock.json`; backend build paso con 0 warnings y 0 errors; pruebas focalizadas de anulacion pasaron 2/2; `dotnet test` completo inicio ejecucion y no reporto fallos, pero el resumen final no quedo visible por limite de consola; frontend `npm test -- --watch=false` paso con 231 pruebas en 28 archivos; frontend `npm run build` paso correctamente; warnings SCSS conocidos no bloquean; `git diff --check` correcto; commit API: `623db44`; mensaje API: `Anular ventas con reversa de stock`; commit web: `e716e76`; mensaje web: `Agregar anulacion de ventas en historial`; `capitalpos-api` quedo limpio, main ahead 4; `capitalpos-web` quedo main ahead 4 y solo mantiene `package-lock.json` previo sin stage; `capitalpos-cpe-api` no fue tocado y conserva `ArchivosController.cs` previo sin stage; no se hizo push.

### INV-010 - Kardex basico de inventario

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: registrar movimientos de inventario por ajuste, venta, anulacion, reserva y liberacion, con producto, variante, sede, cantidad, usuario, fecha y referencia.
- Evidencia: se implemento Kardex basico de inventario; se creo entidad `MovimientoInventario`; se creo enum `TipoMovimientoInventario`; se creo `IMovimientoInventarioRepository`; se creo `EfMovimientoInventarioRepository`; se agrego configuracion EF; se agrego `DbSet` en `CapitalPosDbContext`; se actualizo `DependencyInjection`; se creo migracion `20260726193020_AgregarMovimientosInventario`; se actualizo snapshot EF; se registra movimiento AJUSTE al ajustar stock; se registra movimiento VENTA al registrar venta; se registra movimiento ANULACION_VENTA al anular venta; venta por presentacion usa `VentaDetalle.CantidadBaseDescontada`; Kardex conserva `stockAnterior` y `stockPosterior`; si venta falla, no genera movimiento; se creo `ListarKardexUseCase`; se creo `GET /api/stock/kardex`; endpoint protegido por auth, empresa activa y `PermisoEmpresa.OperarAlmacen`; endpoint filtra por fechas, sede, producto y variante; endpoint no fuga movimientos de otra empresa; se creo ruta Angular `/app/inventario/kardex`; se agrego acceso visible desde Inventario; se creo servicio Angular de Kardex; se creo pagina de Kardex con filtros, tabla y estados basicos; no se modifico `capitalpos-cpe-api`; no se modifico `package-lock.json`; no se instalaron paquetes; `capitalpos-api` `dotnet test CapitalPos.Api.sln -m:1 -nr:false` paso con 852 pruebas; `capitalpos-api` `dotnet build CapitalPos.Api.sln -m:1 -nr:false` paso con 0 errores; `capitalpos-web` `npm test -- --watch=false` paso con 233 pruebas; `capitalpos-web` `npm run build` paso correctamente; warnings SCSS conocidos no bloquean; `git diff --check` correcto; commit API: `2c41d10`; mensaje API: `Agregar kardex basico de inventario`; commit web: `ee391a6`; mensaje web: `Agregar vista de kardex en inventario`; `capitalpos-api` quedo limpio, main ahead 5; `capitalpos-web` quedo main ahead 5 y solo mantiene `package-lock.json` previo sin stage; `capitalpos-cpe-api` no fue tocado y conserva `ArchivosController.cs` previo sin stage; no se hizo push, pull, rebase ni merge.

### SYNC-002 - Push controlado de commits pendientes

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web` / `capitalpos-cpe-api`
- Criterio de aceptacion: los commits pendientes de API y Web quedan sincronizados con `origin/main`, sin incluir cambios locales ajenos ni tocar CPE.
- Evidencia: SYNC-002 verificado y cerrado; `capitalpos-api` quedo `main = origin/main`, ahead/behind 0/0; `capitalpos-web` quedo `main = origin/main`, ahead/behind 0/0; `capitalpos-cpe-api/CapitalPos.Cpe` sigue 0/0 y no se empujo porque no tenia commits pendientes; commits API sincronizados: `0f4bcc1 Agregar historial y detalle de ventas`, `1807dc3 Agregar pagos manuales a ventas`, `c0119fd Agregar resumen operativo de caja`, `623db44 Anular ventas con reversa de stock`, `2c41d10 Agregar kardex basico de inventario`; commits Web sincronizados: `71f2192 Agregar historial de ventas en Angular`, `bcef1b9 Agregar pagos manuales en ventas Angular`, `fa94199 Mostrar resumen operativo de caja en ventas`, `e716e76 Agregar anulacion de ventas en historial`, `ee391a6 Agregar vista de kardex en inventario`; `package-lock.json` sigue modificado localmente en `capitalpos-web`, sin stage; `CapitalPos.Cpe.Api/Controllers/ArchivosController.cs` sigue modificado localmente en CPE, sin stage; no hubo bloqueos; no se modificaron archivos; no se hicieron commits; no se hizo stage; no se hizo pull, rebase ni merge.

### PED-001 - Modelar pedidos digitales y subastas

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: crear modelo de pedido digital para Facebook, WhatsApp, Instagram, ecommerce, subastas u otro canal, con cliente, canal, detalles, importes, estado y trazabilidad base.
- Evidencia: dominio `PedidoDigital`, `PedidoDigitalDetalle`, `PedidoDigitalHistorialEstado`, enums `CanalPedidoDigital` y `EstadoPedidoDigital`; migracion `AgregarPedidosDigitales`; use cases `CrearPedidoDigitalUseCase` y `ObtenerPedidoDigitalUseCase`; endpoints `POST /api/pedidos-digitales` y `GET /api/pedidos-digitales/{id}` con JWT, empresa activa y `PermisoEmpresa.OperarVentas`; creacion registra canal de red social y estado inicial `PendientePago` con historial; pruebas de dominio, application y estructura de permisos verdes.

### PED-002 - Estados operativos de pedido

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: manejar el flujo interno Pendiente de pago, Pagado, Empaquetado, Pendiente de entrega, Entregado y Cancelado, con historial por usuario, fecha y observacion.
- Evidencia: dominio `PedidoDigital.ActualizarEstadoOperativo` con secuencia `PendientePago → Pagado → Empaquetado → PendienteEntrega` y registro en `PedidoDigitalHistorialEstado`; use case `ActualizarEstadoPedidoDigitalUseCase` con filtro multi-tenant `ObtenerPorEmpresaAsync`; endpoint `PUT /api/pedidos-digitales/{id}/estado` con JWT, empresa activa y `PermisoEmpresa.OperarVentas`; bandeja Angular con botones dinamicos por estado (`Marcar como Pagado` / `Empaquetado` / `Pendiente de entrega`) que llaman al servicio HTTP y refrescan la lista; pruebas de dominio, application y estructura de permisos actualizadas.

### PED-003 - Reserva y liberacion de stock por pedido

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: un pedido digital puede reservar stock por sede/producto/variante, liberar reserva al cancelar y descontar stock al convertir en venta o entregar segun regla definida.
- Evidencia: al crear pedido digital, `CrearPedidoDigitalUseCase` valida stock libre por sede/producto/variante, llama `StockProducto.Reservar` (disminuye `CantidadLibre` y aumenta `CantidadReservada` sin bajar `CantidadDisponible`), registra `MovimientoInventario` tipo `RESERVA` con referencia `PEDIDO_DIGITAL`, y falla sin persistir pedido si el stock libre es insuficiente; al cancelar, `CancelarPedidoDigitalUseCase` llama `LiberarReserva` y registra `LIBERACION_RESERVA`; al convertir a venta, `ConvertirPedidoDigitalAVentaUseCase` llama `ConfirmarReserva` (baja `CantidadReservada` y `CantidadDisponible`), crea `Venta` canal `MARKETING` con caja abierta obligatoria, marca pedido `Entregado` y registra kardex `VENTA`; pruebas de dominio y application verdes.

### PED-WEB-001 - Gestion de pedidos digitales desde Angular

- Prioridad: Alta
- Estado: Completado
- Proyecto principal: `capitalpos-web`
- Criterio de aceptacion: `/app/pedidos-digitales` permite crear, listar, filtrar y cambiar estados de pedidos digitales/subastas con vista operativa simple.
- Evidencia: ruta `/app/pedidos-digitales` con `BandejaPedidosDigitalesPageComponent` (listar/filtrar por estado/canal/sede), modal `CrearPedidoDigitalDialogComponent`, acciones de cancelar y convertir a venta, y botones dinamicos de estados intermedios (`Marcar como Pagado` / `Empaquetado` / `Pendiente de entrega`) via `PedidosDigitalesApiService.actualizarEstado` hacia `PUT /api/pedidos-digitales/{id}/estado`.

### PAG-002 - Bandeja de pagos notificados Izipay/Yape

- Prioridad: Media
- Estado: Pendiente
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: recibir o registrar notificaciones de pago externas y mostrarlas en una bandeja para revision, sin exigir asociacion automatica desde el inicio.

### PAG-003 - Asociar pago a pedido o venta

- Prioridad: Media
- Estado: Pendiente
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: la cajera puede asociar un pago notificado o manual a un pedido/venta, marcarlo como confirmado y dejar historial.

### PAG-004 - Cobro integrado desde POS con proveedor externo

- Prioridad: Baja
- Estado: Pendiente
- Proyecto principal: `capitalpos-api` / `capitalpos-web`
- Criterio de aceptacion: desde POS se puede iniciar cobro con proveedor externo, esperar respuesta aprobada/rechazada y registrar la venta solo cuando el pago sea aprobado.

### INT-001 - Modelo de integracion WooCommerce

- Prioridad: Media
- Estado: Pendiente
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: configurar por empresa una integracion WooCommerce con URL, credenciales seguras, sede origen, modo de sincronizacion y mapeo de producto/variante.

### INT-002 - Sincronizar stock CapitalPOS hacia WooCommerce

- Prioridad: Media
- Estado: Pendiente
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: publicar hacia WooCommerce el stock libre de CapitalPOS desde la sede configurada, con historial de sincronizacion, errores y reintentos.

### INT-003 - Importar pedidos WooCommerce como pedidos digitales

- Prioridad: Baja
- Estado: Pendiente
- Proyecto principal: `capitalpos-api`
- Criterio de aceptacion: importar pedidos WooCommerce y registrarlos como pedidos digitales para pago, preparacion, entrega y conversion a venta.
