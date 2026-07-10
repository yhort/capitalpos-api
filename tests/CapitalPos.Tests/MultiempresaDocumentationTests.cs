namespace CapitalPos.Tests;

public class MultiempresaDocumentationTests
{
    [Fact]
    public void Documentacion_multiempresa_define_empresa_id_obligatorio_para_pos()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Toda entidad POS transaccional o configurable por empresa debe tener", documentacion);
        Assert.Contains("`EmpresaId` obligatorio", documentacion);
        Assert.Contains("X-CapitalPos-EmpresaId", documentacion);
        Assert.Contains("EmpresaActivaEndpointFilter", documentacion);
        Assert.Contains("IEmpresaActivaContext", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_lista_entidades_con_y_sin_empresa_id()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("productos", documentacion);
        Assert.Contains("categorias", documentacion);
        Assert.Contains("almacenes", documentacion);
        Assert.Contains("stock", documentacion);
        Assert.Contains("ventas", documentacion);
        Assert.Contains("comprobantes", documentacion);
        Assert.Contains("caja", documentacion);
        Assert.Contains("compras", documentacion);
        Assert.Contains("clientes y proveedores", documentacion);
        Assert.Contains("series", documentacion);
        Assert.Contains("configuracion fiscal", documentacion);
        Assert.Contains("reportes materializados", documentacion);
        Assert.Contains("usuarios globales", documentacion);
        Assert.Contains("credenciales", documentacion);
        Assert.Contains("roles de plataforma", documentacion);
        Assert.Contains("catalogos SUNAT globales", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_define_reglas_de_repositorios_ef_endpoints_y_pruebas()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Nunca consultar datos POS sin empresa activa.", documentacion);
        Assert.Contains("Filtrar siempre por `EmpresaId`", documentacion);
        Assert.Contains("No aceptar `EmpresaId` libre desde el frontend", documentacion);
        Assert.Contains("Validar pertenencia usuario-empresa", documentacion);
        Assert.Contains("FK hacia `Empresa`", documentacion);
        Assert.Contains("indice por `EmpresaId`", documentacion);
        Assert.Contains("indices unicos de negocio deben ser compuestos por `EmpresaId`", documentacion);
        Assert.Contains("DeleteBehavior.Restrict", documentacion);
        Assert.Contains("EmpresaActivaEndpointFilter", documentacion);
        Assert.Contains("RequirePermisoEmpresa", documentacion);
        Assert.Contains("roles de plataforma SaaS y los roles dentro de una empresa", documentacion);
        Assert.Contains("pruebas anti-fuga multiempresa", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_define_patron_operativo_de_filtrado_por_empresa_activa()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Patron operativo de filtrado por empresa activa", documentacion);
        Assert.Contains("Endpoint autenticado", documentacion);
        Assert.Contains("EmpresaActivaEndpointFilter", documentacion);
        Assert.Contains("RequirePermisoEmpresa", documentacion);
        Assert.Contains("Use case con IEmpresaActivaContext.EmpresaId", documentacion);
        Assert.Contains("Repository con metodos por empresa", documentacion);
        Assert.Contains("EF Core con filtro EmpresaId", documentacion);
        Assert.Contains("Los permisos se evaluan despues de establecer empresa activa", documentacion);
        Assert.Contains("no debe mapear un `EmpresaId` del payload", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_define_patron_para_use_cases_y_repositorios()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Debe fallar si `IEmpresaActivaContext.TieneEmpresaActiva` es `false`", documentacion);
        Assert.Contains("usar `IEmpresaActivaContext.EmpresaId` como unica fuente", documentacion);
        Assert.Contains("ListarPorEmpresaAsync(Guid empresaId", documentacion);
        Assert.Contains("ObtenerPorIdAsync(Guid empresaId, Guid id", documentacion);
        Assert.Contains("ExisteCodigoAsync(Guid empresaId, string codigo", documentacion);
        Assert.Contains("`GetById` debe incluir `EmpresaId`", documentacion);
        Assert.Contains("Toda consulta debe incluir predicado por `EmpresaId`", documentacion);
        Assert.Contains("`empresaId` incorrecto debe devolver `null`", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_separa_roles_de_plataforma_y_roles_de_empresa()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Roles de plataforma vs roles de empresa", documentacion);
        Assert.Contains("Un rol de plataforma opera sobre el SaaS completo", documentacion);
        Assert.Contains("no deben depender de `X-CapitalPos-EmpresaId`", documentacion);
        Assert.Contains("no deben depender de `UsuarioEmpresa`", documentacion);
        Assert.Contains("no deben usar `RolEmpresa`", documentacion);
        Assert.Contains("no deben protegerse con `PermisoEmpresa`", documentacion);
        Assert.Contains("Un rol de empresa opera dentro de una empresa activa", documentacion);
        Assert.Contains("relacion `UsuarioEmpresa` activa", documentacion);
        Assert.Contains("Los permisos de empresa solo autorizan operaciones dentro de la empresa activa", documentacion);
        Assert.Contains("`RolEmpresa.Administrador` significa administrador dentro de una empresa", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_marca_endpoints_actuales_para_revision_api_011()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Implicancias para endpoints actuales", documentacion);
        Assert.Contains("Clasificacion API-011 de endpoints actuales", documentacion);
        Assert.Contains("API-011 debe", documentacion);
        Assert.Contains("`/api/empresas`", documentacion);
        Assert.Contains("listar, crear, activar y desactivar empresas parecen operaciones de", documentacion);
        Assert.Contains("`/api/usuarios`", documentacion);
        Assert.Contains("los usuarios son identidades globales", documentacion);
        Assert.Contains("`/api/usuarios-empresas`", documentacion);
        Assert.Contains("gestiona relaciones entre usuario y empresa", documentacion);
        Assert.Contains("no acepten `EmpresaId` libre", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_clasifica_todos_los_endpoints_de_empresas_y_usuarios()
    {
        var documentacion = LeerDocumento();
        var endpoints = new[]
        {
            "`GET /api/empresas`",
            "`GET /api/empresas/{id}`",
            "`POST /api/empresas`",
            "`PATCH /api/empresas/{id}/activar`",
            "`PATCH /api/empresas/{id}/desactivar`",
            "`GET /api/usuarios`",
            "`GET /api/usuarios/{id}`",
            "`POST /api/usuarios`",
            "`PATCH /api/usuarios/{id}/activar`",
            "`PATCH /api/usuarios/{id}/desactivar`",
            "`GET /api/usuarios-empresas`",
            "`GET /api/usuarios-empresas/{id}`",
            "`POST /api/usuarios-empresas`",
            "`PATCH /api/usuarios-empresas/{id}/activar`",
            "`PATCH /api/usuarios-empresas/{id}/desactivar`",
            "`PATCH /api/usuarios-empresas/{id}/rol`"
        };

        foreach (var endpoint in endpoints)
        {
            Assert.Contains(endpoint, documentacion);
        }
    }

    [Fact]
    public void Documentacion_multiempresa_define_riesgo_filtro_y_permiso_por_clasificacion()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Clasificacion recomendada", documentacion);
        Assert.Contains("Riesgo actual", documentacion);
        Assert.Contains("Cambio recomendado futuro", documentacion);
        Assert.Contains("EmpresaActivaEndpointFilter", documentacion);
        Assert.Contains("Permisos", documentacion);
        Assert.Contains("Plataforma global", documentacion);
        Assert.Contains("Empresa activa", documentacion);
        Assert.Contains("Mixto o requiere rediseño", documentacion);
        Assert.Contains("No.", documentacion);
        Assert.Contains("Si.", documentacion);
        Assert.Contains("Plataforma.", documentacion);
        Assert.Contains("Empresa.", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_prioriza_endpoints_criticos_para_correccion()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Prioridad recomendada para correccion", documentacion);
        Assert.Contains("`PATCH /api/empresas/{id}/desactivar`", documentacion);
        Assert.Contains("`POST /api/empresas`", documentacion);
        Assert.Contains("`PATCH /api/usuarios/{id}/desactivar`", documentacion);
        Assert.Contains("`POST /api/usuarios-empresas`", documentacion);
        Assert.Contains("Listados y detalles (`GET`)", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_define_criterios_de_aceptacion_para_modulos_pos()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Criterios de aceptacion para un modulo POS nuevo", documentacion);
        Assert.Contains("Toda entidad POS transaccional o configurable por empresa tiene `EmpresaId`", documentacion);
        Assert.Contains("La configuracion EF define `EmpresaId` obligatorio", documentacion);
        Assert.Contains("Los endpoints usan autenticacion, `EmpresaActivaEndpointFilter` y permisos", documentacion);
        Assert.Contains("empresa A no lee ni", documentacion);
        Assert.Contains("modifica datos de empresa B", documentacion);
    }

    [Fact]
    public void Documentacion_multiempresa_exige_pruebas_anti_fuga_para_modulos_pos()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("los listados devuelven solo datos de la empresa activa", documentacion);
        Assert.Contains("`GetById` con empresa incorrecta devuelve `null`, `404`", documentacion);
        Assert.Contains("las creaciones asignan `EmpresaId` desde el contexto", documentacion);
        Assert.Contains("payload con `EmpresaId` ajeno se ignora o rechaza", documentacion);
    }

    private static string LeerDocumento()
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "Multiempresa.md"));
    }

    private static string EncontrarRaizRepo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CapitalPos.Api.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo encontrar la raiz del repositorio.");
    }
}
