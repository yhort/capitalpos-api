using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class ProductoPresentacionConfiguration : IEntityTypeConfiguration<ProductoPresentacion>
{
    public void Configure(EntityTypeBuilder<ProductoPresentacion> builder)
    {
        builder.ToTable("productos_presentaciones");

        builder.HasKey(presentacion => presentacion.Id);

        builder.Property(presentacion => presentacion.Id)
            .ValueGeneratedNever();

        builder.Property(presentacion => presentacion.EmpresaId)
            .IsRequired();

        builder.Property(presentacion => presentacion.ProductoId)
            .IsRequired();

        builder.Property(presentacion => presentacion.UnidadMedidaId)
            .IsRequired();

        builder.Property(presentacion => presentacion.FactorConversion)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(presentacion => presentacion.EsUnidadBase)
            .IsRequired();

        builder.Property(presentacion => presentacion.PrecioVenta)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(presentacion => presentacion.CodigoBarras)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(presentacion => presentacion.Activa)
            .IsRequired();

        builder.Property(presentacion => presentacion.FechaCreacion)
            .IsRequired();

        builder.HasIndex(presentacion => presentacion.EmpresaId);

        builder.HasIndex(presentacion => new
        {
            presentacion.EmpresaId,
            presentacion.ProductoId
        });

        builder.HasIndex(presentacion => new
            {
                presentacion.EmpresaId,
                presentacion.ProductoId,
                presentacion.UnidadMedidaId
            })
            .IsUnique();

        builder.HasIndex(presentacion => new
            {
                presentacion.EmpresaId,
                presentacion.ProductoId,
                presentacion.EsUnidadBase
            })
            .IsUnique()
            .HasFilter("\"EsUnidadBase\" = TRUE");

        builder.HasIndex(presentacion => new
            {
                presentacion.EmpresaId,
                presentacion.CodigoBarras
            })
            .IsUnique()
            .HasFilter("\"CodigoBarras\" <> ''");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(presentacion => presentacion.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(presentacion => new
            {
                presentacion.ProductoId,
                presentacion.EmpresaId
            })
            .HasPrincipalKey(producto => new
            {
                producto.Id,
                producto.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnidadMedida>()
            .WithMany()
            .HasForeignKey(presentacion => presentacion.UnidadMedidaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
