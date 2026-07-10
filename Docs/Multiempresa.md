# Estandar multiempresa para entidades POS

Este documento define la regla tecnica obligatoria para construir futuros
modulos POS SaaS en `capitalpos-api`. Aplica antes de crear productos, ventas,
inventario, caja, compras, reportes o configuracion fiscal por empresa.

## Principio obligatorio

Toda entidad POS transaccional o configurable por empresa debe tener
`EmpresaId` obligatorio.

La empresa activa se obtiene del contexto validado por
`X-CapitalPos-EmpresaId`, `EmpresaActivaEndpointFilter` e
`IEmpresaActivaContext`. Los casos de uso POS deben operar siempre dentro de
esa empresa activa.

## Entidades futuras que deben tener EmpresaId

Deben incluir `EmpresaId` todas las entidades cuyo dato pertenezca a una
empresa, afecte su operacion comercial o configure su comportamiento fiscal y
operativo:

- productos;
- categorias;
- almacenes;
- stock;
- ventas;
- comprobantes;
- caja;
- compras;
- clientes y proveedores cuando apliquen por empresa;
- series;
- configuracion fiscal;
- reportes materializados.

La regla tambien aplica a tablas de detalle si pueden consultarse o mutarse de
forma independiente. Cuando el detalle solo exista como parte estricta de un
agregado raiz, puede depender del `EmpresaId` del agregado, pero las consultas
deben seguir entrando por el agregado filtrado por empresa activa.

## Entidades que no necesariamente deben tener EmpresaId

No toda entidad del sistema es empresarial. Estas entidades pueden ser globales
si su ciclo de vida pertenece a la plataforma SaaS o a catalogos compartidos:

- usuarios globales;
- credenciales;
- roles de plataforma;
- configuracion global de plataforma;
- auditoria global si aplica;
- catalogos SUNAT globales.

Si una entidad global empieza a almacenar preferencias, permisos, configuracion
o datos operativos de una empresa concreta, esa parte debe modelarse en una
entidad separada con `EmpresaId`.

## Reglas de repositorios y casos de uso

- Nunca consultar datos POS sin empresa activa.
- Filtrar siempre por `EmpresaId` en lecturas, actualizaciones y borrados.
- No aceptar `EmpresaId` libre desde el frontend cuando pueda derivarse de
  `X-CapitalPos-EmpresaId`.
- Validar pertenencia usuario-empresa antes de operar mediante
  `EmpresaActivaEndpointFilter`.
- Los casos de uso POS deben recibir `IEmpresaActivaContext` o un valor de
  empresa derivado de ese contexto, no del payload publico.
- Las creaciones deben asignar `EmpresaId` desde el contexto de empresa activa.
- Las actualizaciones y eliminaciones deben buscar primero el registro por
  `Id` y `EmpresaId`; si no coincide, responder como no encontrado o no
  autorizado sin revelar existencia en otra empresa.
- Los listados deben devolver solo datos de la empresa activa.

## Patron operativo de filtrado por empresa activa

Los futuros modulos POS deben seguir este flujo de extremo a extremo:

```text
Endpoint autenticado
  -> EmpresaActivaEndpointFilter
  -> RequirePermisoEmpresa
  -> Use case con IEmpresaActivaContext.EmpresaId
  -> Repository con metodos por empresa
  -> EF Core con filtro EmpresaId
```

### Endpoint

- Todo endpoint POS transaccional debe estar en un grupo con
  `.RequireAuthorization()` y `.AddEndpointFilter<EmpresaActivaEndpointFilter>()`.
- Cada operacion debe declarar permiso con `RequirePermisoEmpresa(...)`.
- El frontend envia `X-CapitalPos-EmpresaId`, pero el backend valida que el
  usuario autenticado pertenezca a esa empresa antes de ejecutar la operacion.
- Los permisos se evaluan despues de establecer empresa activa. El filtro de
  permisos debe encontrar `IEmpresaActivaContext.TieneEmpresaActiva = true`;
  si no existe empresa activa validada, la operacion no debe continuar.
