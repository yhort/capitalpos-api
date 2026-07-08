# Revisión de paquetes vulnerables

La revisión de paquetes vulnerables de CapitalPOS API se ejecuta con NuGet Audit
habilitado y sin suprimir advertencias `NU190x`.

## Comandos

```bash
dotnet restore CapitalPos.Api.sln
dotnet list CapitalPos.Api.sln package --vulnerable --include-transitive
dotnet build CapitalPos.Api.sln -m:1 -nr:false
dotnet test CapitalPos.Api.sln -m:1 -nr:false
```

## Reglas

- No usar `NoWarn` para ocultar advertencias `NU190x`.
- No desactivar NuGet Audit con `NuGetAudit=false`.
- No aceptar versiones vulnerables.
- Si aparece `NU1900` por una limitación del entorno, repetir `restore` y auditoría
  en el entorno normal antes de continuar.
- Continuar solo cuando `dotnet list ... --vulnerable --include-transitive`
  confirme que no hay paquetes vulnerables conocidos.

## Estado actual

La última revisión ejecutada para este bloque confirmó que la solución no tiene
paquetes vulnerables conocidos con las fuentes NuGet configuradas.
