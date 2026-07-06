using CapitalPos.Domain;
using CapitalPos.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CapitalPos.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Generar_hash_no_devuelve_password_en_texto_plano()
    {
        var credencial = CrearCredencial();
        var hasher = new AspNetCoreIdentityPasswordHasher();
        const string password = "CapitalPOS#2026";

        var hash = hasher.GenerarHash(credencial, password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void Verificar_devuelve_valido_con_password_correcta()
    {
        var credencial = CrearCredencial();
        var hasher = new AspNetCoreIdentityPasswordHasher();
        const string password = "CapitalPOS#2026";
        var hash = hasher.GenerarHash(credencial, password);
        credencial.CambiarPasswordHash(hash, "ASP.NET Core Identity PasswordHasher");

        var resultado = hasher.Verificar(credencial, password);

        Assert.True(resultado.EsValida);
        Assert.False(resultado.RequiereRehash);
    }

    [Fact]
    public void Verificar_rechaza_password_incorrecta()
    {
        var credencial = CrearCredencial();
        var hasher = new AspNetCoreIdentityPasswordHasher();
        var hash = hasher.GenerarHash(credencial, "CapitalPOS#2026");
        credencial.CambiarPasswordHash(hash, "ASP.NET Core Identity PasswordHasher");

        var resultado = hasher.Verificar(credencial, "otra-password");

        Assert.False(resultado.EsValida);
        Assert.False(resultado.RequiereRehash);
    }

    [Fact]
    public void Verificar_indica_rehash_si_el_hash_requiere_actualizacion()
    {
        var credencial = CrearCredencial();
        const string password = "CapitalPOS#2026";
        var legacyHasher = new PasswordHasher<UsuarioCredencial>(
            Options.Create(new PasswordHasherOptions
            {
                CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
            }));
        var legacyHash = legacyHasher.HashPassword(credencial, password);
        credencial.CambiarPasswordHash(legacyHash, "ASP.NET Core Identity PasswordHasher V2");
        var hasher = new AspNetCoreIdentityPasswordHasher();

        var resultado = hasher.Verificar(credencial, password);

        Assert.True(resultado.EsValida);
        Assert.True(resultado.RequiereRehash);
    }

    private static UsuarioCredencial CrearCredencial()
    {
        return new UsuarioCredencial(
            Guid.NewGuid(),
            "hash-temporal",
            "ASP.NET Core Identity PasswordHasher");
    }
}
