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
        Assert.NotNull(context.Model.FindEntityType(typeof(UsuarioCredencial)));
        Assert.NotNull(context.Model.FindEntityType(typeof(UsuarioEmpresa)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Producto)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ProductoVariante)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Cliente)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Venta)));
        Assert.NotNull(context.Model.FindEntityType(typeof(VentaDetalle)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Comprobante)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ConfiguracionFiscalEmpresa)));
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
    public void Usuario_credencial_tiene_relacion_uno_a_uno_campos_obligatorios_e_indices()
    {
        var entityType = ObtenerEntidad<UsuarioCredencial>();

        Assert.Equal("usuarios_credenciales", entityType.GetTableName());
        Assert.Equal(nameof(UsuarioCredencial.UsuarioId), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(UsuarioCredencial.UsuarioId), nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioCredencial.PasswordHash), maxLength: 500, nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioCredencial.Algoritmo), maxLength: 100, nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioCredencial.FechaCambio), nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioCredencial.Activo), nullable: false);
        AssertPropiedad(entityType, nameof(UsuarioCredencial.Bloqueado), nullable: false);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(UsuarioCredencial.Activo),
                nameof(UsuarioCredencial.Bloqueado)
            ]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.IsUnique &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Usuario) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Cascade &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(UsuarioCredencial.UsuarioId)]));
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

    [Fact]
    public void Producto_tiene_empresa_obligatoria_indices_y_relacion_restrictiva()
    {
        var entityType = ObtenerEntidad<Producto>();

        Assert.Equal("productos", entityType.GetTableName());
        Assert.Equal(nameof(Producto.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(Producto.EmpresaId), nullable: false);
        AssertPropiedad(entityType, nameof(Producto.Nombre), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(Producto.CodigoSku), maxLength: 80, nullable: false);
        AssertPropiedad(entityType, nameof(Producto.CodigoBarras), maxLength: 80, nullable: false);
        AssertPropiedad(entityType, nameof(Producto.PrecioVenta), nullable: false);
        AssertPropiedad(entityType, nameof(Producto.Costo));
        AssertPropiedad(entityType, nameof(Producto.Activo), nullable: false);
        AssertPropiedad(entityType, nameof(Producto.FechaCreacion), nullable: false);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Producto.EmpresaId)]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "\"CodigoSku\" <> ''" &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Producto.EmpresaId),
                nameof(Producto.CodigoSku)
            ]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "\"CodigoBarras\" <> ''" &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Producto.EmpresaId),
                nameof(Producto.CodigoBarras)
            ]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Empresa) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(Producto.EmpresaId)]));
    }

    [Fact]
    public void Producto_variante_tiene_empresa_producto_indices_y_relaciones_restrictivas()
    {
        var entityType = ObtenerEntidad<ProductoVariante>();

        Assert.Equal("productos_variantes", entityType.GetTableName());
        Assert.Equal(nameof(ProductoVariante.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(ProductoVariante.EmpresaId), nullable: false);
        AssertPropiedad(entityType, nameof(ProductoVariante.ProductoId), nullable: false);
        AssertPropiedad(entityType, nameof(ProductoVariante.Talla), maxLength: 50, nullable: false);
        AssertPropiedad(entityType, nameof(ProductoVariante.Color), maxLength: 80, nullable: false);
        AssertPropiedad(entityType, nameof(ProductoVariante.CodigoSku), maxLength: 80, nullable: false);
        AssertPropiedad(entityType, nameof(ProductoVariante.CodigoBarras), maxLength: 80, nullable: false);
        AssertPropiedad(entityType, nameof(ProductoVariante.StockActual), nullable: false);
        AssertPropiedad(entityType, nameof(ProductoVariante.Activo), nullable: false);
        AssertPropiedad(entityType, nameof(ProductoVariante.FechaCreacion), nullable: false);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(ProductoVariante.EmpresaId)]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(ProductoVariante.EmpresaId),
                nameof(ProductoVariante.ProductoId)
            ]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "\"CodigoSku\" <> ''" &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(ProductoVariante.EmpresaId),
                nameof(ProductoVariante.CodigoSku)
            ]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "\"CodigoBarras\" <> ''" &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(ProductoVariante.EmpresaId),
                nameof(ProductoVariante.CodigoBarras)
            ]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Empresa) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(ProductoVariante.EmpresaId)]));
        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Producto) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(ProductoVariante.ProductoId),
                nameof(ProductoVariante.EmpresaId)
            ]) &&
            foreignKey.PrincipalKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Producto.Id),
                nameof(Producto.EmpresaId)
            ]));
    }

    [Fact]
    public void Cliente_tiene_empresa_obligatoria_indice_documento_y_relacion_restrictiva()
    {
        var entityType = ObtenerEntidad<Cliente>();

        Assert.Equal("clientes", entityType.GetTableName());
        Assert.Equal(nameof(Cliente.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(Cliente.EmpresaId), nullable: false);
        AssertPropiedad(entityType, nameof(Cliente.TipoDocumento), maxLength: 20, nullable: false);
        AssertPropiedad(entityType, nameof(Cliente.NumeroDocumento), maxLength: 20, nullable: false);
        AssertPropiedad(entityType, nameof(Cliente.NombreRazonSocial), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(Cliente.Direccion), maxLength: 250, nullable: false);
        AssertPropiedad(entityType, nameof(Cliente.Activo), nullable: false);
        AssertPropiedad(entityType, nameof(Cliente.FechaCreacion), nullable: false);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Cliente.EmpresaId)]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "\"NumeroDocumento\" <> ''" &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Cliente.EmpresaId),
                nameof(Cliente.TipoDocumento),
                nameof(Cliente.NumeroDocumento)
            ]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Empresa) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(Cliente.EmpresaId)]));
    }

    [Fact]
    public void Venta_tiene_empresa_cliente_opcional_totales_estado_y_detalles()
    {
        var entityType = ObtenerEntidad<Venta>();

        Assert.Equal("ventas", entityType.GetTableName());
        Assert.Equal(nameof(Venta.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(Venta.EmpresaId), nullable: false);
        AssertPropiedad(entityType, nameof(Venta.ClienteId));
        AssertPropiedad(entityType, nameof(Venta.Fecha), nullable: false);
        AssertPropiedad(entityType, nameof(Venta.Subtotal), nullable: false);
        AssertPropiedad(entityType, nameof(Venta.Igv), nullable: false);
        AssertPropiedad(entityType, nameof(Venta.Total), nullable: false);
        AssertPropiedad(entityType, nameof(Venta.Estado), maxLength: 30, nullable: false);
        AssertPropiedad(entityType, nameof(Venta.FechaCreacion), nullable: false);

        var estadoProperty = entityType.FindProperty(nameof(Venta.Estado));
        Assert.Equal(typeof(string), estadoProperty?.GetProviderClrType());

        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Venta.EmpresaId)]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Empresa) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(Venta.EmpresaId)]));
        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Cliente) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Venta.ClienteId),
                nameof(Venta.EmpresaId)
            ]) &&
            foreignKey.PrincipalKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Cliente.Id),
                nameof(Cliente.EmpresaId)
            ]));
    }

    [Fact]
    public void Venta_detalle_tiene_empresa_producto_variante_opcional_y_relaciones_compuestas()
    {
        var entityType = ObtenerEntidad<VentaDetalle>();

        Assert.Equal("ventas_detalles", entityType.GetTableName());
        Assert.Equal(nameof(VentaDetalle.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(VentaDetalle.EmpresaId), nullable: false);
        AssertPropiedad(entityType, nameof(VentaDetalle.VentaId), nullable: false);
        AssertPropiedad(entityType, nameof(VentaDetalle.ProductoId), nullable: false);
        AssertPropiedad(entityType, nameof(VentaDetalle.ProductoVarianteId));
        AssertPropiedad(entityType, nameof(VentaDetalle.Cantidad), nullable: false);
        AssertPropiedad(entityType, nameof(VentaDetalle.PrecioUnitario), nullable: false);
        AssertPropiedad(entityType, nameof(VentaDetalle.Igv), nullable: false);
        AssertPropiedad(entityType, nameof(VentaDetalle.Total), nullable: false);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(VentaDetalle.EmpresaId)]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(VentaDetalle.EmpresaId),
                nameof(VentaDetalle.VentaId)
            ]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Venta) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(VentaDetalle.VentaId),
                nameof(VentaDetalle.EmpresaId)
            ]));
        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Producto) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(VentaDetalle.ProductoId),
                nameof(VentaDetalle.EmpresaId)
            ]));
        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductoVariante) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(VentaDetalle.ProductoVarianteId),
                nameof(VentaDetalle.EmpresaId)
            ]));
    }

    [Fact]
    public void Comprobante_tiene_empresa_venta_estado_cpe_e_indice_unico()
    {
        var entityType = ObtenerEntidad<Comprobante>();

        Assert.Equal("comprobantes", entityType.GetTableName());
        Assert.Equal(nameof(Comprobante.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(Comprobante.EmpresaId), nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.VentaId), nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.TipoComprobante), maxLength: 2, nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.Serie), maxLength: 4, nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.Correlativo), nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.EstadoCpe), maxLength: 50, nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.Mensaje), maxLength: 500, nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.Hash), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.NombreXml), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.NombreZip), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.NombreCdr), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(Comprobante.FechaCreacion), nullable: false);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Comprobante.EmpresaId)]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Comprobante.EmpresaId),
                nameof(Comprobante.TipoComprobante),
                nameof(Comprobante.Serie),
                nameof(Comprobante.Correlativo)
            ]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Venta) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Comprobante.VentaId),
                nameof(Comprobante.EmpresaId)
            ]));
    }

    [Fact]
    public void Configuracion_fiscal_empresa_tiene_relacion_uno_a_uno_con_empresa()
    {
        var entityType = ObtenerEntidad<ConfiguracionFiscalEmpresa>();

        Assert.Equal("configuraciones_fiscales_empresas", entityType.GetTableName());
        Assert.Equal(nameof(ConfiguracionFiscalEmpresa.EmpresaId), entityType.FindPrimaryKey()?.Properties.Single().Name);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.EmpresaId), nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.Ruc), maxLength: 11, nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.RazonSocial), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.NombreComercial), maxLength: 200, nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.Ubigeo), maxLength: 6, nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.Direccion), maxLength: 250, nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.Departamento), maxLength: 100, nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.Provincia), maxLength: 100, nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.Distrito), maxLength: 100, nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.Activa), nullable: false);
        AssertPropiedad(entityType, nameof(ConfiguracionFiscalEmpresa.FechaCreacion), nullable: false);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(ConfiguracionFiscalEmpresa.EmpresaId)]));

        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.IsUnique &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Empresa) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(ConfiguracionFiscalEmpresa.EmpresaId)]));
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
