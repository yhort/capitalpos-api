using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class VentaDetalleConfiguration : IEntityTypeConfiguration<VentaDetalle>
{
    public void Configure(EntityTypeBuilder<VentaDetalle> builder)
    {
        builder.ToTable("ventas_detalles");

        builder.HasKey(detalle => detalle.Id);

        builder.Property(detalle => detalle.Id)
            .ValueGeneratedNever();

        builder.Property(detalle => detalle.EmpresaId)
            .IsRequired();

        builder.Property(detalle => detalle.VentaId)
            .IsRequired();

        builder.Property(detalle => detalle.ProductoId)
            .IsRequired();

        builder.Property(detalle => detalle.ProductoVarianteId);

        builder.Property(detalle => detalle.ProductoPresentacionId);

        builder.Property(detalle => detalle.Cantidad)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(detalle => detalle.PrecioUnitario)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(detalle => detalle.Igv)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(detalle => detalle.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(detalle => detalle.EmpresaId);

        builder.HasIndex(detalle => new
        {
            detalle.EmpresaId,
            detalle.VentaId
        });

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(detalle => detalle.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(detalle => new
            {
                detalle.ProductoId,
                detalle.EmpresaId
            })
            .HasPrincipalKey(producto => new
            {
                producto.Id,
                producto.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductoVariante>()
            .WithMany()
            .HasForeignKey(detalle => new
            {
                detalle.ProductoVarianteId,
                detalle.EmpresaId
            })
            .HasPrincipalKey(variante => new
            {
                variante.Id,
                variante.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductoPresentacion>()
            .WithMany()
            .HasForeignKey(detalle => new
            {
                detalle.ProductoPresentacionId,
                detalle.EmpresaId
            })
            .HasPrincipalKey(presentacion => new
            {
                presentacion.Id,
                presentacion.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
