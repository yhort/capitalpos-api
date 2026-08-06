using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class PedidoDigitalConfiguration : IEntityTypeConfiguration<PedidoDigital>
{
    public void Configure(EntityTypeBuilder<PedidoDigital> builder)
    {
        builder.ToTable("pedidos_digitales");

        builder.HasKey(pedido => pedido.Id);

        builder.Property(pedido => pedido.Id)
            .ValueGeneratedNever();

        builder.Property(pedido => pedido.EmpresaId)
            .IsRequired();

        builder.Property(pedido => pedido.ClienteId);

        builder.Property(pedido => pedido.SedeId)
            .IsRequired();

        builder.Property(pedido => pedido.PuntoVentaId);

        builder.Property(pedido => pedido.CanalPedido)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(pedido => pedido.Estado)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(pedido => pedido.FechaPedido)
            .IsRequired();

        builder.Property(pedido => pedido.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pedido => pedido.Igv)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pedido => pedido.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pedido => pedido.ReferenciaExterna)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(pedido => pedido.Observacion)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pedido => pedido.FechaCreacion)
            .IsRequired();

        builder.Property(pedido => pedido.FechaActualizacion)
            .IsRequired();

        builder.HasIndex(pedido => pedido.EmpresaId);

        builder.HasIndex(pedido => new
        {
            pedido.EmpresaId,
            pedido.Estado,
            pedido.FechaPedido
        });

        builder.HasIndex(pedido => new
        {
            pedido.EmpresaId,
            pedido.CanalPedido,
            pedido.FechaPedido
        });

        builder.HasAlternateKey(pedido => new
        {
            pedido.Id,
            pedido.EmpresaId
        });

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(pedido => pedido.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(pedido => new
            {
                pedido.ClienteId,
                pedido.EmpresaId
            })
            .HasPrincipalKey(cliente => new
            {
                cliente.Id,
                cliente.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sede>()
            .WithMany()
            .HasForeignKey(pedido => new
            {
                pedido.SedeId,
                pedido.EmpresaId
            })
            .HasPrincipalKey(sede => new
            {
                sede.Id,
                sede.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PuntoVenta>()
            .WithMany()
            .HasForeignKey(pedido => new
            {
                pedido.PuntoVentaId,
                pedido.EmpresaId
            })
            .HasPrincipalKey(puntoVenta => new
            {
                puntoVenta.Id,
                puntoVenta.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pedido => pedido.Detalles)
            .WithOne()
            .HasForeignKey(detalle => new
            {
                detalle.PedidoDigitalId,
                detalle.EmpresaId
            })
            .HasPrincipalKey(pedido => new
            {
                pedido.Id,
                pedido.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(pedido => pedido.Detalles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(pedido => pedido.HistorialEstados)
            .WithOne()
            .HasForeignKey(historial => new
            {
                historial.PedidoDigitalId,
                historial.EmpresaId
            })
            .HasPrincipalKey(pedido => new
            {
                pedido.Id,
                pedido.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(pedido => pedido.HistorialEstados)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
