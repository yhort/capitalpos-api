using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CapitalPos.Tests;

public class EfCoreModelTests
{
    [Fact]
    public void Capital_pos_db_context_expone_entidades_principales()
    {
        using var context = CrearContexto();

        Assert.NotNull(context.Model.FindEntityType(typeof(Empresa)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Usuario)));
        Assert.NotNull(context.Model.FindEntityType(typeof(UsuarioEmpresa)));
    }

    [Fact]
    public void Empresa_tiene_clave_primaria_campos_obligatorios_e_indice_unico()
    {
        var entityType = ObtenerEntidad<Empresa>();

        Assert.Equal("empresas", entityType.GetTableName());
        Assert.Equal(nameof(Empresa.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(Empresa.Ruc), maxLength: 11, nullable: false);
        AssertPropiedad(entityType, nameof(Empresa.RazonSocial), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(Empresa.NombreComercial), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(Empresa.Activa), nullable: false);
        AssertPropiedad(entityType, nameof(Empresa.FechaCreacion), nullable: false);
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Empresa.Ruc)]));
    }

    [Fact]
    public void Usuario_tiene_clave_primaria_campos_obligatorios_e_indice_unico()
    {
        var entityType = ObtenerEntidad<Usuario>();

        Assert.Equal("usuarios", entityType.GetTableName());
        Assert.Equal(nameof(Usuario.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(Usuario.Nombre), maxLength: 100, nullable: false);
        AssertPropiedad(entityType, nameof(Usuario.Apellido), maxLength: 100, nullable: false);
        AssertPropiedad(entityType, nameof(Usuario.Correo), maxLength: 254, nullable: false);
        AssertPropiedad(entityType, nameof(Usuario.Activo), nullable: false);
        AssertPropiedad(entityType, nameof(Usuario.FechaCreacion), nullable: false);
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Usuario.Correo)]));
    }

    [Fact]
    public void Usuario_empresa_tiene_relaciones_indice_unico_y_conversion_de_rol()
    {
        var entityType = ObtenerEntidad<UsuarioEmpresa>();

        Assert.Equal("usuarios_empresas", entityType.GetTableName());
        Assert.Equal(nameof(UsuarioEmpresa.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(UsuarioEmpresa.UsuarioId), nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioEmpresa.EmpresaId), nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioEmpresa.Rol), maxLength: 50, nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioEmpresa.Activo), nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioEmpresa.FechaAsignacion), nullable: false);

        var rolProperty = entityType.FindProperty(nameof(UsuarioEmpresa.Rol));
        Assert.Equal(typeof(string), rolProperty?.GetProviderClrType());

        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(UsuarioEmpresa.UsuarioId),
                nameof(UsuarioEmpresa.EmpresaId)
            ]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Usuario) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(UsuarioEmpresa.UsuarioId)]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Empresa) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(UsuarioEmpresa.EmpresaId)]));
    }

    private static IEntityType ObtenerEntidad<TEntity>()
    {
        using var context = CrearContexto();

        return context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"No se encontro la entidad {typeof(TEntity).Name}.");
    }

    private static CapitalPosDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<CapitalPosDbContext>()
            .UseNpgsql("Host=localhost;Database=capitalpos_model_tests")
            .Options;

        return new CapitalPosDbContext(options);
    }

    private static void AssertPropiedad(
        IEntityType entityType,
        string propertyName,
        int? maxLength = null,
        bool nullable = true)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(nullable, property.IsNullable);
        Assert.Equal(maxLength, property.GetMaxLength());
    }
}
