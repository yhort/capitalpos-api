using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class UnidadMedidaConfiguration : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> builder)
    {
        builder.ToTable("unidades_medida");

        builder.HasKey(unidadMedida => unidadMedida.Id);

        builder.Property(unidadMedida => unidadMedida.Id)
            .ValueGeneratedNever();

        builder.Property(unidadMedida => unidadMedida.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(unidadMedida => unidadMedida.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(unidadMedida => unidadMedida.Activa)
            .IsRequired();

        builder.Property(unidadMedida => unidadMedida.FechaCreacion)
            .IsRequired();

        builder.HasIndex(unidadMedida => unidadMedida.Codigo)
            .IsUnique();
    }
}
