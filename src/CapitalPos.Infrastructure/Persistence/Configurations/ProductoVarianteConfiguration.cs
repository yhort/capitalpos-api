using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class ProductoVarianteConfiguration : IEntityTypeConfiguration<ProductoVariante>
{
    public void Configure(EntityTypeBuilder<ProductoVariante> builder)
    {
        builder.ToTable("productos_variantes");

        builder.HasKey(variante => variante.Id);

        builder.Property(variante => variante.Id)
            .ValueGeneratedNever();

        builder.Property(variante => variante.EmpresaId)
            .IsRequired();

        builder.Property(variante => variante.ProductoId)
            .IsRequired();

        builder.Property(variante => variante.Talla)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(variante => variante.Color)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(variante => variante.CodigoSku)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(variante => variante.CodigoBarras)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(variante => variante.StockActual)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(variante => variante.Activo)
            .IsRequired();

        builder.Property(variante => variante.FechaCreacion)
            .IsRequired();

        builder.HasIndex(variante => variante.EmpresaId);

        builder.HasIndex(variante => new
            {
                variante.EmpresaId,
                variante.ProductoId
            });

        builder.HasIndex(variante => new
            {
                variante.EmpresaId,
                variante.CodigoSku
            })
            .IsUnique()
            .HasFilter("\"CodigoSku\" <> ''");

        builder.HasIndex(variante => new
            {
                variante.EmpresaId,
                variante.CodigoBarras
            })
            .IsUnique()
            .HasFilter("\"CodigoBarras\" <> ''");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(variante => variante.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(variante => new
            {
                variante.ProductoId,
                variante.EmpresaId
            })
            .HasPrincipalKey(producto => new
            {
                producto.Id,
                producto.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