- El endpoint no debe mapear un `EmpresaId` del payload hacia el caso de uso
  para decidir alcance. Si el payload trae `EmpresaId`, debe ignorarse o
  rechazarse segun el contrato del endpoint.

Ejemplo de forma esperada para un endpoint POS futuro:

```csharp
var group = app.MapGroup("/api/productos")
    .RequireAuthorization()
    .AddEndpointFilter<EmpresaActivaEndpointFilter>();

group.MapGet("/", ListarProductosAsync)
    .RequirePermisoEmpresa(PermisoEmpresa.ConsultarProductos);
```

### Use case

- El caso de uso debe recibir `IEmpresaActivaContext` o un contexto seguro
  construido desde este.
- Debe fallar si `IEmpresaActivaContext.TieneEmpresaActiva` es `false`.
- Debe usar `IEmpresaActivaContext.EmpresaId` como unica fuente de alcance
  empresarial.
- No debe confiar en `EmpresaId` del payload publico.
- En creaciones, debe construir la entidad asignando `EmpresaId` desde el
  contexto.
- En lecturas, actualizaciones y eliminaciones, debe llamar al repositorio con
  `empresaId` y `id` cuando aplique.

Ejemplo de forma esperada:

```csharp
public Task<IReadOnlyCollection<Producto>> EjecutarAsync(CancellationToken cancellationToken)
{
    if (!_empresaActiva.TieneEmpresaActiva)
    {
        throw new InvalidOperationException("La empresa activa es obligatoria.");
    }

    return _productoRepository.ListarPorEmpresaAsync(
        _empresaActiva.EmpresaId,
        cancellationToken);
}
```

### Repository

- Los repositorios POS deben exponer metodos por empresa, por ejemplo:
  - `ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken)`;
  - `ObtenerPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken)`;
  - `ExisteCodigoAsync(Guid empresaId, string codigo, CancellationToken cancellationToken)`.
- `GetById` debe incluir `EmpresaId`; evitar metodos POS publicos del tipo
  `ObtenerPorIdAsync(Guid id)` porque permiten acceso cruzado accidental.
- Toda consulta debe incluir predicado por `EmpresaId`.
- Una busqueda con `id` valido pero `empresaId` incorrecto debe devolver `null`
  o una respuesta equivalente a no autorizado/no encontrado, sin revelar que el
  dato existe en otra empresa.
- Las escrituras deben confirmar que el registro pertenece a la empresa activa
  antes de modificarlo.

Ejemplo de consulta esperada:

```csharp
return _dbContext.Productos
    .AsNoTracking()
    .Where(producto => producto.EmpresaId == empresaId)
    .ToListAsync(cancellationToken);
```

## Reglas EF Core

Para toda entidad POS con alcance empresarial:

- `EmpresaId` debe ser `Guid` obligatorio.
- Debe existir FK hacia `Empresa`.
- Debe existir indice por `EmpresaId`.
- Los indices unicos de negocio deben ser compuestos por `EmpresaId` cuando el
  valor pueda repetirse entre empresas. Ejemplos:
  - producto: `EmpresaId + Codigo`;
  - serie: `EmpresaId + TipoComprobante + Serie`;
  - almacen: `EmpresaId + Codigo`.
- `DeleteBehavior.Restrict` es el comportamiento recomendado para FK hacia
  `Empresa`, para evitar borrados en cascada de datos transaccionales.
- Nombres de tablas: plural en snake_case, como `productos`, `ventas` o
  `movimientos_stock`.
- Nombres de columnas: snake_case segun la convencion de migraciones; la
  propiedad de dominio debe llamarse `EmpresaId`.
- Las migraciones deben revisar que no existan indices unicos globales sobre
  datos que deban ser unicos solo dentro de una empresa.

## Reglas de endpoints

- Todo endpoint transaccional POS debe requerir autenticacion.
- Todo endpoint transaccional POS debe usar `EmpresaActivaEndpointFilter`.
- Todo endpoint transaccional POS debe requerir permiso de empresa explicito
  mediante `RequirePermisoEmpresa`.
- Los endpoints globales deben separarse de los endpoints por empresa.
- Los endpoints globales solo deben estar disponibles para roles de plataforma
  cuando esos roles existan.
