using CapitalPos.Application.Empresas;
using CapitalPos.Application.Usuarios;
using CapitalPos.Domain;

namespace CapitalPos.Tests;

public class ApplicationUsuarioEmpresaTests
{
    [Fact]
    public async Task Crear_usuario_use_case_construye_y_guarda_usuario_valido()
    {
        var repository = new UsuarioRepositoryFake();
        var useCase = new CrearUsuarioUseCase(repository);
        var request = new CrearUsuarioRequest(
            "Grace",
            "Hopper",
            " GRACE@CAPITALPOS.COM ");

        var usuario = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, usuario.Id);
        Assert.Equal("Grace", usuario.Nombre);
        Assert.Equal("Hopper", usuario.Apellido);
        Assert.Equal("grace@capitalpos.com", usuario.Correo);
        Assert.True(usuario.Activo);
        Assert.Same(usuario, repository.Usuarios.Single());
    }

    [Fact]
    public async Task Crear_usuario_use_case_propaga_reglas_de_dominio()
    {
        var repository = new UsuarioRepositoryFake();
        var useCase = new CrearUsuarioUseCase(repository);
        var request = new CrearUsuarioRequest("Grace", "Hopper", "");

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Usuarios);
    }

    [Fact]
    public async Task Crear_usuario_use_case_rechaza_correo_duplicado()
    {
        var repository = new UsuarioRepositoryFake();
        await repository.AgregarAsync(new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com"));
        var useCase = new CrearUsuarioUseCase(repository);
        var request = new CrearUsuarioRequest(
            "Ada",
            "Lovelace",
            " GRACE@CAPITALPOS.COM ");

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Single(repository.Usuarios);
    }

    [Fact]
    public async Task Listar_usuarios_use_case_devuelve_usuarios_guardados()
    {
        var repository = new UsuarioRepositoryFake();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com");
        await repository.AgregarAsync(usuario);
        var useCase = new ListarUsuariosUseCase(repository);

        var usuarios = await useCase.EjecutarAsync();

        Assert.Same(usuario, usuarios.Single());
    }

    [Fact]
    public async Task Obtener_usuario_por_id_use_case_devuelve_usuario_guardado()
    {
        var repository = new UsuarioRepositoryFake();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com");
        await repository.AgregarAsync(usuario);
        var useCase = new ObtenerUsuarioPorIdUseCase(repository);

        var usuarioEncontrado = await useCase.EjecutarAsync(usuario.Id);

        Assert.Same(usuario, usuarioEncontrado);
    }

    [Fact]
    public async Task Obtener_usuario_por_id_use_case_devuelve_null_si_no_existe()
    {
        var repository = new UsuarioRepositoryFake();
        var useCase = new ObtenerUsuarioPorIdUseCase(repository);

        var usuario = await useCase.EjecutarAsync(Guid.NewGuid());

        Assert.Null(usuario);
    }

    [Fact]
    public async Task Desactivar_usuario_use_case_cambia_estado_y_actualiza_repositorio()
    {
        var repository = new UsuarioRepositoryFake();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com");
        await repository.AgregarAsync(usuario);
        var useCase = new DesactivarUsuarioUseCase(repository);

        var usuarioDesactivado = await useCase.EjecutarAsync(usuario.Id);

        Assert.Same(usuario, usuarioDesactivado);
        Assert.False(usuario.Activo);
        Assert.Same(usuario, repository.UsuarioActualizado);
    }

    [Fact]
    public async Task Activar_usuario_use_case_cambia_estado_y_actualiza_repositorio()
    {
        var repository = new UsuarioRepositoryFake();
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "grace@capitalpos.com",
            activo: false);
        await repository.AgregarAsync(usuario);
        var useCase = new ActivarUsuarioUseCase(repository);

        var usuarioActivado = await useCase.EjecutarAsync(usuario.Id);

        Assert.Same(usuario, usuarioActivado);
        Assert.True(usuario.Activo);
        Assert.Same(usuario, repository.UsuarioActualizado);
    }

    [Fact]
    public async Task Asignar_usuario_empresa_use_case_construye_y_guarda_relacion_valida()
    {
        var usuarioRepository = new UsuarioRepositoryFake();
        var empresaRepository = new EmpresaRepositoryFake();
        var repository = new UsuarioEmpresaRepositoryFake();
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        await usuarioRepository.AgregarAsync(new Usuario(
            usuarioId,
            "Grace",
            "Hopper",
            "grace@capitalpos.com"));
        await empresaRepository.AgregarAsync(new Empresa(
            empresaId,
            "20606264004",
            "CapitalPOS SAC"));
        var useCase = new AsignarUsuarioEmpresaUseCase(
            repository,
            usuarioRepository,
            empresaRepository);
        var request = new AsignarUsuarioEmpresaRequest(
            usuarioId,
            empresaId,
            RolEmpresa.Cajero);

        var asignacion = await useCase.EjecutarAsync(request);

        Assert.NotEqual(Guid.Empty, asignacion.Id);
        Assert.Equal(usuarioId, asignacion.UsuarioId);
        Assert.Equal(empresaId, asignacion.EmpresaId);
        Assert.Equal(RolEmpresa.Cajero, asignacion.Rol);
        Assert.True(asignacion.Activo);
        Assert.Same(asignacion, repository.Asignaciones.Single());
    }

    [Fact]
    public async Task Asignar_usuario_empresa_use_case_propaga_reglas_de_dominio()
    {
        var usuarioRepository = new UsuarioRepositoryFake();
        var empresaRepository = new EmpresaRepositoryFake();
        var repository = new UsuarioEmpresaRepositoryFake();
        var useCase = new AsignarUsuarioEmpresaUseCase(
            repository,
            usuarioRepository,
            empresaRepository);
        var request = new AsignarUsuarioEmpresaRequest(
            Guid.Empty,
            Guid.NewGuid(),
            RolEmpresa.Vendedor);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Asignaciones);
    }

    [Fact]
    public async Task Asignar_usuario_empresa_use_case_rechaza_asignacion_duplicada()
    {
        var usuarioRepository = new UsuarioRepositoryFake();
        var empresaRepository = new EmpresaRepositoryFake();
        var repository = new UsuarioEmpresaRepositoryFake();
        var usuarioId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        await usuarioRepository.AgregarAsync(new Usuario(
            usuarioId,
            "Grace",
            "Hopper",
            "grace@capitalpos.com"));
        await empresaRepository.AgregarAsync(new Empresa(
            empresaId,
            "20606264004",
            "CapitalPOS SAC"));
        await repository.AgregarAsync(new UsuarioEmpresa(
            Guid.NewGuid(),
            usuarioId,
            empresaId,
            RolEmpresa.Cajero));
        var useCase = new AsignarUsuarioEmpresaUseCase(
            repository,
            usuarioRepository,
            empresaRepository);
        var request = new AsignarUsuarioEmpresaRequest(
            usuarioId,
            empresaId,
            RolEmpresa.Administrador);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Single(repository.Asignaciones);
    }

    [Fact]
    public async Task Asignar_usuario_empresa_use_case_rechaza_usuario_inexistente()
    {
        var usuarioRepository = new UsuarioRepositoryFake();
        var empresaRepository = new EmpresaRepositoryFake();
        var repository = new UsuarioEmpresaRepositoryFake();
        var empresaId = Guid.NewGuid();
        await empresaRepository.AgregarAsync(new Empresa(
            empresaId,
            "20606264004",
            "CapitalPOS SAC"));
        var useCase = new AsignarUsuarioEmpresaUseCase(
            repository,
            usuarioRepository,
            empresaRepository);
        var request = new AsignarUsuarioEmpresaRequest(
            Guid.NewGuid(),
            empresaId,
            RolEmpresa.Cajero);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Asignaciones);
    }

    [Fact]
    public async Task Asignar_usuario_empresa_use_case_rechaza_empresa_inexistente()
    {
        var usuarioRepository = new UsuarioRepositoryFake();
        var empresaRepository = new EmpresaRepositoryFake();
        var repository = new UsuarioEmpresaRepositoryFake();
        var usuarioId = Guid.NewGuid();
        await usuarioRepository.AgregarAsync(new Usuario(
            usuarioId,
            "Grace",
            "Hopper",
            "grace@capitalpos.com"));
        var useCase = new AsignarUsuarioEmpresaUseCase(
            repository,
            usuarioRepository,
            empresaRepository);
        var request = new AsignarUsuarioEmpresaRequest(
            usuarioId,
            Guid.NewGuid(),
            RolEmpresa.Cajero);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Empty(repository.Asignaciones);
    }

    [Fact]
    public async Task Listar_usuarios_empresa_use_case_devuelve_asignaciones_guardadas()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Contador);
        await repository.AgregarAsync(asignacion);
        var useCase = new ListarUsuariosEmpresaUseCase(repository);

        var asignaciones = await useCase.EjecutarAsync();

        Assert.Same(asignacion, asignaciones.Single());
    }

    [Fact]
    public async Task Obtener_usuario_empresa_por_id_use_case_devuelve_asignacion_guardada()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Contador);
        await repository.AgregarAsync(asignacion);
        var useCase = new ObtenerUsuarioEmpresaPorIdUseCase(repository);

        var asignacionEncontrada = await useCase.EjecutarAsync(asignacion.Id);

        Assert.Same(asignacion, asignacionEncontrada);
    }

    [Fact]
    public async Task Obtener_usuario_empresa_por_id_use_case_devuelve_null_si_no_existe()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var useCase = new ObtenerUsuarioEmpresaPorIdUseCase(repository);

        var asignacion = await useCase.EjecutarAsync(Guid.NewGuid());

        Assert.Null(asignacion);
    }

    [Fact]
    public async Task Desactivar_usuario_empresa_use_case_cambia_estado_y_actualiza_repositorio()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Contador);
        await repository.AgregarAsync(asignacion);
        var useCase = new DesactivarUsuarioEmpresaUseCase(repository);

        var asignacionDesactivada = await useCase.EjecutarAsync(asignacion.Id);

        Assert.Same(asignacion, asignacionDesactivada);
        Assert.False(asignacion.Activo);
        Assert.Same(asignacion, repository.AsignacionActualizada);
    }

    [Fact]
    public async Task Activar_usuario_empresa_use_case_cambia_estado_y_actualiza_repositorio()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Contador,
            activo: false);
        await repository.AgregarAsync(asignacion);
        var useCase = new ActivarUsuarioEmpresaUseCase(repository);

        var asignacionActivada = await useCase.EjecutarAsync(asignacion.Id);

        Assert.Same(asignacion, asignacionActivada);
        Assert.True(asignacion.Activo);
        Assert.Same(asignacion, repository.AsignacionActualizada);
    }

    [Fact]
    public async Task Cambiar_rol_usuario_empresa_use_case_cambia_rol_y_actualiza_repositorio()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);
        await repository.AgregarAsync(asignacion);
        var useCase = new CambiarRolUsuarioEmpresaUseCase(repository);
        var request = new CambiarRolUsuarioEmpresaRequest(RolEmpresa.Contador);

        var asignacionActualizada = await useCase.EjecutarAsync(asignacion.Id, request);

        Assert.Same(asignacion, asignacionActualizada);
        Assert.Equal(RolEmpresa.Contador, asignacion.Rol);
        Assert.Same(asignacion, repository.AsignacionActualizada);
    }

    [Fact]
    public async Task Cambiar_rol_usuario_empresa_use_case_devuelve_null_si_no_existe()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var useCase = new CambiarRolUsuarioEmpresaUseCase(repository);
        var request = new CambiarRolUsuarioEmpresaRequest(RolEmpresa.Vendedor);

        var asignacion = await useCase.EjecutarAsync(Guid.NewGuid(), request);

        Assert.Null(asignacion);
        Assert.Null(repository.AsignacionActualizada);
    }

    [Fact]
    public async Task Cambiar_rol_usuario_empresa_use_case_propaga_rol_invalido()
    {
        var repository = new UsuarioEmpresaRepositoryFake();
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RolEmpresa.Cajero);
        await repository.AgregarAsync(asignacion);
        var useCase = new CambiarRolUsuarioEmpresaUseCase(repository);
        var request = new CambiarRolUsuarioEmpresaRequest((RolEmpresa)999);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            useCase.EjecutarAsync(asignacion.Id, request));
        Assert.Equal(RolEmpresa.Cajero, asignacion.Rol);
        Assert.Null(repository.AsignacionActualizada);
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public List<Usuario> Usuarios { get; } = new();

        public Usuario? UsuarioActualizado { get; private set; }

        public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Usuarios.Add(usuario);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Usuario>>(Usuarios);
        }

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var usuario = Usuarios.SingleOrDefault(usuario => usuario.Id == id);

            return Task.FromResult(usuario);
        }

        public Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            UsuarioActualizado = usuario;

            return Task.CompletedTask;
        }

        public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            var existe = Usuarios.Any(usuario => usuario.Correo == correo);

            return Task.FromResult(existe);
        }
    }

    private sealed class EmpresaRepositoryFake : IEmpresaRepository
    {
        public List<Empresa> Empresas { get; } = new();

        public Empresa? EmpresaActualizada { get; private set; }

        public Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            Empresas.Add(empresa);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Empresa>> ListarAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Empresa>>(Empresas);
        }

        public Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var empresa = Empresas.SingleOrDefault(empresa => empresa.Id == id);

            return Task.FromResult(empresa);
        }

        public Task ActualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            EmpresaActualizada = empresa;

            return Task.CompletedTask;
        }

        public Task<bool> ExisteRucAsync(string ruc, CancellationToken cancellationToken = default)
        {
            var existe = Empresas.Any(empresa => empresa.Ruc == ruc);

            return Task.FromResult(existe);
        }
    }

    private sealed class UsuarioEmpresaRepositoryFake : IUsuarioEmpresaRepository
    {
        public List<UsuarioEmpresa> Asignaciones { get; } = new();

        public UsuarioEmpresa? AsignacionActualizada { get; private set; }

        public Task AgregarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            Asignaciones.Add(usuarioEmpresa);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<UsuarioEmpresa>> ListarAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<UsuarioEmpresa>>(Asignaciones);
        }

        public Task<UsuarioEmpresa?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var asignacion = Asignaciones.SingleOrDefault(asignacion => asignacion.Id == id);

            return Task.FromResult(asignacion);
        }

        public Task ActualizarAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken = default)
        {
            AsignacionActualizada = usuarioEmpresa;

            return Task.CompletedTask;
        }

        public Task<bool> ExisteAsignacionAsync(
            Guid usuarioId,
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            var existe = Asignaciones.Any(asignacion =>
                asignacion.UsuarioId == usuarioId &&
                asignacion.EmpresaId == empresaId);

            return Task.FromResult(existe);
        }
    }
}
