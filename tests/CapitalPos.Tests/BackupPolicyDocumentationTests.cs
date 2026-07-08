namespace CapitalPos.Tests;

public class BackupPolicyDocumentationTests
{
    [Fact]
    public void Documentacion_de_backups_define_alcance_productivo()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("base de datos principal", documentacion);
        Assert.Contains("metadatos necesarios para restauracion", documentacion);
        Assert.Contains("historial de migraciones aplicadas", documentacion);
        Assert.Contains("configuracion operativa necesaria", documentacion);
        Assert.Contains("archivos CPE solo si finalmente se almacenan fuera de la base", documentacion);
        Assert.Contains("decision sobre almacenamiento externo de archivos CPE queda pendiente", documentacion);
    }

    [Fact]
    public void Documentacion_de_backups_define_frecuencia_y_retencion()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("backups automaticos diarios", documentacion);
        Assert.Contains("retencion de corto plazo", documentacion);
        Assert.Contains("retencion semanal y mensual", documentacion);
        Assert.Contains("snapshot o backup previo a migraciones productivas", documentacion);
        Assert.Contains("backup manual antes de cambios de alto riesgo", documentacion);
    }

    [Fact]
    public void Documentacion_de_backups_define_rpo_y_rto()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("RPO objetivo inicial", documentacion);
        Assert.Contains("maximo 24 horas", documentacion);
        Assert.Contains("RTO objetivo inicial", documentacion);
        Assert.Contains("maximo 4 horas", documentacion);
        Assert.Contains("Tiempo maximo esperado de restauracion inicial", documentacion);
        Assert.Contains("ajustarse cuando existan datos reales de uso", documentacion);
    }

    [Fact]
    public void Documentacion_de_backups_define_seguridad()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Cifrar backups en transito y en reposo", documentacion);
        Assert.Contains("Restringir acceso a backups", documentacion);
        Assert.Contains("credenciales separadas", documentacion);
        Assert.Contains("No almacenar secretos en Git", documentacion);
        Assert.Contains("No exponer backups publicamente", documentacion);
        Assert.Contains("Rotar y revocar accesos", documentacion);
        Assert.Contains("Registrar quien ejecuta backups", documentacion);
    }

    [Fact]
    public void Documentacion_de_backups_define_restore_y_validaciones()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("ambiente aislado", documentacion);
        Assert.Contains("Validar integridad", documentacion);
        Assert.Contains("ejecutar migraciones pendientes", documentacion);
        Assert.Contains("corresponde al plan de recuperacion", documentacion);
        Assert.Contains("pruebas funcionales posteriores", documentacion);
        Assert.Contains("Verificar usuarios, empresas y relaciones", documentacion);
        Assert.Contains("Verificar emision CPE si aplica", documentacion);
        Assert.Contains("Declarar la restauracion exitosa", documentacion);
    }

    [Fact]
    public void Documentacion_de_backups_exige_pruebas_periodicas_de_restore()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("al menos trimestralmente", documentacion);
        Assert.Contains("evidencia de fecha, duracion y resultado", documentacion);
        Assert.Contains("Registrar incidencias", documentacion);
        Assert.Contains("acciones correctivas", documentacion);
        Assert.Contains("No considerar valido un backup que nunca haya sido restaurado", documentacion);
    }

    [Fact]
    public void Documentacion_de_backups_define_responsables_y_alertas()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("Responsable de verificar backups", documentacion);
        Assert.Contains("Responsable de autorizar restauraciones", documentacion);
        Assert.Contains("Responsable de revisar fallos", documentacion);
        Assert.Contains("Escalamiento ante perdida de datos", documentacion);
        Assert.Contains("backup fallido", documentacion);
        Assert.Contains("backup no ejecutado", documentacion);
        Assert.Contains("retencion incumplida", documentacion);
        Assert.Contains("restauracion de prueba fallida", documentacion);
        Assert.Contains("almacenamiento proximo al limite", documentacion);
    }

    [Fact]
    public void Documentacion_de_backups_define_criterios_de_proveedor_sin_elegir_uno()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("backups automaticos", documentacion);
        Assert.Contains("restauracion a punto en el tiempo", documentacion);
        Assert.Contains("cifrado", documentacion);
        Assert.Contains("retencion configurable", documentacion);
        Assert.Contains("restauracion en otra instancia", documentacion);
        Assert.Contains("exportacion", documentacion);
        Assert.Contains("region", documentacion);
        Assert.Contains("SLA", documentacion);
        Assert.Contains("costos", documentacion);
        Assert.Contains("auditoria de accesos", documentacion);

        Assert.DoesNotContain("Azure", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AWS", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Railway", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Render", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Supabase", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Fly.io", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Documentacion_de_backups_incluye_plantilla_segura()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("BackupId: <identificador-del-backup>", documentacion);
        Assert.Contains("FechaUtc: <fecha-hora-utc>", documentacion);
        Assert.Contains("MigracionAplicada: <nombre-o-id-de-migracion>", documentacion);
        Assert.Contains("RPOObjetivo: <objetivo-rpo>", documentacion);
        Assert.Contains("RTOObjetivo: <objetivo-rto>", documentacion);
        Assert.Contains("ResultadoRestore: <pendiente|exitoso|fallido>", documentacion);
        Assert.Contains("La plantilla usa placeholders", documentacion);
    }

    [Fact]
    public void Documentacion_de_backups_no_incluye_credenciales_reales()
    {
        var documentacion = LeerDocumento();

        Assert.DoesNotContain("Password=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Username=", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres://", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgresql://", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".pfx", documentacion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".p12", documentacion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Documentacion_de_backups_deja_configuracion_real_pendiente()
    {
        var documentacion = LeerDocumento();

        Assert.Contains("elegir proveedor", documentacion);
        Assert.Contains("configurar backups automaticos reales", documentacion);
        Assert.Contains("configurar retencion real", documentacion);
        Assert.Contains("ejecutar snapshots reales", documentacion);
        Assert.Contains("ejecutar restauraciones reales", documentacion);
    }

    [Fact]
    public void Roadmap_marca_politica_de_backups_y_deja_configuracion_real_pendiente()
    {
        var roadmap = File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "Roadmap.md"));

        Assert.Contains("[x] Definir backups", roadmap);
        Assert.Contains("Politica documentada", roadmap);
        Assert.Contains("configuracion real pendiente de proveedor y base productiva", roadmap);
    }

    private static string LeerDocumento()
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), "Docs", "Backups.md"));
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