- Los roles de plataforma SaaS y los roles dentro de una empresa son conceptos
  diferentes. No usar `RolEmpresa` para autorizar administracion global de la
  plataforma.
- El payload publico no debe permitir operar sobre otra empresa enviando un
  `EmpresaId` distinto al de `X-CapitalPos-EmpresaId`.

## Roles de plataforma vs roles de empresa

CapitalPOS debe separar dos niveles de autorizacion:

### Rol de plataforma SaaS

Un rol de plataforma opera sobre el SaaS completo. Sirve para tareas de
administracion global y no representa pertenencia a una empresa concreta.

Puede incluir capacidades como:

- crear, activar o desactivar empresas;
- ver metricas globales de la plataforma;
- operar configuracion global del SaaS;
- administrar catalogos globales;
- ejecutar soporte tecnico o acciones operativas globales.

Las acciones globales:

- no deben depender de `X-CapitalPos-EmpresaId`;
- no deben depender de `UsuarioEmpresa`;
- no deben usar `RolEmpresa`;
- no deben protegerse con `PermisoEmpresa`;
- deben usar permisos de plataforma separados cuando se implemente ese nivel,
  por ejemplo `PermisoPlataforma` o una convencion equivalente.

### Rol de empresa

Un rol de empresa opera dentro de una empresa activa. En el modelo actual esta
representado por `RolEmpresa` y sus permisos se expresan con `PermisoEmpresa`.

Depende de:

- una relacion `UsuarioEmpresa` activa;
- el header `X-CapitalPos-EmpresaId`;
- `EmpresaActivaEndpointFilter`;
- `IEmpresaActivaContext`;
- permisos como ventas, caja, inventario, CPE y usuarios de empresa.

Los permisos de empresa solo autorizan operaciones dentro de la empresa activa.
No deben habilitar administracion global SaaS.

### Reglas de separacion

- `RolEmpresa.Administrador` significa administrador dentro de una empresa, no
  superadministrador de plataforma.
- `PermisoEmpresa` no debe incluir permisos de administracion global SaaS.
- Los endpoints globales no deben colgar del flujo
  `EmpresaActivaEndpointFilter` si realmente operan sobre toda la plataforma.
- Los endpoints por empresa si deben requerir empresa activa y permisos de
  empresa.
- Las rutas deben distinguir claramente alcance de plataforma y alcance de
  empresa. Convenciones recomendadas:
  - plataforma: `/api/plataforma/...`;
  - empresa activa: `/api/...` o `/api/empresas/actual/...`, siempre con
    `X-CapitalPos-EmpresaId`.

### Implicancias para endpoints actuales

Los endpoints actuales mezclan parte del lenguaje global con el flujo de
empresa activa. No se cambia comportamiento en esta tarea, pero API-011 debe
revisarlos:

- `/api/empresas`:
  - listar, crear, activar y desactivar empresas parecen operaciones de
    plataforma SaaS;
  - hoy usan `EmpresaActivaEndpointFilter` y `PermisoEmpresa`;
  - API-011 debe decidir si se mueven a un grupo de plataforma o si se separa
    un endpoint de empresa activa para consultar solo la empresa actual.
- `/api/usuarios`:
  - los usuarios son identidades globales;
  - listarlos o activarlos globalmente puede ser operacion de plataforma;
  - administrar usuarios dentro de una empresa debe limitarse a usuarios
    asociados a la empresa activa;
  - API-011 debe separar administracion global de identidad y gestion de
    usuarios por empresa.
- `/api/usuarios-empresas`:
  - gestiona relaciones entre usuario y empresa;
  - puede ser una operacion por empresa cuando solo afecta la empresa activa;
  - puede ser una operacion de plataforma si asigna usuarios a cualquier
    empresa;
  - API-011 debe exigir que las operaciones por empresa usen
    `IEmpresaActivaContext.EmpresaId` y no acepten `EmpresaId` libre para
    modificar otra empresa.

### Clasificacion API-011 de endpoints actuales

Esta tabla clasifica el comportamiento recomendado. No cambia el estado actual
del codigo; sirve como plan de ajuste para separar plataforma global y empresa
activa.

