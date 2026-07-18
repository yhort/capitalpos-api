using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class PuntoVentaConfiguration : IEntityTypeConfiguration<PuntoVenta>
{
    public void Configure(EntityTypeBuilder<PuntoVenta> builder)
    {
        builder.ToTable("puntos_venta");

        builder.HasKey(puntoVenta => puntoVenta.Id);

        builder.Property(puntoVenta => puntoVenta.Id)
            .ValueGeneratedNever();

        builder.Property(puntoVenta => puntoVenta.EmpresaId)
            .IsRequired();

        builder.Property(puntoVenta => puntoVenta.SedeId)
            .IsRequired();

        builder.Property(puntoVenta => puntoVenta.Nombre)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(puntoVenta => puntoVenta.Activo)
            .IsRequired();

        builder.Property(puntoVenta => puntoVenta.FechaCreacion)
            .IsRequired();

        builder.HasIndex(puntoVenta => puntoVenta.EmpresaId);

        builder.HasIndex(puntoVenta => new
        {
            puntoVenta.EmpresaId,
            puntoVenta.SedeId
        });

        builder.HasAlternateKey(puntoVenta => new
        {
            puntoVenta.Id,
            puntoVenta.EmpresaId
        });

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(puntoVenta => puntoVenta.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sede>()
            .WithMany()
            .HasForeignKey(puntoVenta => new
            {
                puntoVenta.SedeId,
                puntoVenta.EmpresaId
            })
            .HasPrincipalKey(sede => new
            {
                sede.Id,
                sede.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
