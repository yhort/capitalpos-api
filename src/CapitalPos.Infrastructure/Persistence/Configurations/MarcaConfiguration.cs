using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class MarcaConfiguration : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> builder)
    {
        builder.ToTable("marcas");

        builder.HasKey(marca => marca.Id);

        builder.Property(marca => marca.Id)
            .ValueGeneratedNever();

        builder.Property(marca => marca.EmpresaId)
            .IsRequired();

        builder.Property(marca => marca.Nombre)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(marca => marca.Activa)
            .IsRequired();

        builder.Property(marca => marca.FechaCreacion)
            .IsRequired();

        builder.HasIndex(marca => marca.EmpresaId);

        builder.HasAlternateKey(marca => new
        {
            marca.Id,
            marca.EmpresaId
        });

        builder.HasIndex(marca => new
            {
                marca.EmpresaId,
                marca.Nombre
            })
            .IsUnique();

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(marca => marca.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
