using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class SerieComprobanteConfiguration : IEntityTypeConfiguration<SerieComprobante>
{
    public void Configure(EntityTypeBuilder<SerieComprobante> builder)
    {
        builder.ToTable("series_comprobante");

        builder.HasKey(serie => serie.Id);

        builder.Property(serie => serie.Id)
            .ValueGeneratedNever();

        builder.Property(serie => serie.EmpresaId)
            .IsRequired();

        builder.Property(serie => serie.SedeId)
            .IsRequired();

        builder.Property(serie => serie.TipoComprobante)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(serie => serie.Serie)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(serie => serie.CorrelativoActual)
            .IsRequired();

        builder.Property(serie => serie.Activa)
            .IsRequired();

        builder.Property(serie => serie.FechaCreacion)
            .IsRequired();

        builder.HasIndex(serie => serie.EmpresaId);

        builder.HasIndex(serie => new
        {
            serie.EmpresaId,
            serie.SedeId
        });

        builder.HasIndex(serie => new
            {
                serie.EmpresaId,
                serie.SedeId,
                serie.TipoComprobante,
                serie.Serie
            })
            .IsUnique();

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(serie => serie.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sede>()
            .WithMany()
            .HasForeignKey(serie => new
            {
                serie.SedeId,
                serie.EmpresaId
            })
            .HasPrincipalKey(sede => new
            {
                sede.Id,
                sede.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
