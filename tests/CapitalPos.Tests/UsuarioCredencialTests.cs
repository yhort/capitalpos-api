using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class UsuarioCredencialTests
{
    [Fact]
    public void Crear_credencial_valida()
    {
        var usuarioId = Guid.NewGuid();
        var fechaCambio = DateTimeOffset.UtcNow;

        var credencial = new UsuarioCredencial(
            usuarioId,
            " hash-con-salt-incorporado ",
            " PBKDF2 ",
            fechaCambio);

        Assert.Equal(usuarioId, credencial.UsuarioId);
        Assert.Equal("hash-con-salt-incorporado", credencial.PasswordHash);
        Assert.Equal("PBKDF2", credencial.Algoritmo);
        Assert.Equal(fechaCambio, credencial.FechaCambio);
        Assert.True(credencial.Activo);
        Assert.False(credencial.Bloqueado);
    }

    [Fact]
    public void Rechaza_usuario_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new UsuarioCredencial(Guid.Empty, "hash", "PBKDF2"));
    }

    [Fact]
    public void Rechaza_hash_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new UsuarioCredencial(Guid.NewGuid(), " ", "PBKDF2"));
    }

    [Fact]
    public void Rechaza_algoritmo_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new UsuarioCredencial(Guid.NewGuid(), "hash", " "));
    }

    [Fact]
    public void Permite_bloquear_desbloquear_activar_y_desactivar()
    {
        var credencial = new UsuarioCredencial(Guid.NewGuid(), "hash", "PBKDF2");

        credencial.Bloquear();
        credencial.Desactivar();

        Assert.True(credencial.Bloqueado);
        Assert.False(credencial.Activo);

        credencial.Desbloquear();
        credencial.Activar();

        Assert.False(credencial.Bloqueado);
        Assert.True(credencial.Activo);
    }

    [Fact]
    public void Cambiar_password_hash_actualiza_hash_algoritmo_y_fecha()
    {
        var credencial = new UsuarioCredencial(Guid.NewGuid(), "hash-anterior", "PBKDF2");
        var fechaCambio = DateTimeOffset.UtcNow.AddMinutes(1);

        credencial.CambiarPasswordHash("hash-nuevo", "Argon2id", fechaCambio);

        Assert.Equal("hash-nuevo", credencial.PasswordHash);
        Assert.Equal("Argon2id", credencial.Algoritmo);
        Assert.Equal(fechaCambio, credencial.FechaCambio);
    }
}
