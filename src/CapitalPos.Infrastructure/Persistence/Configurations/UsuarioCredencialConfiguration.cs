using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class UsuarioCredencialConfiguration : IEntityTypeConfiguration<UsuarioCredencial>
{
    public void Configure(EntityTypeBuilder<UsuarioCredencial> builder)
    {
        builder.ToTable("usuarios_credenciales");

        builder.HasKey(credencial => credencial.UsuarioId);

        builder.Property(credencial => credencial.UsuarioId)
            .ValueGeneratedNever();

        builder.Property(credencial => credencial.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(credencial => credencial.Algoritmo)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(credencial => credencial.FechaCambio)
            .IsRequired();

        builder.Property(credencial => credencial.Activo)
            .IsRequired();

        builder.Property(credencial => credencial.Bloqueado)
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithOne()
            .HasForeignKey<UsuarioCredencial>(credencial => credencial.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(credencial => new
            {
                credencial.Activo,
                credencial.Bloqueado
            });
    }
}
