using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class SedeConfiguration : IEntityTypeConfiguration<Sede>
{
    public void Configure(EntityTypeBuilder<Sede> builder)
    {
        builder.ToTable("sedes");

        builder.HasKey(sede => sede.Id);

        builder.Property(sede => sede.Id)
            .ValueGeneratedNever();

        builder.Property(sede => sede.EmpresaId)
            .IsRequired();

        builder.Property(sede => sede.Nombre)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(sede => sede.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(sede => sede.CodigoEstablecimiento)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sede => sede.Direccion)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(sede => sede.Distrito)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sede => sede.Provincia)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sede => sede.Departamento)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sede => sede.Activa)
            .IsRequired();

        builder.Property(sede => sede.FechaCreacion)
            .IsRequired();

        builder.HasIndex(sede => sede.EmpresaId);

        builder.HasAlternateKey(sede => new
        {
            sede.Id,
            sede.EmpresaId
        });

        builder.HasIndex(sede => new
        {
            sede.EmpresaId,
            sede.Nombre
        });

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(sede => sede.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
