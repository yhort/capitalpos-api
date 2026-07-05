using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(usuario => usuario.Id);

        builder.Property(usuario => usuario.Id)
            .ValueGeneratedNever();

        builder.Property(usuario => usuario.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(usuario => usuario.Apellido)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(usuario => usuario.Correo)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(usuario => usuario.Activo)
            .IsRequired();

        builder.Property(usuario => usuario.FechaCreacion)
            .IsRequired();

        builder.HasIndex(usuario => usuario.Correo)
            .IsUnique();
    }
}
