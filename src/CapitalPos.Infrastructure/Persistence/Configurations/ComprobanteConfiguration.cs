using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class ComprobanteConfiguration : IEntityTypeConfiguration<Comprobante>
{
    public void Configure(EntityTypeBuilder<Comprobante> builder)
    {
        builder.ToTable("comprobantes");

        builder.HasKey(comprobante => comprobante.Id);

        builder.Property(comprobante => comprobante.Id)
            .ValueGeneratedNever();

        builder.Property(comprobante => comprobante.EmpresaId)
            .IsRequired();

        builder.Property(comprobante => comprobante.VentaId)
            .IsRequired();

        builder.Property(comprobante => comprobante.TipoComprobante)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(comprobante => comprobante.Serie)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(comprobante => comprobante.Correlativo)
            .IsRequired();

        builder.Property(comprobante => comprobante.EstadoCpe)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(comprobante => comprobante.Mensaje)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(comprobante => comprobante.Hash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(comprobante => comprobante.NombreXml)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(comprobante => comprobante.NombreZip)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(comprobante => comprobante.NombreCdr)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(comprobante => comprobante.FechaCreacion)
            .IsRequired();

        builder.Property(comprobante => comprobante.ComprobanteAfectadoId);

        builder.Property(comprobante => comprobante.TipoComprobanteAfectado)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(comprobante => comprobante.SerieAfectada)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(comprobante => comprobante.CorrelativoAfectado);

        builder.Property(comprobante => comprobante.CodigoMotivo)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(comprobante => comprobante.DescripcionMotivo)
            .HasMaxLength(500)
            .IsRequired();

        builder.Ignore(comprobante => comprobante.EsNotaCredito);
        builder.Ignore(comprobante => comprobante.EsEmision);
        builder.Ignore(comprobante => comprobante.EstaAceptadoOSimulado);

        builder.HasIndex(comprobante => comprobante.EmpresaId);

        builder.HasIndex(comprobante => comprobante.ComprobanteAfectadoId);

        builder.HasIndex(comprobante => new
            {
                comprobante.EmpresaId,
                comprobante.TipoComprobante,
                comprobante.Serie,
                comprobante.Correlativo
            })
            .IsUnique();

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(comprobante => comprobante.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(comprobante => new
            {
                comprobante.VentaId,
                comprobante.EmpresaId
            })
            .HasPrincipalKey(venta => new
            {
                venta.Id,
                venta.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Comprobante>()
            .WithMany()
            .HasForeignKey(comprobante => comprobante.ComprobanteAfectadoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