| Endpoint | Clasificacion recomendada | Riesgo actual | Cambio recomendado futuro | EmpresaActivaEndpointFilter | Permisos |
| --- | --- | --- | --- | --- | --- |
| `GET /api/empresas` | Plataforma global | Lista empresas desde un flujo que hoy requiere empresa activa y `PermisoEmpresa.ConsultarEmpresa`; mezcla administracion SaaS con contexto empresarial. | Mover a `/api/plataforma/empresas` con permiso de plataforma, o crear un endpoint separado para consultar solo la empresa activa. | No para listado global; si es empresa activa, si. | Plataforma para global; empresa solo para consultar empresa activa. |
| `GET /api/empresas/{id}` | Mixto o requiere rediseño | Permite consultar una empresa arbitraria usando permiso de empresa activa, lo que puede exponer datos de otra empresa. | Separar `GET /api/plataforma/empresas/{id}` y `GET /api/empresas/actual`; en flujo por empresa, ignorar `id` externo y usar `IEmpresaActivaContext.EmpresaId`. | No para plataforma; si para empresa activa. | Plataforma para consulta global; empresa para consulta propia. |
| `POST /api/empresas` | Plataforma global | Crear empresas es alta administracion SaaS, pero hoy depende de empresa activa y `PermisoEmpresa.GestionarEmpresas`. | Mover a endpoint de plataforma protegido por permiso de plataforma. | No. | Plataforma. |
| `PATCH /api/empresas/{id}/activar` | Plataforma global | Activar empresas es operacion SaaS global, pero hoy usa rol de empresa y puede actuar sobre otro tenant por `id`. | Mover a endpoint de plataforma; auditar sin requerir empresa activa. | No. | Plataforma. |
| `PATCH /api/empresas/{id}/desactivar` | Plataforma global | Desactivar empresas es operacion SaaS global, pero hoy usa rol de empresa y puede afectar otro tenant por `id`. | Mover a endpoint de plataforma; bloquear autodesactivacion accidental si aplica. | No. | Plataforma. |
| `GET /api/usuarios` | Mixto o requiere rediseño | Lista usuarios globales desde flujo de empresa activa; puede mezclar identidad global con usuarios asociados a una empresa. | Separar listado global de plataforma y listado de usuarios de la empresa activa filtrado por `UsuarioEmpresa.EmpresaId`. | No para global; si para empresa activa. | Plataforma para global; empresa para usuarios de la empresa activa. |
| `GET /api/usuarios/{id}` | Mixto o requiere rediseño | Obtiene usuario global por `id` usando permiso de empresa; puede revelar usuarios no asociados a la empresa activa. | En flujo por empresa, obtener solo si el usuario esta asociado a `IEmpresaActivaContext.EmpresaId`; global queda en plataforma. | No para global; si para empresa activa. | Plataforma para global; empresa para usuarios asociados a la empresa activa. |
| `POST /api/usuarios` | Mixto o requiere rediseño | Crear identidad puede ser global, pero crear/invitar usuario para una empresa debe asociarlo a la empresa activa; hoy no queda separado. | Separar creacion global de identidad y alta/invitacion dentro de empresa activa. | No para global; si para invitacion por empresa. | Plataforma para global; empresa para invitacion/asociacion local. |
| `PATCH /api/usuarios/{id}/activar` | Plataforma global | Activa una identidad global usando permiso de empresa activa. | Mover activacion global a plataforma; para empresa activa usar activar relacion `UsuarioEmpresa`. | No. | Plataforma. |
| `PATCH /api/usuarios/{id}/desactivar` | Plataforma global | Desactiva una identidad global usando permiso de empresa activa; puede afectar acceso a otras empresas. | Mover desactivacion global a plataforma; para empresa activa usar desactivar relacion `UsuarioEmpresa`. | No. | Plataforma. |
| `GET /api/usuarios-empresas` | Empresa activa | Lista relaciones sin filtro por empresa activa en el caso de uso actual; riesgo de fuga entre empresas. | Filtrar por `IEmpresaActivaContext.EmpresaId` y devolver solo relaciones de la empresa activa. | Si. | Empresa. |
| `GET /api/usuarios-empresas/{id}` | Empresa activa | Busca relacion por `id` sin incluir `EmpresaId`; puede consultar relacion de otra empresa. | Buscar por `id` y `IEmpresaActivaContext.EmpresaId`; si no coincide, devolver `404` o equivalente seguro. | Si. | Empresa. |
| `POST /api/usuarios-empresas` | Empresa activa | El request actual incluye `EmpresaId`; permite intentar asignar usuarios a otra empresa desde payload. | Derivar `EmpresaId` desde `IEmpresaActivaContext.EmpresaId`; ignorar o rechazar `EmpresaId` del payload. | Si. | Empresa. |
| `PATCH /api/usuarios-empresas/{id}/activar` | Empresa activa | Activa relacion por `id` sin confirmar empresa activa; puede afectar relacion de otra empresa. | Actualizar solo por `id` y `IEmpresaActivaContext.EmpresaId`. | Si. | Empresa. |
| `PATCH /api/usuarios-empresas/{id}/desactivar` | Empresa activa | Desactiva relacion por `id` sin confirmar empresa activa; puede afectar relacion de otra empresa. | Actualizar solo por `id` y `IEmpresaActivaContext.EmpresaId`. | Si. | Empresa. |
| `PATCH /api/usuarios-empresas/{id}/rol` | Empresa activa | Cambia rol por `id` sin confirmar empresa activa; puede modificar rol de usuario en otra empresa. | Cambiar rol solo por `id` y `IEmpresaActivaContext.EmpresaId`. | Si. | Empresa. |

