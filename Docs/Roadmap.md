# Roadmap — CapitalPOS API

## Objetivo

Construir el backend principal de CapitalPOS para:

- usuarios;
- empresas;
- roles y permisos;
- operaciones comerciales;
- integración segura con CapitalPOS CPE API.

## Fase 1 — Base técnica

- [x] Crear solución .NET 10
- [x] Configurar arquitectura por capas
- [x] Crear endpoint GET /api/health
- [x] Habilitar OpenAPI
- [x] Configurar pruebas
- [x] Inicializar Git y GitHub

## Fase 2 — Dominio multiempresa

- [x] Crear Empresa
- [x] Crear Usuario
- [x] Crear UsuarioEmpresa
- [x] Crear RolEmpresa
- [x] Agregar pruebas unitarias
- [x] Crear casos de uso iniciales
- [x] Crear endpoints iniciales

## Fase 3 — Persistencia

- [x] Elegir motor de base de datos
- [x] Instalar EF Core
- [x] Crear DbContext
- [x] Crear configuraciones Fluent API
- [x] Crear migración inicial
- [x] Implementar repositorios
- [x] Reemplazar almacenamiento temporal
- [x] Agregar pruebas de integración

## Fase 4 — Autenticación y seguridad

- [x] Agregar credenciales de usuario
- [x] Implementar hashing de contraseñas
- [x] Crear login
- [x] Crear endpoint público POST /api/auth/login
- [x] Crear datos demo seguros para desarrollo local
- [x] Implementar JWT o cookie segura
- [x] Crear refresh token si aplica — No aplicado por ahora; fuera del MVP actual
- [x] Implementar empresa activa
- [x] Validar roles y permisos
- [x] Proteger endpoints

## Fase 5 — Integración CPE

- [x] Configurar cliente HTTP tipado
- [x] Guardar BaseUrl de CPE API
- [x] Guardar X-API-KEY en configuración segura
- [x] Crear gateway hacia CapitalPOS CPE API
- [x] Crear endpoint seguro POST /api/cpe/emitir
- [x] Validar usuario, empresa y permisos
- [x] Normalizar respuesta para Angular
- [x] Agregar pruebas del cliente CPE

## Fase 6 — Calidad y operación

- [x] Manejo global de excepciones
- [x] Logs estructurados
- [x] Auditoría de operaciones
- [x] Validaciones de entrada
- [x] Pruebas de integración HTTP
- [x] Revisión de paquetes vulnerables
- [x] Documentación de ejecución

## Fase 7 — Producción

- [x] Configurar base de datos productiva — Preparación segura documentada; aprovisionamiento real queda pendiente del despliegue
- [x] Configurar secretos — Política documentada; integración con gestor real queda pendiente del despliegue
- [x] Configurar HTTPS — Preparación para reverse proxy completada; certificados, dominio y proxy final quedan pendientes del despliegue
- [x] Desplegar API — Preparación documentada; despliegue real pendiente de elegir proveedor y ambiente productivo
- [x] Agregar monitoreo — Preparación documentada; integración real pendiente de elegir proveedor y desplegar la API
- [x] Definir backups — Politica documentada; configuracion real pendiente de proveedor y base productiva
- [x] Ejecutar pruebas end-to-end — Pruebas locales de CapitalPOS API contra capitalpos_test con CPE stub; Angular y CPE real quedan pendientes

## Criterio de MVP terminado

El MVP de CapitalPOS API se considerará terminado cuando:

- un usuario pueda iniciar sesión;
- pertenezca a una empresa;
- tenga un rol válido;
- los datos se almacenen en base de datos;
- los endpoints estén protegidos;
- el backend pueda llamar de forma segura a CapitalPOS CPE API;
- Angular pueda emitir un CPE sin conocer X-API-KEY;
- existan pruebas automáticas y despliegue funcional.
