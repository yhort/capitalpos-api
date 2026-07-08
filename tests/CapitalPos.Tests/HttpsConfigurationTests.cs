namespace CapitalPos.Tests;

public class HttpsConfigurationTests
{
    [Fact]
    public void Program_configura_forwarded_headers_antes_de_https_auth_y_endpoints()
    {
        var source = LeerArchivo("src/CapitalPos.Api/Program.cs");
        var forwardedHeadersOptionsIndex = source.IndexOf(
            "builder.Services.Configure<ForwardedHeadersOptions>",
            StringComparison.Ordinal);
        var forwardedHeadersIndex = source.IndexOf("app.UseForwardedHeaders();", StringComparison.Ordinal);
        var httpsRedirectionIndex = source.IndexOf("app.UseHttpsRedirection();", StringComparison.Ordinal);
        var authenticationIndex = source.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        var endpointsIndex = source.IndexOf("app.MapGet(\"/api/health\"", StringComparison.Ordinal);

        Assert.True(forwardedHeadersOptionsIndex >= 0);
        Assert.True(forwardedHeadersIndex >= 0);
        Assert.True(httpsRedirectionIndex > forwardedHeadersIndex);
        Assert.True(authenticationIndex > forwardedHeadersIndex);
        Assert.True(endpointsIndex > forwardedHeadersIndex);
    }

    [Fact]
    public void Program_habilita_x_forwarded_for_y_x_forwarded_proto()
    {
        var source = LeerArchivo("src/CapitalPos.Api/Program.cs");

        Assert.Contains("ForwardedHeaders.XForwardedFor", source);
        Assert.Contains("ForwardedHeaders.XForwardedProto", source);
    }

    [Fact]
    public void Program_no_confia_en_cualquier_proxy()
    {
        var source = LeerArchivo("src/CapitalPos.Api/Program.cs");

        Assert.DoesNotContain("KnownProxies.Clear()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("KnownNetworks.Clear()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ForwardLimit = null", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_aplica_hsts_solo_fuera_de_development()
    {
        var source = LeerArchivo("src/CapitalPos.Api/Program.cs");
        var nonDevelopmentIndex = source.IndexOf("if (!app.Environment.IsDevelopment())", StringComparison.Ordinal);
        var hstsIndex = source.IndexOf("app.UseHsts();", StringComparison.Ordinal);
        var openApiDevelopmentIndex = source.IndexOf("if (app.Environment.IsDevelopment())", StringComparison.Ordinal);

        Assert.True(nonDevelopmentIndex >= 0);
        Assert.True(hstsIndex > nonDevelopmentIndex);
        Assert.True(openApiDevelopmentIndex > hstsIndex);
    }

    [Fact]
    public void Configuracion_https_no_expone_certificados_ni_secretos()
    {
        var archivos = new[]
        {
            LeerArchivo("src/CapitalPos.Api/Program.cs"),
            LeerArchivo("Docs/Https.md")
        };
        var contenido = string.Join(Environment.NewLine, archivos);

        Assert.DoesNotContain(".pfx", LeerArchivo("src/CapitalPos.Api/Program.cs"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".p12", LeerArchivo("src/CapitalPos.Api/Program.cs"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".key", LeerArchivo("src/CapitalPos.Api/Program.cs"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CertificatePassword", contenido, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", contenido, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Documentacion_https_cubre_reverse_proxy_y_pendientes_de_despliegue()
    {
        var documentacion = LeerArchivo("Docs/Https.md");

        Assert.Contains("TLS debe terminar", documentacion);
        Assert.Contains("X-Forwarded-Proto", documentacion);
        Assert.Contains("X-Forwarded-For", documentacion);
        Assert.Contains("HSTS", documentacion);
        Assert.Contains("redirección HTTP a HTTPS", documentacion);
        Assert.Contains("certificados públicos", documentacion);
        Assert.Contains("administrados y renovados", documentacion);
        Assert.Contains("No almacenar certificados privados", documentacion);
        Assert.Contains("No se debe confiar indiscriminadamente en cualquier proxy", documentacion);
        Assert.Contains("IPs o redes confiables", documentacion);
        Assert.Contains("Pendiente del despliegue", documentacion);
    }

    private static string LeerArchivo(string relativePath)
    {
        return File.ReadAllText(Path.Combine(EncontrarRaizRepo(), relativePath));
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
