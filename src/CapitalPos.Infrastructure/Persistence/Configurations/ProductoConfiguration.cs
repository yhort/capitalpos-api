using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("productos");

        builder.HasKey(producto => producto.Id);

        builder.Property(producto => producto.Id)
            .ValueGeneratedNever();

        builder.Property(producto => producto.EmpresaId)
            .IsRequired();

        builder.Property(producto => producto.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(producto => producto.CodigoSku)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(producto => producto.CodigoBarras)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(producto => producto.PrecioVenta)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(producto => producto.Costo)
            .HasPrecision(18, 2);

        builder.Property(producto => producto.Activo)
            .IsRequired();

        builder.Property(producto => producto.FechaCreacion)
            .IsRequired();

        builder.HasIndex(producto => producto.EmpresaId);

        builder.HasAlternateKey(producto => new
        {
            producto.Id,
            producto.EmpresaId
        });

        builder.HasIndex(producto => new
            {
                producto.EmpresaId,
                producto.CodigoSku
            })
            .IsUnique()
            .HasFilter("\"CodigoSku\" <> ''");

        builder.HasIndex(producto => new
            {
                producto.EmpresaId,
                producto.CodigoBarras
            })
            .IsUnique()
            .HasFilter("\"CodigoBarras\" <> ''");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(producto => producto.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
