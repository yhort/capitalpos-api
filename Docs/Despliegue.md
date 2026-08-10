# Preparación de despliegue

Esta guía prepara el despliegue de CapitalPOS API sin elegir proveedor ni
publicar la aplicación todavía.

## Prerrequisitos

- PostgreSQL productivo administrado, con TLS/SSL obligatorio.
- Secretos configurados fuera del repositorio.
- HTTPS terminado en proveedor, reverse proxy o load balancer.
- Dominio pendiente de definir.
- Migraciones productivas manuales y revisadas.
- Health check disponible en `GET /api/health`.
- Logs estructurados disponibles.
- Backups y restauración definidos para la base de datos.
- Monitoreo y alertas definidos antes de abrir tráfico real.
- Región y costos evaluados.
- Topologia interna documentada en `Docs/Topologia.md`: `capitalpos-web` solo
  consume `capitalpos-api` y `capitalpos-cpe-api` queda en red privada.

## Variables requeridas

Configurar estas variables en el ambiente de despliegue, usando placeholders
solo como referencia:

```bash
ConnectionStrings__CapitalPos="<cadena-postgresql-productiva-con-tls>"
Jwt__SigningKey="<clave-productiva-de-al-menos-32-caracteres>"
CpeApi__BaseUrl="<url-productiva-de-capitalpos-cpe-api>"
CpeApi__ApiKey="<api-key-productiva-de-cpe>"
Cors__AllowedOrigins__0="<https-origen-frontend-autorizado>"
ASPNETCORE_ENVIRONMENT="Production"
```

No guardar valores reales en Git, documentación, `appsettings`, archivos `.env`,
logs ni mensajes de error.

## Checklist previo

- Build Release correcto.
- Tests automatizados aprobados.
- Auditoría NuGet sin paquetes vulnerables conocidos.
- Secretos configurados fuera del repositorio.
- Base PostgreSQL productiva disponible.
- Migraciones revisadas y aprobadas.
- TLS/HTTPS habilitado en la entrada pública.
- Health check definido y accesible.
- Logs disponibles y sin secretos.
- Backups automáticos confirmados.
- Restauración probada o procedimiento de restauración documentado.
- Rollback definido antes de publicar tráfico.

## Procedimiento general

1. Compilar en Release:

   ```bash
   dotnet publish src/CapitalPos.Api/CapitalPos.Api.csproj -c Release -o ./artifacts/publish
   ```

2. Publicar el artefacto en la plataforma elegida.
3. Configurar variables y secretos desde el mecanismo seguro del ambiente.
4. Aplicar migraciones de forma controlada si corresponde:

   ```bash
   dotnet ef database update \
     --project src/CapitalPos.Infrastructure \
     --startup-project src/CapitalPos.Api
   ```

5. Iniciar el servicio.
6. Validar `GET /api/health`.
7. Validar OpenAPI según la política de producción definida para el despliegue.
8. Ejecutar una prueba funcional mínima autenticada.
9. Comprobar logs estructurados y propagación de `X-Correlation-Id`.
10. Confirmar que no se exponen secretos en respuestas, logs ni errores.
11. Ejecutar rollback si falla una verificación crítica.

## Rollback

El procedimiento de rollback debe definirse antes del despliegue real:

- conservar el artefacto anterior;
- conservar o poder restaurar el backup previo de base de datos;
- documentar cómo revertir variables de entorno;
- documentar cómo retirar tráfico de la versión fallida;
- validar health y logs después del rollback.

## Criterios para elegir proveedor

Cuando se elija proveedor, evaluar:

- región cercana a Perú o usuarios principales;
- soporte para .NET 10;
- variables y secretos administrados;
- HTTPS automático;
- health checks;
- escalado;
- logs;
- límites de memoria y CPU;
- integración con PostgreSQL;
- costos;
- backups y restauración;
- facilidad de rollback.

## Plantilla segura

```bash
ASPNETCORE_ENVIRONMENT="Production"
ConnectionStrings__CapitalPos="<cadena-postgresql-productiva-con-tls>"
Jwt__SigningKey="<clave-productiva-de-al-menos-32-caracteres>"
CpeApi__BaseUrl="<url-productiva-de-capitalpos-cpe-api>"
CpeApi__ApiKey="<api-key-productiva-de-cpe>"
Cors__AllowedOrigins__0="<https-origen-frontend-autorizado>"
```

Los valores anteriores son placeholders. Sustituirlos solo en el gestor de
secretos del ambiente cuando se elija proveedor.

## Pendiente

Queda pendiente para un bloque posterior:

- elegir proveedor;
- crear Dockerfile si la plataforma lo requiere;
- crear pipeline CI/CD;
- configurar dominio y DNS;
- desplegar la API;
- aplicar migraciones productivas reales;
- configurar secretos reales;
- ejecutar verificación productiva completa.
