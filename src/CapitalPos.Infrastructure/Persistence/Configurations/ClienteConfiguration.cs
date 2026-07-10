using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitalPos.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");

        builder.HasKey(cliente => cliente.Id);

        builder.Property(cliente => cliente.Id)
            .ValueGeneratedNever();

        builder.Property(cliente => cliente.EmpresaId)
            .IsRequired();

        builder.Property(cliente => cliente.TipoDocumento)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(cliente => cliente.NumeroDocumento)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(cliente => cliente.NombreRazonSocial)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(cliente => cliente.Direccion)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(cliente => cliente.Activo)
            .IsRequired();

        builder.Property(cliente => cliente.FechaCreacion)
            .IsRequired();

        builder.HasIndex(cliente => cliente.EmpresaId);

        builder.HasAlternateKey(cliente => new
        {
            cliente.Id,
            cliente.EmpresaId
        });

        builder.HasIndex(cliente => new
            {
                cliente.EmpresaId,
                cliente.TipoDocumento,
                cliente.NumeroDocumento
            })
            .IsUnique()
            .HasFilter("\"NumeroDocumento\" <> ''");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(cliente => cliente.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
