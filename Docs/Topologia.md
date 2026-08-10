# Topologia de despliegue interno

Esta guia define el aislamiento de red entre los componentes de CapitalPOS
sin elegir proveedor ni publicar la aplicacion.

## Componentes

- `capitalpos-web`: frontend Angular. Solo consume `capitalpos-api`.
- `capitalpos-api`: API publica/controlada del negocio. Unico caller autorizado
  hacia `capitalpos-cpe-api`.
- `capitalpos-cpe-api`: servicio de emision CPE/SUNAT. Queda en red privada
  (backend-only) y no se expone a Internet ni a browsers.

## Reglas de aislamiento

1. `capitalpos-web` nunca llama a `capitalpos-cpe-api` de forma directa.
2. En desarrollo local, Angular usa proxy hacia `capitalpos-api`
   (`src/proxy.conf.json`); no se configura acceso browser a CPE.
3. Solo `capitalpos-api` invoca CPE con el header interno `X-API-KEY`.
4. `capitalpos-cpe-api` no configura CORS publico ni politicas pensadas para
   origenes de frontend.
5. La entrada publica tipica es: usuarios -> `capitalpos-web` + `capitalpos-api`.
   CPE permanece en red interna o privada.
6. Certificados PFX, claves SOL y API keys de CPE se inyectan por secretos del
   ambiente; no viajan al frontend.

## Diagrama logico

```text
[Browser / capitalpos-web]
            |
            v
      [capitalpos-api]  ----(X-API-KEY, red privada)---->  [capitalpos-cpe-api]
            |
            v
      [PostgreSQL]
```

## Verificacion minima

- CORS de `capitalpos-api` lista solo origenes frontend autorizados
  (`Cors:AllowedOrigins` / `Cors__AllowedOrigins__N`).
- `capitalpos-cpe-api` no declara `AddCors` / `UseCors` para origenes publicos.
- Health y diagnosticos de CPE, si existen, no se publican fuera de la red
  privada sin proteccion por API key.
