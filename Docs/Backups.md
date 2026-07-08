# Politica de backups

Esta guia define la politica minima de backups de CapitalPOS API para
PostgreSQL productivo. No elige proveedor, no crea jobs, no ejecuta backups
reales y no almacena credenciales.

## Alcance

Los backups productivos deben cubrir:

- base de datos principal de CapitalPOS API;
- metadatos necesarios para restauracion, como identificador del backup,
  fecha UTC, version de PostgreSQL, ambiente, version de la aplicacion y
  migracion aplicada;
- historial de migraciones aplicadas;
- configuracion operativa necesaria para restaurar el servicio, sin incluir
  secretos en claro;
- archivos CPE solo si finalmente se almacenan fuera de la base de datos.

La decision sobre almacenamiento externo de archivos CPE queda pendiente del
diseno de operaciones y del proveedor de despliegue.

## Frecuencia y retencion

Politica minima inicial:

- backups automaticos diarios;
- retencion de corto plazo para recuperaciones recientes;
- retencion semanal y mensual para recuperaciones historicas;
- snapshot o backup previo a migraciones productivas;
- backup manual antes de cambios de alto riesgo.

La retencion exacta se definira al elegir proveedor, considerando costo,
volumen de datos, obligaciones comerciales y tiempo requerido de restauracion.

## Objetivos iniciales

- RPO objetivo inicial: maximo 24 horas de perdida aceptable hasta medir uso
  real y capacidad del proveedor.
- RTO objetivo inicial: restaurar el servicio en maximo 4 horas.
- Tiempo maximo esperado de restauracion inicial: 4 horas, incluyendo restore,
  validacion y pruebas funcionales basicas.

Estos valores deben ajustarse cuando existan datos reales de uso, volumen de
transacciones, requisitos de clientes, tiempos observados de restauracion,
costos y capacidades del proveedor elegido.

## Seguridad

- Cifrar backups en transito y en reposo.
- Restringir acceso a backups por rol operativo.
- Usar credenciales separadas para operacion normal y administracion de
  backups.
- No almacenar secretos en Git.
- No exponer backups publicamente.
- Rotar y revocar accesos periodicamente y ante sospecha de exposicion.
- Registrar quien ejecuta backups, quien restaura y quien autoriza accesos.
- No incluir contrasenas, JWT, API keys, certificados privados ni cadenas de
  conexion en claro dentro de documentacion, logs o plantillas.

## Restauracion

Procedimiento minimo:

1. Restaurar el backup en un ambiente aislado.
2. Validar integridad del backup y consistencia de la base restaurada.
3. Verificar la migracion aplicada y ejecutar migraciones pendientes solo si
   corresponde al plan de recuperacion.
4. Ejecutar pruebas funcionales posteriores.
5. Verificar usuarios, empresas y relaciones `UsuarioEmpresa`.
6. Verificar emision CPE si aplica al incidente o al alcance restaurado.
7. Revisar logs y `X-Correlation-Id` de las pruebas de validacion.
8. Declarar la restauracion exitosa solo cuando el checklist este completo.

## Pruebas periodicas de restauracion

- Ejecutar restauracion de prueba al menos trimestralmente.
- Registrar evidencia de fecha, duracion y resultado.
- Registrar incidencias encontradas durante el restore.
- Definir acciones correctivas y responsable de cierre.
- No considerar valido un backup que nunca haya sido restaurado.

## Responsables

- Responsable de verificar backups: rol operativo designado.
- Responsable de autorizar restauraciones: rol de direccion o continuidad.
- Responsable de revisar fallos: rol tecnico de plataforma o base de datos.
- Escalamiento ante perdida de datos: canal operativo definido durante el
  despliegue productivo.

Los nombres reales de personas, canales y guardias se definiran durante el
despliegue, fuera del repositorio si contienen datos sensibles.

## Alertas minimas

Configurar alertas para:

- backup fallido;
- backup no ejecutado;
- retencion incumplida;
- restauracion de prueba fallida;
- almacenamiento proximo al limite.

## Criterios para elegir proveedor

Cuando se elija proveedor de base de datos o almacenamiento de backups,
evaluar:

- backups automaticos;
- restauracion a punto en el tiempo;
- cifrado;
- retencion configurable;
- restauracion en otra instancia;
- exportacion;
- region;
- SLA;
- costos;
- auditoria de accesos.

## Plantilla segura de checklist

```text
Ambiente: <produccion|staging-restore>
BackupId: <identificador-del-backup>
FechaUtc: <fecha-hora-utc>
MigracionAplicada: <nombre-o-id-de-migracion>
RPOObjetivo: <objetivo-rpo>
RTOObjetivo: <objetivo-rto>
RestoreAislado: <si|no>
IntegridadValidada: <si|no>
UsuariosEmpresasRelacionesValidadas: <si|no>
EmisionCpeValidadaSiAplica: <si|no|no-aplica>
ResultadoRestore: <pendiente|exitoso|fallido>
Incidencias: <resumen-sin-secretos>
AccionesCorrectivas: <resumen-sin-secretos>
ResponsableVerificacion: <rol>
ResponsableAutorizacion: <rol>
```

La plantilla usa placeholders y no debe completarse en Git con valores reales.

## Pendiente para despliegue

Queda pendiente:

- elegir proveedor;
- configurar backups automaticos reales;
- configurar retencion real;
- configurar almacenamiento seguro;
- ejecutar snapshots reales;
- ejecutar restauraciones reales;
- documentar evidencia operativa fuera del repositorio cuando contenga datos
  sensibles.
