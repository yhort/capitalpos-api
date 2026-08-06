using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class PedidoDigitalHistorialEstadoConfiguration : IEntityTypeConfiguration<PedidoDigitalHistorialEstado>
{
    public void Configure(EntityTypeBuilder<PedidoDigitalHistorialEstado> builder)
    {
        builder.ToTable("pedidos_digitales_historial_estados");

        builder.HasKey(historial => historial.Id);

        builder.Property(historial => historial.Id)
            .ValueGeneratedNever();

        builder.Property(historial => historial.EmpresaId)
            .IsRequired();

        builder.Property(historial => historial.PedidoDigitalId)
            .IsRequired();

        builder.Property(historial => historial.EstadoAnterior)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(historial => historial.EstadoNuevo)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(historial => historial.UsuarioId);

        builder.Property(historial => historial.Fecha)
            .IsRequired();

        builder.Property(historial => historial.Observacion)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(historial => historial.EmpresaId);

        builder.HasIndex(historial => new
        {
            historial.EmpresaId,
            historial.PedidoDigitalId,
            historial.Fecha
        });

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(historial => historial.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(historial => historial.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
