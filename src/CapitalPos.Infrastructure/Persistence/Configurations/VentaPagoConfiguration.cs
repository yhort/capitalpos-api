using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class VentaPagoConfiguration : IEntityTypeConfiguration<VentaPago>
{
    public void Configure(EntityTypeBuilder<VentaPago> builder)
    {
        builder.ToTable("ventas_pagos");

        builder.HasKey(pago => pago.Id);

        builder.Property(pago => pago.Id)
            .ValueGeneratedNever();

        builder.Property(pago => pago.EmpresaId)
            .IsRequired();

        builder.Property(pago => pago.VentaId)
            .IsRequired();

        builder.Property(pago => pago.MetodoPago)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(pago => pago.Monto)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pago => pago.CodigoOperacion)
            .HasMaxLength(100);

        builder.Property(pago => pago.Observacion)
            .HasMaxLength(500);

        builder.Property(pago => pago.FechaCreacion)
            .IsRequired();

        builder.HasIndex(pago => pago.EmpresaId);

        builder.HasIndex(pago => new
        {
            pago.EmpresaId,
            pago.VentaId
        });

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(pago => pago.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
