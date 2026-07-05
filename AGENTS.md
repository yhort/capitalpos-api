# CapitalPOS API

## Proyecto

Backend principal de CapitalPOS desarrollado con .NET 10 y ASP.NET Core.

Responsabilidades principales:

- autenticacion y autorizacion;
- usuarios, empresas, roles y permisos;
- operaciones comerciales;
- integracion segura servidor-servidor con CapitalPOS CPE API.

`capitalpos-cpe-api` permanece como servicio especializado en SUNAT, XML, firma y CDR.

## Forma de trabajo

Trabajar por bloques funcionales relacionados.

Antes de editar:

1. Ejecutar `git status`.
2. Inspeccionar unicamente los archivos relacionados.
3. Dar un plan breve con:
   - objetivo;
   - archivos a modificar;
   - validacion prevista.
4. Esperar autorizacion cuando el usuario lo pida expresamente o cuando exista una decision de arquitectura/riesgo.

Despues de editar:

1. Ejecutar:

   ```bash
   dotnet build CapitalPos.Api.sln -m:1 -nr:false
   dotnet test CapitalPos.Api.sln -m:1 -nr:false
   ```

2. Mostrar `git status`.
3. Mostrar `git diff --stat`.
4. Explicar brevemente el resultado.

## NuGet Audit

Si aparece `NU1900` dentro del sandbox:

- repetir restore y auditoria fuera del sandbox;
- no usar `NoWarn`;
- no desactivar NuGet Audit;
- continuar si el entorno normal confirma que no hay paquetes vulnerables.

## Restricciones

- No agregar EF Core, `DbContext` ni migraciones hasta que el bloque lo autorice.
- No agregar autenticacion JWT hasta que el bloque lo autorice.
- No crear integracion HTTP hacia CPE API hasta que el bloque lo autorice.
- No agregar secretos, API keys, certificados, archivos `.env` ni credenciales al repositorio.
- No modificar `capitalpos-cpe-api` desde este repositorio.
- No hacer commit ni push salvo autorizacion explicita del usuario.
- Preparar para commit solo los archivos relacionados con el bloque aprobado.
- No revertir cambios existentes del usuario sin autorizacion explicita.

## Arquitectura

La solucion mantiene una arquitectura por capas:

- `CapitalPos.Api`: entrada HTTP, OpenAPI, endpoints y composicion de dependencias.
- `CapitalPos.Application`: casos de uso, puertos e interfaces de aplicacion.
- `CapitalPos.Domain`: entidades, enums y reglas de negocio puras.
- `CapitalPos.Infrastructure`: adaptadores tecnicos e implementaciones de puertos.
- `CapitalPos.Tests`: pruebas unitarias y de integracion ligera.

Reglas de dependencia:

- `Api` puede depender de `Application` e `Infrastructure`.
- `Application` puede depender de `Domain`.
- `Infrastructure` puede depender de `Application` y `Domain`.
- `Domain` no debe depender de ASP.NET Core, EF Core, Infrastructure ni librerias externas innecesarias.

## Integracion CPE

`capitalpos-api` sera el unico backend consumido por Angular para operaciones de negocio.

Angular nunca debe conocer la `X-API-KEY` de `capitalpos-cpe-api`.

Cuando se implemente la integracion:

- `capitalpos-api` llamara a `capitalpos-cpe-api` de servidor a servidor;
- la `X-API-KEY` se agregara desde configuracion segura;
- no se subiran API keys, certificados PFX, claves SOL ni secretos al repositorio;
- `capitalpos-cpe-api` seguira encargado de credenciales SOL, certificados digitales, XML UBL, firma, envio SUNAT y procesamiento CDR.
