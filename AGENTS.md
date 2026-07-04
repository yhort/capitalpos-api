# CapitalPOS API

## Proyecto

Backend principal de CapitalPOS desarrollado con .NET 10 y ASP.NET Core.

Arquitectura:

- CapitalPos.Api
- CapitalPos.Application
- CapitalPos.Domain
- CapitalPos.Infrastructure
- CapitalPos.Tests

Responsabilidades principales:

- autenticación y autorización;
- usuarios, empresas, roles y permisos;
- operaciones comerciales;
- integración segura servidor-servidor con CapitalPOS CPE API.

`capitalpos-cpe-api` permanece como servicio especializado en SUNAT, XML, firma y CDR.

## Forma de trabajo

Trabajar por bloques funcionales relacionados.

Antes de editar:

1. Ejecutar `git status`.
2. Inspeccionar únicamente los archivos relacionados.
3. Dar un plan breve con:
   - objetivo;
   - archivos a modificar;
   - validación prevista.
4. Esperar autorización.

Después de editar:

1. Ejecutar:

   ```bash
   dotnet build CapitalPos.Api.sln -m:1 -nr:false
   dotnet test CapitalPos.Api.sln -m:1 -nr:false