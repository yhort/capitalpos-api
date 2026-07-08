# Datos demo para desarrollo local

CapitalPOS API puede crear datos demo seguros para probar `capitalpos-web`
contra `capitalpos-api` usando una base local de desarrollo, por ejemplo
`capitalpos_dev`.

El seed demo solo se ejecuta cuando se cumplen ambas condiciones:

- `ASPNETCORE_ENVIRONMENT=Development`;
- `DemoSeed:Enabled=true`.

Nunca debe habilitarse en producción.

## Datos creados

- Empresa demo:
  - RUC: `20600000001`
  - Razón social: `CapitalPOS Demo S.A.C.`
- Usuario administrador demo:
  - correo: `admin@capitalpos.test`
  - rol: `Administrador`
  - relación `UsuarioEmpresa` activa
- Credencial demo:
  - se crea solo si existe `DemoSeed:AdminPassword`;
  - se almacena como hash usando el hasher de la aplicación;
  - la contraseña no debe guardarse en Git ni en `appsettings`.

## Configuración con user-secrets

Mantener `DemoSeed:Enabled` en `false` por defecto en
`appsettings.Development.json`. Para activar el seed localmente:

```bash
dotnet user-secrets set "DemoSeed:Enabled" "true" --project src/CapitalPos.Api
dotnet user-secrets set "DemoSeed:AdminPassword" "<password-demo-local>" --project src/CapitalPos.Api
```

También puede usarse variable de entorno:

```bash
DemoSeed__Enabled=true
DemoSeed__AdminPassword="<password-demo-local>"
```

Los valores anteriores son placeholders. No registrar valores reales en
documentación, logs, `.env`, `appsettings` ni Git.

## Migraciones

Antes de iniciar la API, aplicar migraciones sobre la base local de desarrollo:

```bash
dotnet ef database update --project src/CapitalPos.Infrastructure --startup-project src/CapitalPos.Api
```

La cadena `ConnectionStrings:CapitalPos` debe configurarse con user-secrets o
variables de entorno, no en `appsettings`.

## Ejecución local

```bash
dotnet run --project src/CapitalPos.Api
```

Al iniciar en Development con `DemoSeed:Enabled=true`, la API verificará los
datos demo de forma idempotente. Si falta `DemoSeed:AdminPassword`, creará solo
los datos seguros que correspondan y no creará la credencial.

## Login desde Angular

Usar el endpoint:

```http
POST /api/auth/login
```

Body:

```json
{
  "correo": "admin@capitalpos.test",
  "password": "<password-demo-local>"
}
```

Después del login, Angular debe enviar el JWT como `Authorization: Bearer` y
usar el identificador de la empresa demo en `X-CapitalPos-EmpresaId`.

Angular nunca debe conocer ni enviar la `X-API-KEY` de CapitalPOS CPE API.
