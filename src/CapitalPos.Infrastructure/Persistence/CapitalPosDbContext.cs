using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence;

public sealed class CapitalPosDbContext : DbContext
{
    public CapitalPosDbContext(DbContextOptions<CapitalPosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<UsuarioCredencial> UsuariosCredenciales => Set<UsuarioCredencial>();

    public DbSet<UsuarioEmpresa> UsuariosEmpresa => Set<UsuarioEmpresa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CapitalPosDbContext).Assembly);
    }
}
