using CapitalPos.Domain;
using CapitalPos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Api.Development;

public sealed class EfDemoSeedStore : IDemoSeedStore
{
    private readonly CapitalPosDbContext _dbContext;

    public EfDemoSeedStore(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Empresa?> ObtenerEmpresaPorRucAsync(string ruc, CancellationToken cancellationToken)
    {
        return _dbContext.Empresas
            .SingleOrDefaultAsync(empresa => empresa.Ruc == ruc, cancellationToken);
    }

    public Task<Usuario?> ObtenerUsuarioPorCorreoAsync(string correo, CancellationToken cancellationToken)
    {
        return _dbContext.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Correo == correo, cancellationToken);
    }

    public Task<UsuarioEmpresa?> ObtenerUsuarioEmpresaAsync(
        Guid usuarioId,
        Guid empresaId,
        CancellationToken cancellationToken)
    {
        return _dbContext.UsuariosEmpresa
            .SingleOrDefaultAsync(usuarioEmpresa =>
                usuarioEmpresa.UsuarioId == usuarioId &&
                usuarioEmpresa.EmpresaId == empresaId,
                cancellationToken);
    }

    public Task<UsuarioCredencial?> ObtenerCredencialAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.UsuariosCredenciales
            .SingleOrDefaultAsync(credencial => credencial.UsuarioId == usuarioId, cancellationToken);
    }

    public Task<Sede?> ObtenerSedeAsync(
        Guid empresaId,
        Guid sedeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Sedes
            .SingleOrDefaultAsync(
                sede => sede.EmpresaId == empresaId && sede.Id == sedeId,
                cancellationToken);
    }

    public Task<PuntoVenta?> ObtenerPuntoVentaAsync(
        Guid empresaId,
        Guid puntoVentaId,
        CancellationToken cancellationToken)
    {
        return _dbContext.PuntosVenta
            .SingleOrDefaultAsync(
                puntoVenta => puntoVenta.EmpresaId == empresaId && puntoVenta.Id == puntoVentaId,
                cancellationToken);
    }

    public Task<Categoria?> ObtenerCategoriaAsync(
        Guid empresaId,
        string nombre,
        CancellationToken cancellationToken)
    {
        var nombreNormalizado = NormalizarTexto(nombre);

        return _dbContext.Categorias
            .SingleOrDefaultAsync(
                categoria => categoria.EmpresaId == empresaId && categoria.Nombre == nombreNormalizado,
                cancellationToken);
    }

    public Task<Marca?> ObtenerMarcaAsync(
        Guid empresaId,
        string nombre,
        CancellationToken cancellationToken)
    {
        var nombreNormalizado = NormalizarTexto(nombre);

        return _dbContext.Marcas
            .SingleOrDefaultAsync(
                marca => marca.EmpresaId == empresaId && marca.Nombre == nombreNormalizado,
                cancellationToken);
    }

    public Task<UnidadMedida?> ObtenerUnidadMedidaAsync(
        string codigo,
        CancellationToken cancellationToken)
    {
        var codigoNormalizado = NormalizarTexto(codigo).ToUpperInvariant();

        return _dbContext.UnidadesMedida
            .SingleOrDefaultAsync(
                unidadMedida => unidadMedida.Codigo == codigoNormalizado,
                cancellationToken);
    }

    public Task<Producto?> ObtenerProductoAsync(
        Guid empresaId,
        Guid productoId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Productos
            .SingleOrDefaultAsync(
                producto => producto.EmpresaId == empresaId && producto.Id == productoId,
                cancellationToken);
    }

    public Task<StockProducto?> ObtenerStockProductoAsync(
        Guid empresaId,
        Guid sedeId,
        Guid productoId,
        CancellationToken cancellationToken)
    {
        return _dbContext.StocksProductos
            .SingleOrDefaultAsync(
                stock =>
                    stock.EmpresaId == empresaId &&
                    stock.SedeId == sedeId &&
                    stock.ProductoId == productoId &&
                    stock.ProductoVarianteId == null,
                cancellationToken);
    }

    public Task<SerieComprobante?> ObtenerSerieComprobanteAsync(
        Guid empresaId,
        Guid sedeId,
        string tipoComprobante,
        string serie,
        CancellationToken cancellationToken)
    {
        var tipoNormalizado = NormalizarTexto(tipoComprobante).ToUpperInvariant();
        var serieNormalizada = NormalizarTexto(serie).ToUpperInvariant();

        return _dbContext.SeriesComprobante
            .SingleOrDefaultAsync(
                serieComprobante =>
                    serieComprobante.EmpresaId == empresaId &&
                    serieComprobante.SedeId == sedeId &&
                    serieComprobante.TipoComprobante == tipoNormalizado &&
                    serieComprobante.Serie == serieNormalizada,
                cancellationToken);
    }

    public async Task AgregarEmpresaAsync(Empresa empresa, CancellationToken cancellationToken)
    {
        await _dbContext.Empresas.AddAsync(empresa, cancellationToken);
    }

    public async Task AgregarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        await _dbContext.Usuarios.AddAsync(usuario, cancellationToken);
    }

    public async Task AgregarUsuarioEmpresaAsync(UsuarioEmpresa usuarioEmpresa, CancellationToken cancellationToken)
    {
        await _dbContext.UsuariosEmpresa.AddAsync(usuarioEmpresa, cancellationToken);
    }

    public async Task AgregarCredencialAsync(UsuarioCredencial credencial, CancellationToken cancellationToken)
    {
        await _dbContext.UsuariosCredenciales.AddAsync(credencial, cancellationToken);
    }

    public async Task AgregarSedeAsync(Sede sede, CancellationToken cancellationToken)
    {
        await _dbContext.Sedes.AddAsync(sede, cancellationToken);
    }

    public async Task AgregarPuntoVentaAsync(PuntoVenta puntoVenta, CancellationToken cancellationToken)
    {
        await _dbContext.PuntosVenta.AddAsync(puntoVenta, cancellationToken);
    }

    public async Task AgregarCategoriaAsync(Categoria categoria, CancellationToken cancellationToken)
    {
        await _dbContext.Categorias.AddAsync(categoria, cancellationToken);
    }

    public async Task AgregarMarcaAsync(Marca marca, CancellationToken cancellationToken)
    {
        await _dbContext.Marcas.AddAsync(marca, cancellationToken);
    }

    public async Task AgregarUnidadMedidaAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken)
    {
        await _dbContext.UnidadesMedida.AddAsync(unidadMedida, cancellationToken);
    }

    public async Task AgregarProductoAsync(Producto producto, CancellationToken cancellationToken)
    {
        await _dbContext.Productos.AddAsync(producto, cancellationToken);
    }

    public async Task AgregarStockProductoAsync(StockProducto stock, CancellationToken cancellationToken)
    {
        await _dbContext.StocksProductos.AddAsync(stock, cancellationToken);
    }

    public async Task AgregarSerieComprobanteAsync(SerieComprobante serie, CancellationToken cancellationToken)
    {
        await _dbContext.SeriesComprobante.AddAsync(serie, cancellationToken);
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizarTexto(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }
}
