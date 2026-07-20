using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categorias");

        builder.HasKey(categoria => categoria.Id);

        builder.Property(categoria => categoria.Id)
            .ValueGeneratedNever();

        builder.Property(categoria => categoria.EmpresaId)
            .IsRequired();

        builder.Property(categoria => categoria.CategoriaPadreId);

        builder.Property(categoria => categoria.Nombre)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(categoria => categoria.Activa)
            .IsRequired();

        builder.Property(categoria => categoria.FechaCreacion)
            .IsRequired();

        builder.HasIndex(categoria => categoria.EmpresaId);

        builder.HasAlternateKey(categoria => new
        {
            categoria.Id,
            categoria.EmpresaId
        });

        builder.HasIndex(categoria => new
            {
                categoria.EmpresaId,
                categoria.Nombre
            })
            .IsUnique();

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(categoria => categoria.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(categoria => new
            {
                categoria.CategoriaPadreId,
                categoria.EmpresaId
            })
            .HasPrincipalKey(categoria => new
            {
                categoria.Id,
                categoria.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
