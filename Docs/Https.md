# HTTPS y reverse proxy

CapitalPOS API está preparada para operar detrás de un proveedor, reverse proxy
o load balancer confiable. En producción, TLS debe terminar en esa capa de
entrada y el tráfico público debe usar HTTPS.

## Estrategia

- El proveedor, reverse proxy o load balancer termina TLS.
- La API reconoce `X-Forwarded-Proto` para detectar el esquema original.
- La API reconoce `X-Forwarded-For` para conservar la IP de cliente reenviada.
- `UseForwardedHeaders` debe ejecutarse antes de redirección HTTPS,
  autenticación y endpoints.
- HSTS se aplica únicamente fuera de `Development`.
- La redirección HTTP a HTTPS se mantiene habilitada cuando corresponda.

## Proxies confiables

No se debe confiar indiscriminadamente en cualquier proxy.

La configuración actual habilita los headers reenviados sin limpiar globalmente
`KnownProxies` ni `KnownNetworks`. En producción, el proveedor de despliegue
debe definir las IPs o redes confiables del reverse proxy o load balancer según
su documentación operativa.

No agregar `KnownProxies.Clear()` ni `KnownNetworks.Clear()` para aceptar
cualquier origen salvo que exista una justificación explícita y revisada.

## Certificados

- Los certificados públicos deben ser administrados y renovados por el
  proveedor, reverse proxy o load balancer.
- No almacenar certificados privados, archivos `.pfx`, `.p12`, `.key` ni
  contraseñas de certificados en Git.
- No agregar rutas locales a certificados productivos en `appsettings`.
- Development debe seguir funcionando sin exigir certificados productivos.

## Verificación

Antes de habilitar tráfico productivo:

1. Confirmar que el tráfico público usa HTTPS.
2. Confirmar que el proxy envía `X-Forwarded-Proto: https`.
3. Confirmar que CapitalPOS API interpreta el esquema como HTTPS detrás del
   proxy.
4. Confirmar que HSTS está activo fuera de `Development`.
5. Confirmar que HTTP redirige a HTTPS cuando corresponda.
6. Confirmar que solo proxies o redes confiables pueden enviar headers
   reenviados.

## Pendiente del despliegue

Quedan pendientes para el bloque de despliegue:

- elección de proveedor;
- dominio;
- DNS;
- certificados reales;
- puertos productivos;
- IPs o redes confiables del proxy;
- configuración final del reverse proxy o load balancer.
