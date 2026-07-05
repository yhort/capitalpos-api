using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");

        builder.HasKey(empresa => empresa.Id);

        builder.Property(empresa => empresa.Id)
            .ValueGeneratedNever();

        builder.Property(empresa => empresa.Ruc)
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(empresa => empresa.RazonSocial)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(empresa => empresa.NombreComercial)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(empresa => empresa.Activa)
            .IsRequired();

        builder.Property(empresa => empresa.FechaCreacion)
            .IsRequired();

        builder.HasIndex(empresa => empresa.Ruc)
            .IsUnique();
    }
}
