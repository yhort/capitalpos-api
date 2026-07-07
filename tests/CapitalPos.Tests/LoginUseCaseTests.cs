using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class LoginUseCaseTests
{
    [Fact]
    public async Task Login_devuelve_credenciales_validas_con_correo_y_password_correctos()
    {
        var usuario = CrearUsuario();
        var credencial = CrearCredencial(usuario.Id);
        var useCase = CrearUseCase([usuario], [credencial], passwordValida: true);
        var request = new LoginRequest(" USUARIO@CAPITALPOS.COM ", "password-correcta");

        var resultado = await useCase.EjecutarAsync(request);

        Assert.True(resultado.EsValido);
        Assert.Equal(LoginStatus.CredencialesValidas, resultado.Status);
        Assert.Equal(usuario.Id, resultado.UsuarioId);
        Assert.Equal(usuario.Correo, resultado.Correo);
        Assert.False(resultado.RequiereRehash);
    }

    [Fact]
    public async Task Login_devuelve_usuario_no_encontrado_si_el_correo_no_existe()
    {
        var useCase = CrearUseCase([], [], passwordValida: true);

        var resultado = await useCase.EjecutarAsync(new LoginRequest("nadie@capitalpos.com", "password"));

        Assert.False(resultado.EsValido);
        Assert.Equal(LoginStatus.UsuarioNoEncontrado, resultado.Status);
        Assert.Null(resultado.UsuarioId);
        Assert.Null(resultado.Correo);
    }

    [Fact]
    public async Task Login_devuelve_credencial_no_encontrada_si_el_usuario_no_tiene_credencial()
    {
        var usuario = CrearUsuario();
        var useCase = CrearUseCase([usuario], [], passwordValida: true);

        var resultado = await useCase.EjecutarAsync(new LoginRequest(usuario.Correo, "password"));

        Assert.False(resultado.EsValido);
        Assert.Equal(LoginStatus.CredencialNoEncontrada, resultado.Status);
        Assert.Null(resultado.UsuarioId);
    }

    [Fact]
    public async Task Login_devuelve_credencial_inactiva_si_la_credencial_no_esta_activa()
    {
        var usuario = CrearUsuario();
        var credencial = CrearCredencial(usuario.Id, activa: false);
        var useCase = CrearUseCase([usuario], [credencial], passwordValida: true);

        var resultado = await useCase.EjecutarAsync(new LoginRequest(usuario.Correo, "password"));

        Assert.False(resultado.EsValido);
        Assert.Equal(LoginStatus.CredencialInactiva, resultado.Status);
    }

    [Fact]
    public async Task Login_devuelve_credencial_bloqueada_si_la_credencial_esta_bloqueada()
    {
        var usuario = CrearUsuario();
        var credencial = CrearCredencial(usuario.Id, bloqueada: true);
        var useCase = CrearUseCase([usuario], [credencial], passwordValida: true);

        var resultado = await useCase.EjecutarAsync(new LoginRequest(usuario.Correo, "password"));

        Assert.False(resultado.EsValido);
        Assert.Equal(LoginStatus.CredencialBloqueada, resultado.Status);
    }

    [Fact]
    public async Task Login_devuelve_password_incorrecto_si_la_verificacion_falla()
    {
        var usuario = CrearUsuario();
        var credencial = CrearCredencial(usuario.Id);
        var useCase = CrearUseCase([usuario], [credencial], passwordValida: false);

        var resultado = await useCase.EjecutarAsync(new LoginRequest(usuario.Correo, "password-incorrecta"));

        Assert.False(resultado.EsValido);
        Assert.Equal(LoginStatus.PasswordIncorrecto, resultado.Status);
    }

    [Fact]
    public async Task Login_propaga_rehash_requerido_sin_devolver_datos_sensibles()
    {
        var usuario = CrearUsuario();
        var credencial = CrearCredencial(usuario.Id);
        var useCase = CrearUseCase([usuario], [credencial], passwordValida: true, requiereRehash: true);

        var resultado = await useCase.EjecutarAsync(new LoginRequest(usuario.Correo, "password"));

        Assert.True(resultado.EsValido);
        Assert.True(resultado.RequiereRehash);
        Assert.Equal(usuario.Id, resultado.UsuarioId);
        Assert.Equal(usuario.Correo, resultado.Correo);
    }

    private static LoginUseCase CrearUseCase(
        IReadOnlyCollection<Usuario> usuarios,
        IReadOnlyCollection<UsuarioCredencial> credenciales,
        bool passwordValida,
        bool requiereRehash = false)
    {
        return new LoginUseCase(
            new UsuarioRepositoryFake(usuarios),
            new UsuarioCredencialRepositoryFake(credenciales),
            new PasswordHasherFake(passwordValida, requiereRehash));
    }

    private static Usuario CrearUsuario()
    {
        return new Usuario(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "usuario@capitalpos.com");
    }

    private static UsuarioCredencial CrearCredencial(
        Guid usuarioId,
        bool activa = true,
        bool bloqueada = false)
    {
        return new UsuarioCredencial(
            usuarioId,
            "hash-no-sensible",
            "ASP.NET Core Identity PasswordHasher",
            activo: activa,
            bloqueado: bloqueada);
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        private readonly IReadOnlyCollection<Usuario> _usuarios;

        public UsuarioRepositoryFake(IReadOnlyCollection<Usuario> usuarios)
        {
            _usuarios = usuarios;
        }

        public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var usuario = _usuarios.SingleOrDefault(usuario => usuario.Id == id);

            return Task.FromResult(usuario);
        }

        public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            var correoNormalizado = correo.Trim().ToLowerInvariant();
            var usuario = _usuarios.SingleOrDefault(usuario => usuario.Correo == correoNormalizado);

            return Task.FromResult(usuario);
        }

        public Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class UsuarioCredencialRepositoryFake : IUsuarioCredencialRepository
    {
        private readonly IReadOnlyCollection<UsuarioCredencial> _credenciales;

        public UsuarioCredencialRepositoryFake(IReadOnlyCollection<UsuarioCredencial> credenciales)
        {
            _credenciales = credenciales;
        }

        public Task<UsuarioCredencial?> ObtenerPorUsuarioIdAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default)
        {
            var credencial = _credenciales.SingleOrDefault(credencial => credencial.UsuarioId == usuarioId);

            return Task.FromResult(credencial);
        }
    }

    private sealed class PasswordHasherFake : IPasswordHasher
    {
        private readonly bool _esValida;
        private readonly bool _requiereRehash;

        public PasswordHasherFake(bool esValida, bool requiereRehash)
        {
            _esValida = esValida;
            _requiereRehash = requiereRehash;
        }

        public string GenerarHash(UsuarioCredencial credencial, string password)
        {
            throw new NotSupportedException();
        }

        public PasswordVerificationResult Verificar(UsuarioCredencial credencial, string password)
        {
            return new PasswordVerificationResult(_esValida, _esValida && _requiereRehash);
        }
    }
}
