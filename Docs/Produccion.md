# Preparación para producción

Esta guía prepara CapitalPOS API para una futura base PostgreSQL administrada.
No elige proveedor, no aprovisiona recursos y no contiene credenciales reales.

## Estrategia aprobada

- Usar PostgreSQL administrado para producción.
- Elegir proveedor más adelante durante el bloque de despliegue.
- Mantener `ConnectionStrings:CapitalPos` vacío en `appsettings.json` y
  `appsettings.Development.json`.
- Inyectar la conexión productiva únicamente con configuración segura del
  ambiente.

## Variable requerida

La API debe recibir la cadena productiva mediante variable de entorno:

```bash
ConnectionStrings__CapitalPos="<cadena-postgresql-productiva-con-tls>"
```

La cadena real debe administrarse en el gestor de secretos del ambiente de
despliegue. No debe almacenarse en Git, `appsettings.json`,
`appsettings.Development.json`, archivos `.env`, documentación ni logs.

## Requisitos de la conexión productiva

- Usar una base exclusiva para producción.
- Usar un usuario exclusivo para la API.
- Exigir TLS/SSL en tránsito.
- Aplicar principio de privilegios mínimos.
- Evitar credenciales compartidas con desarrollo o pruebas.
- Rotar credenciales según la política operativa del proveedor.

El usuario de ejecución de la API debe tener solo los permisos necesarios para
operar la aplicación. Para migraciones, preferir un usuario operativo separado
con permisos elevados temporales.

## Separación de ambientes

- Desarrollo: usar user-secrets o variables locales y una base local o de
  desarrollo.
- Pruebas: usar `CAPITALPOS_TEST_CONNECTION_STRING` con una base exclusiva cuyo
  nombre contenga `test`.
- Producción: usar `ConnectionStrings__CapitalPos` desde el gestor de secretos
  del ambiente y una base productiva exclusiva.

Nunca ejecutar pruebas automatizadas contra la base productiva.

## Migraciones

La API no debe ejecutar migraciones automáticamente al iniciar.

Política recomendada:

1. Revisar el diff de la migración antes de aprobar el despliegue.
2. Probar la migración en una base de staging o pruebas representativa.
3. Tomar backup o snapshot antes de aplicar cambios productivos.
4. Aplicar migraciones como paso explícito del pipeline o una tarea operativa
   controlada.
5. Verificar logs, salud de la API y conectividad después de aplicar.

Comando base, ejecutado solo desde un ambiente autorizado y con secretos ya
inyectados:

```bash
dotnet ef database update \
  --project src/CapitalPos.Infrastructure \
  --startup-project src/CapitalPos.Api
```

## Verificación de conexión

Antes de publicar tráfico productivo:

1. Confirmar que `ConnectionStrings__CapitalPos` existe en el ambiente sin
   imprimir su valor.
2. Confirmar que la cadena exige TLS/SSL.
3. Ejecutar migraciones de forma controlada si corresponde.
4. Iniciar la API.
5. Consultar `GET /api/health`.
6. Revisar logs estructurados sin exponer secretos.

## Criterios para elegir proveedor

Cuando se elija proveedor, evaluar:

- región cercana a los usuarios;
- compatibilidad con PostgreSQL;
- soporte de TLS obligatorio;
- backups automáticos;
- procedimiento de restauración;
- monitoreo y alertas;
- límites de conexiones;
- costos;
- soporte para rotación de credenciales;
- opciones de escalamiento.

## Plantilla sin valores reales

```bash
ConnectionStrings__CapitalPos="<postgresql-productivo-con-tls>"
Jwt__SigningKey="<clave-productiva-de-al-menos-32-caracteres>"
CpeApi__BaseUrl="<url-productiva-de-capitalpos-cpe-api>"
CpeApi__ApiKey="<api-key-productiva-de-cpe>"
ASPNETCORE_ENVIRONMENT="Production"
```

Los valores anteriores son placeholders. Sustituirlos solo en el gestor de
secretos del ambiente de despliegue.
