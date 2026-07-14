using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class StockProductoConfiguration : IEntityTypeConfiguration<StockProducto>
{
    public void Configure(EntityTypeBuilder<StockProducto> builder)
    {
        builder.ToTable("stocks_productos");

        builder.HasKey(stock => stock.Id);

        builder.Property(stock => stock.Id)
            .ValueGeneratedNever();

        builder.Property(stock => stock.EmpresaId)
            .IsRequired();

        builder.Property(stock => stock.ProductoId)
            .IsRequired();

        builder.Property(stock => stock.ProductoVarianteId);

        builder.Property(stock => stock.CantidadDisponible)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(stock => stock.CantidadReservada)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(stock => stock.FechaCreacion)
            .IsRequired();

        builder.Property(stock => stock.FechaActualizacion)
            .IsRequired();

        builder.Ignore(stock => stock.CantidadLibre);

        builder.HasIndex(stock => stock.EmpresaId);

        builder.HasIndex(stock => new
        {
            stock.EmpresaId,
            stock.ProductoId
        });

        builder.HasIndex(stock => new
            {
                stock.EmpresaId,
                stock.ProductoId
            })
            .IsUnique()
            .HasFilter("\"ProductoVarianteId\" IS NULL");

        builder.HasIndex(stock => new
            {
                stock.EmpresaId,
                stock.ProductoId,
                stock.ProductoVarianteId
            })
            .IsUnique()
            .HasFilter("\"ProductoVarianteId\" IS NOT NULL");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(stock => stock.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(stock => new
            {
                stock.ProductoId,
                stock.EmpresaId
            })
            .HasPrincipalKey(producto => new
            {
                producto.Id,
                producto.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductoVariante>()
            .WithMany()
            .HasForeignKey(stock => new
            {
                stock.ProductoVarianteId,
                stock.EmpresaId
            })
            .HasPrincipalKey(variante => new
            {
                variante.Id,
                variante.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
