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
- [x] Implementar JWT o cookie segura
- [x] Crear refresh token si aplica — No aplicado por ahora; fuera del MVP actual
- [x] Implementar empresa activa
- [x] Validar roles y permisos
- [x] Proteger endpoints

## Fase 5 — Integración CPE

- [x] Configurar cliente HTTP tipado
- [x] Guardar BaseUrl de CPE API
- [x] Guardar X-API-KEY en configuración segura
- [ ] Crear gateway hacia CapitalPOS CPE API
- [ ] Crear endpoint seguro POST /api/cpe/emitir
- [ ] Validar usuario, empresa y permisos
- [ ] Normalizar respuesta para Angular
- [ ] Agregar pruebas del cliente CPE

## Fase 6 — Calidad y operación

- [ ] Manejo global de excepciones
- [ ] Logs estructurados
- [ ] Auditoría de operaciones
- [ ] Validaciones de entrada
- [ ] Pruebas de integración HTTP
- [ ] Revisión de paquetes vulnerables
- [ ] Documentación de ejecución

## Fase 7 — Producción

- [ ] Configurar base de datos productiva
- [ ] Configurar secretos
- [ ] Configurar HTTPS
- [ ] Desplegar API
- [ ] Agregar monitoreo
- [ ] Definir backups
- [ ] Ejecutar pruebas end-to-end

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
