using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence;

public sealed class CapitalPosDbContext : DbContext
{
    public CapitalPosDbContext(DbContextOptions<CapitalPosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<UsuarioCredencial> UsuariosCredenciales => Set<UsuarioCredencial>();

    public DbSet<UsuarioEmpresa> UsuariosEmpresa => Set<UsuarioEmpresa>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Marca> Marcas => Set<Marca>();

    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<ProductoPresentacion> ProductosPresentaciones => Set<ProductoPresentacion>();

    public DbSet<ProductoVariante> ProductosVariantes => Set<ProductoVariante>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Venta> Ventas => Set<Venta>();

    public DbSet<VentaDetalle> VentasDetalles => Set<VentaDetalle>();

    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();

    public DbSet<ConfiguracionFiscalEmpresa> ConfiguracionesFiscalesEmpresas => Set<ConfiguracionFiscalEmpresa>();

    public DbSet<StockProducto> StocksProductos => Set<StockProducto>();

    public DbSet<Sede> Sedes => Set<Sede>();

    public DbSet<PuntoVenta> PuntosVenta => Set<PuntoVenta>();

    public DbSet<SerieComprobante> SeriesComprobante => Set<SerieComprobante>();

    public DbSet<SesionCaja> SesionesCaja => Set<SesionCaja>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CapitalPosDbContext).Assembly);
    }
}
