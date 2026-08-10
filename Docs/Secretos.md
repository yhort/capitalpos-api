# Política de secretos

Esta política define cómo preparar los secretos de CapitalPOS API sin elegir
todavía un proveedor ni almacenar valores reales.

## Secretos y variables actuales

- `ConnectionStrings__CapitalPos`: cadena de conexión PostgreSQL del ambiente.
- `Jwt__SigningKey`: clave de firma HMAC SHA-256 para access tokens JWT.
- `CpeApi__BaseUrl`: URL base de CapitalPOS CPE API (red privada / interna).
- `CpeApi__ApiKey`: API key enviada por servidor como `X-API-KEY` hacia
  CapitalPOS CPE API.
- `Cors__AllowedOrigins__N`: origenes frontend autorizados para CORS en
  `capitalpos-api` (no es un secreto, pero se configura por ambiente).
- `CAPITALPOS_TEST_CONNECTION_STRING`: cadena exclusiva para pruebas de
  integración con PostgreSQL real.

### Secretos de `capitalpos-cpe-api`

Configurar fuera del repositorio (user-secrets o variables de entorno del
proceso CPE):

- `CpeSecuritySettings__ApiKey`: API key interna exigida por `X-API-KEY`.
- `CpeSettings__PasswordCertificado`: password del certificado PFX SUNAT.
- `CpeSettings__UsuarioSol` / `CpeSettings__ClaveSol`: credenciales SOL.
- `CpeSettings__RutaCertificado`: ruta a un archivo PFX fuera de Git
  (volumen montado o secreto de archivo del ambiente).

No versionar archivos `.pfx`, passwords ni claves SOL en el repositorio.

## Gestión por ambiente

### Desarrollo local

Usar `dotnet user-secrets` contra el proyecto `src/CapitalPos.Api`.

```bash
dotnet user-secrets set "ConnectionStrings:CapitalPos" "<cadena-postgresql-local>" --project src/CapitalPos.Api
dotnet user-secrets set "Jwt:SigningKey" "<clave-local-de-al-menos-32-caracteres>" --project src/CapitalPos.Api
dotnet user-secrets set "CpeApi:BaseUrl" "<url-local-de-capitalpos-cpe-api>" --project src/CapitalPos.Api
dotnet user-secrets set "CpeApi:ApiKey" "<api-key-local-de-cpe>" --project src/CapitalPos.Api
dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:4200" --project src/CapitalPos.Api
```

Para `capitalpos-cpe-api` (proyecto `CapitalPos.Cpe.Api`):

```bash
dotnet user-secrets set "CpeSecuritySettings:ApiKey" "<api-key-local-de-cpe>" --project CapitalPos.Cpe.Api
dotnet user-secrets set "CpeSettings:PasswordCertificado" "<password-pfx-local>" --project CapitalPos.Cpe.Api
dotnet user-secrets set "CpeSettings:UsuarioSol" "<usuario-sol-local>" --project CapitalPos.Cpe.Api
dotnet user-secrets set "CpeSettings:ClaveSol" "<clave-sol-local>" --project CapitalPos.Cpe.Api
```

### Pruebas

Usar variables de entorno o configuración aislada del host de pruebas. Para
pruebas con PostgreSQL real:

```bash
CAPITALPOS_TEST_CONNECTION_STRING="<cadena-postgresql-de-pruebas>"
```

La base de pruebas debe ser exclusiva y su nombre debe contener `test`.

### Producción

Usar el gestor de secretos del proveedor de despliegue que se elija más
adelante. La integración con un gestor real queda pendiente del despliegue.

Variables esperadas:

```bash
ConnectionStrings__CapitalPos="<cadena-postgresql-productiva-con-tls>"
Jwt__SigningKey="<clave-productiva-de-al-menos-32-caracteres>"
CpeApi__BaseUrl="<url-productiva-de-capitalpos-cpe-api>"
CpeApi__ApiKey="<api-key-productiva-de-cpe>"
Cors__AllowedOrigins__0="<https-origen-frontend-autorizado>"
```

En el proceso de `capitalpos-cpe-api`:

```bash
CpeSecuritySettings__ApiKey="<api-key-productiva-de-cpe>"
CpeSettings__PasswordCertificado="<password-pfx-productivo>"
CpeSettings__UsuarioSol="<usuario-sol-productivo>"
CpeSettings__ClaveSol="<clave-sol-productiva>"
CpeSettings__RutaCertificado="<ruta-segura-al-pfx-fuera-de-git>"
```

Los valores anteriores son placeholders y no deben copiarse como secretos.

## Reglas obligatorias

- No almacenar secretos en `appsettings.json` ni `appsettings.Development.json`.
- No almacenar secretos en Git.
- No incluir secretos en documentación, ejemplos, logs ni mensajes de error.
- No compartir secretos entre desarrollo, pruebas y producción.
- Usar valores distintos por ambiente.
- Aplicar privilegios mínimos para cuentas, usuarios y API keys.
- Limitar quién puede leer o modificar secretos.
- Rotar secretos periódicamente y ante cualquier sospecha de exposición.
- Revocar inmediatamente secretos comprometidos.
- Verificar si una variable existe sin imprimir su valor.
- No enviar la `X-API-KEY` de CapitalPOS CPE API a Angular.

## Rotación mínima

### JWT SigningKey

- Rotar en una ventana controlada.
- Publicar la nueva clave en el gestor de secretos del ambiente.
- Reiniciar o reciclar instancias de API para cargar la clave nueva.
- Considerar que los access tokens anteriores expiran en 15 minutos.
- Revocar inmediatamente si hay sospecha de exposición.

### API key de CPE

- Generar una nueva API key en CapitalPOS CPE API o su mecanismo operativo.
- Actualizar `CpeApi__ApiKey` en el gestor de secretos del ambiente.
- Reiniciar o reciclar CapitalPOS API.
- Revocar la API key anterior cuando la nueva esté validada.
- Revocar inmediatamente si hay sospecha de exposición.

### Credenciales PostgreSQL

- Crear credenciales nuevas con privilegios mínimos.
- Actualizar `ConnectionStrings__CapitalPos` en el gestor de secretos del
  ambiente.
- Verificar conectividad sin imprimir la cadena.
- Reiniciar o reciclar la API.
- Revocar las credenciales anteriores cuando la nueva conexión esté validada.
- Tomar en cuenta migraciones pendientes y tareas operativas antes de rotar.

## Verificación segura

Para comprobar configuración en cualquier ambiente:

1. Confirmar que cada variable requerida existe sin imprimir su valor.
2. Confirmar que `ConnectionStrings__CapitalPos` exige TLS/SSL en producción.
3. Confirmar que los valores pertenecen al ambiente correcto.
4. Ejecutar `GET /api/health`.
5. Revisar logs estructurados sin exponer secretos.

## Appsettings

Estos valores deben permanecer vacíos en los archivos versionados:

```json
{
  "ConnectionStrings": {
    "CapitalPos": ""
  },
  "Jwt": {
    "SigningKey": ""
  },
  "CpeApi": {
    "BaseUrl": "",
    "ApiKey": ""
  },
  "Cors": {
    "AllowedOrigins": []
  }
}
```

En `capitalpos-cpe-api`, mantener vacíos en archivos versionados:

- `CpeSecuritySettings:ApiKey`
- `CpeSettings:PasswordCertificado`
- `CpeSettings:UsuarioSol`
- `CpeSettings:ClaveSol`
