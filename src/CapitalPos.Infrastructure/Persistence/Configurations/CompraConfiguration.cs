using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("compras");

        builder.HasKey(compra => compra.Id);

        builder.Property(compra => compra.Id)
            .ValueGeneratedNever();

        builder.Property(compra => compra.EmpresaId)
            .IsRequired();

        builder.Property(compra => compra.SedeId)
            .IsRequired();

        builder.Property(compra => compra.Proveedor)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(compra => compra.TipoComprobante)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(compra => compra.Serie)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(compra => compra.Correlativo)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(compra => compra.FechaCompra)
            .IsRequired();

        builder.Property(compra => compra.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(compra => compra.FechaCreacion)
            .IsRequired();

        builder.HasIndex(compra => compra.EmpresaId);

        builder.HasIndex(compra => new
        {
            compra.EmpresaId,
            compra.SedeId
        });

        builder.HasIndex(compra => new
        {
            compra.EmpresaId,
            compra.TipoComprobante,
            compra.Serie,
            compra.Correlativo
        }).IsUnique();

        builder.HasAlternateKey(compra => new
        {
            compra.Id,
            compra.EmpresaId
        });

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(compra => compra.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sede>()
            .WithMany()
            .HasForeignKey(compra => new
            {
                compra.SedeId,
                compra.EmpresaId
            })
            .HasPrincipalKey(sede => new
            {
                sede.Id,
                sede.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(compra => compra.Detalles)
            .WithOne()
            .HasForeignKey(detalle => new
            {
                detalle.CompraId,
                detalle.EmpresaId
            })
            .HasPrincipalKey(compra => new
            {
                compra.Id,
                compra.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(compra => compra.Detalles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
