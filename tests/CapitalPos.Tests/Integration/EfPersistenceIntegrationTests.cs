using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Tests.Integration;

public class EfPersistenceIntegrationTests
{
    [PostgreSqlIntegrationFact]
    public async Task Repositorios_ef_persisten_y_consultan_modelo_multiempresa()
    {
        await using var context = PostgreSqlTestDatabase.CreateContext();
        await context.Database.MigrateAsync();

        var empresaRepository = new EfEmpresaRepository(context);
        var usuarioRepository = new EfUsuarioRepository(context);
        var usuarioEmpresaRepository = new EfUsuarioEmpresaRepository(context);

        var empresa = new Empresa(
            Guid.NewGuid(),
            CrearRucUnico(),
            "CapitalPOS Integration Test",
            "CapitalPOS Test");
        var usuario = new Usuario(
            Guid.NewGuid(),
            "Integration",
            "Test",
            $"integration-{Guid.NewGuid():N}@capitalpos.test");
        var asignacion = new UsuarioEmpresa(
            Guid.NewGuid(),
            usuario.Id,
            empresa.Id,
            RolEmpresa.Administrador);

        try
        {
            await empresaRepository.AgregarAsync(empresa);
            await usuarioRepository.AgregarAsync(usuario);
            await usuarioEmpresaRepository.AgregarAsync(asignacion);

            var empresaGuardada = await empresaRepository.ObtenerPorIdAsync(empresa.Id);
            var usuarioGuardado = await usuarioRepository.ObtenerPorIdAsync(usuario.Id);
            var asignacionGuardada = await usuarioEmpresaRepository.ObtenerPorIdAsync(asignacion.Id);

            Assert.NotNull(empresaGuardada);
            Assert.NotNull(usuarioGuardado);
            Assert.NotNull(asignacionGuardada);
            Assert.True(await empresaRepository.ExisteRucAsync(empresa.Ruc));
            Assert.True(await usuarioRepository.ExisteCorreoAsync(usuario.Correo));
            Assert.True(await usuarioEmpresaRepository.ExisteAsignacionAsync(usuario.Id, empresa.Id));

            asignacionGuardada.CambiarRol(RolEmpresa.Contador);
            await usuarioEmpresaRepository.ActualizarAsync(asignacionGuardada);

            var asignacionActualizada = await usuarioEmpresaRepository.ObtenerPorIdAsync(asignacion.Id);

            Assert.Equal(RolEmpresa.Contador, asignacionActualizada?.Rol);
        }
        finally
        {
            await LimpiarDatosAsync(context, empresa.Id, usuario.Id, asignacion.Id);
        }
    }

    private static string CrearRucUnico()
    {
        var valor = Math.Abs(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));

        return $"20{valor:D9}"[..11];
    }

    private static async Task LimpiarDatosAsync(
        DbContext context,
        Guid empresaId,
        Guid usuarioId,
        Guid asignacionId)
    {
        await context.Set<UsuarioEmpresa>()
            .Where(usuarioEmpresa => usuarioEmpresa.Id == asignacionId)
            .ExecuteDeleteAsync();
        await context.Set<Usuario>()
            .Where(usuario => usuario.Id == usuarioId)
            .ExecuteDeleteAsync();
        await context.Set<Empresa>()
            .Where(empresa => empresa.Id == empresaId)
            .ExecuteDeleteAsync();
    }
}