Prioridad recomendada para correccion:

1. `PATCH /api/empresas/{id}/desactivar`, `PATCH /api/empresas/{id}/activar` y
   `POST /api/empresas`, porque son operaciones globales SaaS con impacto alto.
2. `PATCH /api/usuarios/{id}/desactivar` y
   `PATCH /api/usuarios/{id}/activar`, porque afectan identidades globales.
3. `POST /api/usuarios-empresas` y los `PATCH /api/usuarios-empresas/{id}/*`,
   porque deben quedar filtrados por empresa activa y no por `EmpresaId` libre.
4. Listados y detalles (`GET`) para eliminar fugas de lectura entre empresas.

## Reglas de pruebas

Cada modulo POS nuevo debe incluir pruebas anti-fuga multiempresa:

- un usuario de empresa A no puede leer datos de empresa B;
- un usuario de empresa A no puede modificar datos de empresa B;
- los listados devuelven solo datos de la empresa activa;
- `GetById` con empresa incorrecta devuelve `null`, `404` o equivalente
  seguro;
- las creaciones asignan `EmpresaId` desde el contexto, no desde payload libre;
- payload con `EmpresaId` ajeno se ignora o rechaza;
- los indices unicos permiten repetir codigos entre empresas distintas cuando
  el negocio lo permita;
- los endpoints fallan si falta `X-CapitalPos-EmpresaId`;
- los endpoints fallan si el usuario no pertenece a la empresa activa;
- los endpoints fallan si falta el permiso de empresa requerido.

Las pruebas deben cubrir repositorios/casos de uso y, cuando exista endpoint,
tambien el flujo HTTP.

## Criterios de aceptacion para un modulo POS nuevo

Un modulo POS nuevo solo se considera aceptable si cumple:

- Toda entidad POS transaccional o configurable por empresa tiene `EmpresaId`.
- La configuracion EF define `EmpresaId` obligatorio, FK a `Empresa` e indice.
- Los unicos de negocio son compuestos por `EmpresaId` cuando corresponde.
- Los casos de uso no aceptan `EmpresaId` libre desde frontend para decidir
  alcance operativo.
- Los listados, detalles, actualizaciones y eliminaciones filtran por empresa
  activa.
- Los endpoints usan autenticacion, `EmpresaActivaEndpointFilter` y permisos
  de empresa.
- Existe al menos una prueba anti-fuga que demuestre que empresa A no lee ni
  modifica datos de empresa B.
- La documentacion del modulo indica que sus datos son de alcance empresarial.
