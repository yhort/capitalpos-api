using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class PedidoDigitalDetalleConfiguration : IEntityTypeConfiguration<PedidoDigitalDetalle>
{
    public void Configure(EntityTypeBuilder<PedidoDigitalDetalle> builder)
    {
        builder.ToTable("pedidos_digitales_detalles");

        builder.HasKey(detalle => detalle.Id);

        builder.Property(detalle => detalle.Id)
            .ValueGeneratedNever();

        builder.Property(detalle => detalle.EmpresaId)
            .IsRequired();

        builder.Property(detalle => detalle.PedidoDigitalId)
            .IsRequired();

        builder.Property(detalle => detalle.ProductoId)
            .IsRequired();

        builder.Property(detalle => detalle.ProductoVarianteId);

        builder.Property(detalle => detalle.ProductoPresentacionId);

        builder.Property(detalle => detalle.Descripcion)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(detalle => detalle.Cantidad)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(detalle => detalle.PrecioUnitario)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(detalle => detalle.FactorConversionAplicado)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(detalle => detalle.CantidadBase)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(detalle => detalle.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(detalle => detalle.FechaCreacion)
            .IsRequired();

        builder.HasIndex(detalle => detalle.EmpresaId);

        builder.HasIndex(detalle => new
        {
            detalle.EmpresaId,
            detalle.PedidoDigitalId
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
