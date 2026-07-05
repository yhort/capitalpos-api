using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class UsuarioEmpresaConfiguration : IEntityTypeConfiguration<UsuarioEmpresa>
{
    public void Configure(EntityTypeBuilder<UsuarioEmpresa> builder)
    {
        builder.ToTable("usuarios_empresas");

        builder.HasKey(usuarioEmpresa => usuarioEmpresa.Id);

        builder.Property(usuarioEmpresa => usuarioEmpresa.Id)
            .ValueGeneratedNever();

        builder.Property(usuarioEmpresa => usuarioEmpresa.UsuarioId)
            .IsRequired();

        builder.Property(usuarioEmpresa => usuarioEmpresa.EmpresaId)
            .IsRequired();

        builder.Property(usuarioEmpresa => usuarioEmpresa.Rol)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(usuarioEmpresa => usuarioEmpresa.Activo)
            .IsRequired();

        builder.Property(usuarioEmpresa => usuarioEmpresa.FechaAsignacion)
            .IsRequired();

        builder.HasIndex(usuarioEmpresa => new
            {
                usuarioEmpresa.UsuarioId,
                usuarioEmpresa.EmpresaId
            })
            .IsUnique();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(usuarioEmpresa => usuarioEmpresa.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(usuarioEmpresa => usuarioEmpresa.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
