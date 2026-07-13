using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracionFiscalEmpresaConfiguration : IEntityTypeConfiguration<ConfiguracionFiscalEmpresa>
{
    public void Configure(EntityTypeBuilder<ConfiguracionFiscalEmpresa> builder)
    {
        builder.ToTable("configuraciones_fiscales_empresas");

        builder.HasKey(configuracion => configuracion.EmpresaId);

        builder.Property(configuracion => configuracion.EmpresaId)
            .ValueGeneratedNever();

        builder.Property(configuracion => configuracion.Ruc)
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(configuracion => configuracion.RazonSocial)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(configuracion => configuracion.NombreComercial)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(configuracion => configuracion.Ubigeo)
            .HasMaxLength(6)
            .IsRequired();

        builder.Property(configuracion => configuracion.Direccion)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(configuracion => configuracion.Departamento)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(configuracion => configuracion.Provincia)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(configuracion => configuracion.Distrito)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(configuracion => configuracion.Activa)
            .IsRequired();

        builder.Property(configuracion => configuracion.FechaCreacion)
            .IsRequired();

        builder.HasIndex(configuracion => configuracion.EmpresaId)
            .IsUnique();

        builder.HasOne<Empresa>()
            .WithOne()
            .HasForeignKey<ConfiguracionFiscalEmpresa>(configuracion => configuracion.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
