# Preparación de monitoreo

Esta guía define la política mínima de monitoreo de CapitalPOS API sin elegir
proveedor ni integrar una herramienta concreta todavía.

## Señales mínimas

CapitalPOS API debe observar como mínimo:

- disponibilidad de `GET /api/health`;
- tasa de errores HTTP 5xx;
- tasa de respuestas 4xx relevantes;
- latencia por endpoint;
- tiempo de respuesta del servicio CapitalPOS CPE API;
- errores de conexión a PostgreSQL;
- fallos de autenticación y autorización agregados, sin datos sensibles;
- excepciones no controladas;
- volumen de solicitudes;
- uso de CPU, memoria y otros recursos cuando el proveedor lo permita.

## Alertas mínimas

Configurar alertas para:

- health no disponible;
- aumento sostenido de errores 5xx;
- latencia elevada;
- fallos repetidos hacia CapitalPOS CPE API;
- fallos de conexión a PostgreSQL;
- ausencia inesperada de tráfico;
- crecimiento anormal de errores de autenticación;
- vencimiento próximo de certificados si el proveedor lo permite.

## Capacidades existentes

El monitoreo debe aprovechar lo ya implementado:

- `RequestLoggingMiddleware`;
- `X-Correlation-Id`;
- `GlobalExceptionHandlingMiddleware`;
- `GET /api/health`;
- logs estructurados;
- auditoría de operaciones.

## Seguridad

- No registrar JWT.
- No registrar API keys.
- No registrar contraseñas.
- No registrar cadenas de conexión.
- No registrar certificados.
- No registrar request bodies completos.
- Limitar datos personales en logs, métricas y alertas.
- Restringir acceso a logs y métricas por rol operativo.
- Definir retención según costo, necesidad operativa y cumplimiento.
- Evitar imprimir secretos al validar configuración.

## Checklist posterior al despliegue

- Health visible desde el monitor.
- Logs disponibles.
- `X-Correlation-Id` rastreable entre solicitudes y errores.
- Alerta de prueba funcionando.
- Errores 5xx detectables.
- Latencia visible por endpoint.
- Integración CPE observable.
- Errores de PostgreSQL observables.
- Acceso a logs restringido.
- Retención configurada.

## Criterios para elegir proveedor

Cuando se elija proveedor o herramienta, evaluar:

- integración con la plataforma de despliegue;
- soporte para logs estructurados;
- métricas;
- trazas distribuidas;
- alertas;
- retención;
- costos;
- región;
- control de acceso;
- exportación de datos;
- facilidad de correlacionar CapitalPOS API con CapitalPOS CPE API.

## Plantilla segura

```bash
MONITORING_ENVIRONMENT="Production"
MONITORING_SERVICE_NAME="CapitalPos.Api"
MONITORING_HEALTH_PATH="/api/health"
MONITORING_CORRELATION_HEADER="X-Correlation-Id"
```

Los valores anteriores no contienen endpoints externos, tokens ni claves reales.
La integración real con una herramienta de monitoreo queda pendiente del
despliegue y de la elección del proveedor.
