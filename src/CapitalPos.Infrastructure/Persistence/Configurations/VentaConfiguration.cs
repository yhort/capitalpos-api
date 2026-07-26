using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("ventas");

        builder.HasKey(venta => venta.Id);

        builder.Property(venta => venta.Id)
            .ValueGeneratedNever();

        builder.Property(venta => venta.EmpresaId)
            .IsRequired();

        builder.Property(venta => venta.SedeId)
            .IsRequired();

        builder.Property(venta => venta.ClienteId);

        builder.Property(venta => venta.CanalVenta)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(venta => venta.PuntoVentaId)
            .IsRequired();

        builder.Property(venta => venta.VendedorId);

        builder.Property(venta => venta.Fecha)
            .IsRequired();

        builder.Property(venta => venta.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(venta => venta.Igv)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(venta => venta.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(venta => venta.Estado)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(venta => venta.FechaCreacion)
            .IsRequired();

        builder.Property(venta => venta.FechaAnulacion);

        builder.Property(venta => venta.ObservacionAnulacion)
            .HasMaxLength(500);

        builder.HasIndex(venta => venta.EmpresaId);

        builder.HasIndex(venta => new
        {
            venta.EmpresaId,
            venta.SedeId
        });

        builder.HasIndex(venta => new
        {
            venta.EmpresaId,
            venta.CanalVenta,
            venta.Fecha
        });

        builder.HasAlternateKey(venta => new
        {
            venta.Id,
            venta.EmpresaId
        });

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(venta => venta.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(venta => new
            {
                venta.ClienteId,
                venta.EmpresaId
            })
            .HasPrincipalKey(cliente => new
            {
                cliente.Id,
                cliente.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sede>()
            .WithMany()
            .HasForeignKey(venta => new
            {
                venta.SedeId,
                venta.EmpresaId
            })
            .HasPrincipalKey(sede => new
            {
                sede.Id,
                sede.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PuntoVenta>()
            .WithMany()
            .HasForeignKey(venta => new
            {
                venta.PuntoVentaId,
                venta.EmpresaId
            })
            .HasPrincipalKey(puntoVenta => new
            {
                puntoVenta.Id,
                puntoVenta.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(venta => venta.Detalles)
            .WithOne()
            .HasForeignKey(detalle => new
            {
                detalle.VentaId,
                detalle.EmpresaId
            })
            .HasPrincipalKey(venta => new
            {
                venta.Id,
                venta.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(venta => venta.Detalles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(venta => venta.Pagos)
            .WithOne()
            .HasForeignKey(pago => new
            {
                pago.VentaId,
                pago.EmpresaId
            })
            .HasPrincipalKey(venta => new
            {
                venta.Id,
                venta.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(venta => venta.Pagos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
