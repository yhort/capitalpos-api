using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class SesionCajaConfiguration : IEntityTypeConfiguration<SesionCaja>
{
    public void Configure(EntityTypeBuilder<SesionCaja> builder)
    {
        builder.ToTable("sesiones_caja");

        builder.HasKey(sesion => sesion.Id);

        builder.Property(sesion => sesion.Id)
            .ValueGeneratedNever();

        builder.Property(sesion => sesion.EmpresaId)
            .IsRequired();

        builder.Property(sesion => sesion.SedeId)
            .IsRequired();

        builder.Property(sesion => sesion.PuntoVentaId)
            .IsRequired();

        builder.Property(sesion => sesion.UsuarioAperturaId);

        builder.Property(sesion => sesion.UsuarioCierreId);

        builder.Property(sesion => sesion.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sesion => sesion.MontoInicial)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(sesion => sesion.MontoDeclaradoCierre)
            .HasPrecision(18, 2);

        builder.Property(sesion => sesion.DiferenciaCierre)
            .HasPrecision(18, 2);

        builder.Property(sesion => sesion.FechaApertura)
            .IsRequired();

        builder.Property(sesion => sesion.FechaCierre);

        builder.Property(sesion => sesion.ObservacionApertura)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(sesion => sesion.ObservacionCierre)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(sesion => sesion.EmpresaId);

        builder.HasIndex(sesion => new
        {
            sesion.EmpresaId,
            sesion.SedeId
        });

        builder.HasIndex(sesion => new
            {
                sesion.EmpresaId,
                sesion.PuntoVentaId,
                sesion.Estado
            })
            .IsUnique()
            .HasFilter("\"Estado\" = 'Abierta'");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(sesion => sesion.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sede>()
            .WithMany()
            .HasForeignKey(sesion => new
            {
                sesion.SedeId,
                sesion.EmpresaId
            })
            .HasPrincipalKey(sede => new
            {
                sede.Id,
                sede.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PuntoVenta>()
            .WithMany()
            .HasForeignKey(sesion => new
            {
                sesion.PuntoVentaId,
                sesion.EmpresaId
            })
            .HasPrincipalKey(puntoVenta => new
            {
                puntoVenta.Id,
                puntoVenta.EmpresaId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
