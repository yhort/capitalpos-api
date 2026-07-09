# Contrato publico de emision CPE

Este documento define el contrato publico estable que expone `capitalpos-api`
para `POST /api/cpe/emitir`. El frontend debe consumir solo este endpoint; no
debe llamar directamente a `capitalpos-cpe-api` ni conocer su `X-API-KEY`.

## Seguridad y contexto

El endpoint requiere:

- JWT valido.
- Header `X-CapitalPos-EmpresaId` con la empresa activa.
- Permiso de empresa `EmitirCpe`.

`capitalpos-api` actua como fachada: valida identidad, empresa activa,
permisos, audita la operacion, llama internamente a `capitalpos-cpe-api` y
normaliza la respuesta publica.

## Request

El cuerpo se reenvia como JSON al servicio CPE interno. Mientras el contrato
CPE interno termina de cerrarse, `capitalpos-api` no debe agregar secretos ni
datos internos al payload recibido.

Ejemplo minimo:

```json
{
  "tipoComprobante": "01",
  "serie": "F001",
  "correlativo": 1,
  "rucEmisor": "20123456789"
}
```

## Response publico

Todas las respuestas normalizadas usan esta envoltura:

```json
{
  "ok": true,
  "mensaje": "Aceptado por SUNAT.",
  "data": {
    "ok": true,
    "estado": "ACEPTADO",
    "mensaje": "Aceptado por SUNAT.",
    "codigo": "ACEPTADO",
    "comprobante": "F001-1",
    "hash": "abc123",
    "nombreXml": "20123456789-01-F001-1.xml",
    "nombreZip": "20123456789-01-F001-1.zip",
    "nombreCdr": "R-20123456789-01-F001-1.zip",
    "errores": []
  },
  "errores": []
}
```

Campos principales:

- `ok`: resultado funcional publico.
- `mensaje`: mensaje seguro para mostrar al usuario.
- `data`: detalle normalizado de emision.
- `errores`: lista simple de mensajes para compatibilidad visual.
- `data.estado`: estado canonico de emision.
- `data.codigo`: codigo estable asociado al estado.
- `data.errores`: errores estructurados con `codigo`, `campo` y `mensaje`.

## Estados canonicos

`data.estado` puede ser:

- `SIMULADO`: emision aceptada en modo simulacion.
- `ACEPTADO`: emision aceptada por SUNAT o por el proveedor CPE.
- `RECHAZADO`: rechazo funcional del comprobante.
- `ERROR_VALIDACION`: request o comprobante con datos invalidos.
- `ERROR_XML`: no se pudo generar o validar el XML del comprobante.
- `ERROR_FIRMA`: no se pudo firmar digitalmente el comprobante.
- `ERROR_SUNAT`: error funcional/controlado al comunicarse con SUNAT.
- `ERROR_CDR`: no se pudo generar, recibir, guardar o interpretar el CDR.
- `ERROR_INTERNO`: error tecnico inesperado dentro de `capitalpos-cpe-api`.
- `ERROR_CPE`: servicio CPE no disponible o error tecnico controlado.
- `RESPUESTA_CPE_INVALIDA`: estado tecnico de normalizacion usado cuando la
  respuesta del servicio CPE no pudo interpretarse de forma segura.

Estados no listados que vengan del servicio CPE deben conservarse solo si son
seguros y no exponen detalles internos.

### Diferencias entre errores tecnicos

- `ERROR_CPE`: `capitalpos-api` no pudo comunicarse correctamente con
  `capitalpos-cpe-api`, o recibio un error tecnico controlado desde ese
  servicio.
- `RESPUESTA_CPE_INVALIDA`: `capitalpos-api` recibio una respuesta vacia,
  malformada o sin el contrato minimo necesario para construir una respuesta
  publica segura.
- `ERROR_INTERNO`: `capitalpos-cpe-api` si respondio con JSON interpretable,
  pero informa que fallo internamente al procesar la emision.

## Politica HTTP

- `200 OK`: emision funcionalmente exitosa, incluyendo `SIMULADO` y
  `ACEPTADO`.
- `400 Bad Request`: rechazo funcional o validacion controlada, como
  `RECHAZADO`, `ERROR_VALIDACION` o `ERROR_SUNAT`.
- `502 Bad Gateway`: el servicio CPE no responde, responde JSON invalido o la
  respuesta no cumple el contrato minimo.
- `5xx`: error tecnico real no recuperable en `capitalpos-api`.

Angular debe leer el cuerpo tambien en respuestas `4xx` y `5xx` cuando exista.
Un `4xx` con `ok=false` y `data.estado` funcional no debe mostrarse como
"error inesperado".

## Ejemplos

### SIMULADO

```json
{
  "ok": true,
  "mensaje": "Comprobante aceptado en modo simulacion.",
  "data": {
    "ok": true,
    "estado": "SIMULADO",
    "mensaje": "Comprobante aceptado en modo simulacion.",
    "codigo": "SIMULADO",
    "comprobante": "F001-1",
    "hash": "abc123",
    "nombreXml": "20123456789-01-F001-1.xml",
    "nombreZip": "20123456789-01-F001-1.zip",
    "nombreCdr": "R-20123456789-01-F001-1.zip",
    "errores": []
  },
  "errores": []
}
```

