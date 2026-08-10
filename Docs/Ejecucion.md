# Ejecución local

Guía mínima para levantar CapitalPOS API en desarrollo sin agregar secretos al
repositorio.

## Requisitos

- .NET SDK 10, fijado por `global.json`.
- Herramientas locales del repositorio restauradas desde `dotnet-tools.json`.
- PostgreSQL disponible para la base local.
- `capitalpos-cpe-api` disponible solo si se probará emisión CPE extremo a
  extremo.

## Restaurar dependencias

```bash
dotnet restore CapitalPos.Api.sln
dotnet tool restore
```

## Configuración segura local

`appsettings.json` y `appsettings.Development.json` deben conservar valores
vacíos para secretos y credenciales.

Configurar los valores locales con user-secrets:

```bash
dotnet user-secrets set "ConnectionStrings:CapitalPos" "<cadena-postgresql-local>" --project src/CapitalPos.Api
dotnet user-secrets set "Jwt:SigningKey" "<clave-local-de-al-menos-32-caracteres>" --project src/CapitalPos.Api
dotnet user-secrets set "CpeApi:BaseUrl" "<url-local-de-capitalpos-cpe-api>" --project src/CapitalPos.Api
dotnet user-secrets set "CpeApi:ApiKey" "<api-key-local-de-cpe>" --project src/CapitalPos.Api
```

En otros ambientes se pueden usar variables de entorno equivalentes:

```bash
ConnectionStrings__CapitalPos
Jwt__SigningKey
CpeApi__BaseUrl
CpeApi__ApiKey
Cors__AllowedOrigins__0
```

No imprimir ni registrar esos valores.

## Base de datos

Aplicar migraciones sobre la base configurada en `ConnectionStrings:CapitalPos`:

```bash
dotnet ef database update \
  --project src/CapitalPos.Infrastructure \
  --startup-project src/CapitalPos.Api
```

Para pruebas de integración con PostgreSQL real, usar una base exclusiva cuyo
nombre contenga `test` y definir la variable:

```bash
CAPITALPOS_TEST_CONNECTION_STRING="<cadena-postgresql-de-pruebas>"
```

No usar la base de desarrollo ni producción para pruebas automatizadas.

## Ejecutar la API

```bash
dotnet run --project src/CapitalPos.Api
```

Con el perfil HTTP de desarrollo, la API escucha en:

```text
http://localhost:5198
```

Endpoints públicos útiles:

```bash
curl http://localhost:5198/api/health
curl http://localhost:5198/openapi/v1.json
```

Los endpoints empresariales requieren JWT y el header:

```text
X-CapitalPos-EmpresaId
```

Angular nunca debe conocer ni enviar la `X-API-KEY` de `capitalpos-cpe-api`.
El contrato publico de `POST /api/cpe/emitir` esta definido en
`Docs/ContratoCpeEmision.md`.

## Validación local

Antes de preparar un commit, ejecutar:

```bash
dotnet restore CapitalPos.Api.sln
dotnet list CapitalPos.Api.sln package --vulnerable --include-transitive
dotnet build CapitalPos.Api.sln -m:1 -nr:false
dotnet test CapitalPos.Api.sln -m:1 -nr:false
```

La auditoría debe confirmar que no hay paquetes vulnerables conocidos.
