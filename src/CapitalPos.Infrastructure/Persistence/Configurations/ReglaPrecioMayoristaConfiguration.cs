using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class ReglaPrecioMayoristaConfiguration : IEntityTypeConfiguration<ReglaPrecioMayorista>
{
    public void Configure(EntityTypeBuilder<ReglaPrecioMayorista> builder)
    {
        builder.ToTable("reglas_precios_mayoristas");

        builder.HasKey(regla => regla.Id);

        builder.Property(regla => regla.Id)
            .ValueGeneratedNever();

        builder.Property(regla => regla.EmpresaId)
            .IsRequired();

        builder.Property(regla => regla.ProductoId)
            .IsRequired();

        builder.Property(regla => regla.CantidadMinima)
            .IsRequired();

        builder.Property(regla => regla.PrecioUnitarioMayorista)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(regla => regla.Activa)
            .IsRequired();

        builder.Property(regla => regla.FechaCreacion)
            .IsRequired();

        builder.HasIndex(regla => regla.EmpresaId);

        builder.HasIndex(regla => new
        {
            regla.EmpresaId,
            regla.ProductoId
        });

        builder.HasIndex(regla => new
            {
                regla.EmpresaId,
                regla.ProductoId,
                regla.CantidadMinima
            })
            .IsUnique();

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(regla => regla.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(regla => new
            {
                regla.ProductoId,
                regla.EmpresaId
            })
            .HasPrincipalKey(producto => new
            {
                producto.Id,
                producto.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