### ACEPTADO

```json
{
  "ok": true,
  "mensaje": "Aceptado por SUNAT.",
  "data": {
    "ok": true,
    "estado": "ACEPTADO",
    "mensaje": "Aceptado por SUNAT.",
    "codigo": "ACEPTADO",
    "comprobante": "F001-2",
    "hash": "def456",
    "nombreXml": "20123456789-01-F001-2.xml",
    "nombreZip": "20123456789-01-F001-2.zip",
    "nombreCdr": "R-20123456789-01-F001-2.zip",
    "errores": []
  },
  "errores": []
}
```

### RECHAZADO

```json
{
  "ok": false,
  "mensaje": "El comprobante fue rechazado.",
  "data": {
    "ok": false,
    "estado": "RECHAZADO",
    "mensaje": "El comprobante fue rechazado.",
    "codigo": "RECHAZADO",
    "comprobante": null,
    "hash": null,
    "nombreXml": null,
    "nombreZip": null,
    "nombreCdr": null,
    "errores": [
      {
        "codigo": "SUNAT_2335",
        "campo": null,
        "mensaje": "El RUC del receptor no existe."
      }
    ]
  },
  "errores": [
    "El RUC del receptor no existe."
  ]
}
```

### ERROR_VALIDACION

```json
{
  "ok": false,
  "mensaje": "El comprobante tiene errores de validacion.",
  "data": {
    "ok": false,
    "estado": "ERROR_VALIDACION",
    "mensaje": "El comprobante tiene errores de validacion.",
    "codigo": "ERROR_VALIDACION",
    "comprobante": null,
    "hash": null,
    "nombreXml": null,
    "nombreZip": null,
    "nombreCdr": null,
    "errores": [
      {
        "codigo": "CPE_SERIE_OBLIGATORIA",
        "campo": "serie",
        "mensaje": "Debe indicar la serie del comprobante."
      },
      {
        "codigo": "CPE_CLIENTE_OBLIGATORIO",
        "campo": "cliente",
        "mensaje": "Debe indicar los datos del cliente."
      }
    ]
  },
  "errores": [
    "Debe indicar la serie del comprobante.",
    "Debe indicar los datos del cliente."
  ]
}
```

### ERROR_SUNAT

```json
{
  "ok": false,
  "mensaje": "SUNAT no pudo procesar el comprobante.",
  "data": {
    "ok": false,
    "estado": "ERROR_SUNAT",
    "mensaje": "SUNAT no pudo procesar el comprobante.",
    "codigo": "ERROR_SUNAT",
    "comprobante": null,
    "hash": null,
    "nombreXml": null,
    "nombreZip": null,
    "nombreCdr": null,
    "errores": [
      {
        "codigo": "SUNAT_TIMEOUT",
        "campo": null,
        "mensaje": "SUNAT no respondio dentro del tiempo esperado."
      }
    ]
  },
  "errores": [
    "SUNAT no respondio dentro del tiempo esperado."
  ]
}
```

### ERROR_CPE

```json
{
  "ok": false,
  "mensaje": "Servicio CPE no disponible.",
  "data": {
    "ok": false,
    "estado": "ERROR_CPE",
    "mensaje": "Servicio CPE no disponible.",
    "codigo": "ERROR_CPE",
    "comprobante": null,
    "hash": null,
    "nombreXml": null,
    "nombreZip": null,
    "nombreCdr": null,
    "errores": [
      {
        "codigo": "ERROR_CPE",
        "campo": null,
        "mensaje": "Servicio CPE no disponible."
      }
    ]
  },
  "errores": [
    "Servicio CPE no disponible."
  ]
}
```

### RESPUESTA_CPE_INVALIDA

```json
{
  "ok": false,
  "mensaje": "No se pudo interpretar la respuesta del servicio CPE.",
  "data": {
    "ok": false,
    "estado": "RESPUESTA_CPE_INVALIDA",
    "mensaje": "No se pudo interpretar la respuesta del servicio CPE.",
    "codigo": "CPE_RESPUESTA_INVALIDA",
    "comprobante": null,
    "hash": null,
    "nombreXml": null,
    "nombreZip": null,
    "nombreCdr": null,
    "errores": [
      {
        "codigo": "CPE_RESPUESTA_INVALIDA",
        "campo": null,
        "mensaje": "No se pudo interpretar la respuesta del servicio CPE."
      }
    ]
  },
  "errores": [
    "No se pudo interpretar la respuesta del servicio CPE."
  ]
}
```

## Datos sensibles

La respuesta publica no debe incluir:

- `X-API-KEY`.
- rutas internas.
- certificados.
- credenciales SUNAT.
- cuerpo crudo de `capitalpos-cpe-api`.
- XML, ZIP o CDR en bruto.
