using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class CompraDetalleConfiguration : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> builder)
    {
        builder.ToTable("compras_detalles");

        builder.HasKey(detalle => detalle.Id);

        builder.Property(detalle => detalle.Id)
            .ValueGeneratedNever();

        builder.Property(detalle => detalle.EmpresaId)
            .IsRequired();

        builder.Property(detalle => detalle.CompraId)
            .IsRequired();

        builder.Property(detalle => detalle.ProductoId)
            .IsRequired();

        builder.Property(detalle => detalle.ProductoVarianteId);

        builder.Property(detalle => detalle.Cantidad)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(detalle => detalle.CostoUnitario)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(detalle => detalle.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(detalle => detalle.EmpresaId);

        builder.HasIndex(detalle => new
        {
            detalle.EmpresaId,
            detalle.CompraId
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
    }
}
