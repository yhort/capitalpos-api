using CapitalPos.Application.Catalogo;
using CapitalPos.Application.Caja;
using CapitalPos.Application.Compras;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Empresas;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Pedidos;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Sedes;
using CapitalPos.Application.Seguridad;
using CapitalPos.Application.Series;
using CapitalPos.Application.Usuarios;
using CapitalPos.Application.Ventas;
using CapitalPos.Infrastructure.Persistence.Repositories;

namespace CapitalPos.Tests;

public class EfRepositoryStructureTests
{
    [Fact]
    public void Ef_empresa_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IEmpresaRepository).IsAssignableFrom(typeof(EfEmpresaRepository)));
    }

    [Fact]
    public void Ef_usuario_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IUsuarioRepository).IsAssignableFrom(typeof(EfUsuarioRepository)));
    }

    [Fact]
    public void Ef_usuario_empresa_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IUsuarioEmpresaRepository).IsAssignableFrom(typeof(EfUsuarioEmpresaRepository)));
    }

    [Fact]
    public void Ef_usuario_credencial_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IUsuarioCredencialRepository).IsAssignableFrom(typeof(EfUsuarioCredencialRepository)));
    }

    [Fact]
    public void Ef_configuracion_fiscal_empresa_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IConfiguracionFiscalEmpresaRepository).IsAssignableFrom(typeof(EfConfiguracionFiscalEmpresaRepository)));
    }

    [Fact]
    public void Ef_categoria_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(ICategoriaRepository).IsAssignableFrom(typeof(EfCategoriaRepository)));
    }

    [Fact]
    public void Ef_marca_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IMarcaRepository).IsAssignableFrom(typeof(EfMarcaRepository)));
    }

    [Fact]
    public void Ef_unidad_medida_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IUnidadMedidaRepository).IsAssignableFrom(typeof(EfUnidadMedidaRepository)));
    }

    [Fact]
    public void Ef_producto_presentacion_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IProductoPresentacionRepository).IsAssignableFrom(typeof(EfProductoPresentacionRepository)));
    }

    [Fact]
    public void Ef_regla_precio_mayorista_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IReglaPrecioMayoristaRepository).IsAssignableFrom(typeof(EfReglaPrecioMayoristaRepository)));
    }

    [Fact]
    public void Ef_stock_producto_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IStockProductoRepository).IsAssignableFrom(typeof(EfStockProductoRepository)));
    }

    [Fact]
    public void Ef_sede_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(ISedeRepository).IsAssignableFrom(typeof(EfSedeRepository)));
    }

    [Fact]
    public void Ef_punto_venta_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IPuntoVentaRepository).IsAssignableFrom(typeof(EfPuntoVentaRepository)));
    }

    [Fact]
    public void Ef_serie_comprobante_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(ISerieComprobanteRepository).IsAssignableFrom(typeof(EfSerieComprobanteRepository)));
    }

    [Fact]
    public void Ef_sesion_caja_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(ISesionCajaRepository).IsAssignableFrom(typeof(EfSesionCajaRepository)));
    }

    [Fact]
    public void Ef_pedido_digital_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(IPedidoDigitalRepository).IsAssignableFrom(typeof(EfPedidoDigitalRepository)));
    }

    [Fact]
    public void Ef_compra_repository_implementa_puerto_de_aplicacion()
    {
        Assert.True(typeof(ICompraRepository).IsAssignableFrom(typeof(EfCompraRepository)));
        var source = File.ReadAllText(ResolverRutaRepo(
            "src/CapitalPos.Infrastructure/Persistence/Repositories/EfCompraRepository.cs"));

        Assert.Contains("Include(compra => compra.Detalles)", source);
        Assert.Contains("compra.EmpresaId == empresaId", source);
    }

    [Fact]
    public void Ef_venta_repository_incluye_pagos_y_filtra_por_empresa()
    {
        Assert.True(typeof(IVentaRepository).IsAssignableFrom(typeof(EfVentaRepository)));
        var source = File.ReadAllText(ResolverRutaRepo(
            "src/CapitalPos.Infrastructure/Persistence/Repositories/EfVentaRepository.cs"));

        Assert.Contains("Include(venta => venta.Pagos)", source);
        Assert.Contains("venta.EmpresaId == empresaId", source);
    }

    [Fact]
    public void Ef_pedido_digital_repository_incluye_detalles_historial_y_filtra_por_empresa()
    {
        var source = File.ReadAllText(ResolverRutaRepo(
            "src/CapitalPos.Infrastructure/Persistence/Repositories/EfPedidoDigitalRepository.cs"));

        Assert.Contains("AsNoTracking()", source);
        Assert.Contains(".Include(pedido => pedido.Detalles)", source);
        Assert.Contains(".Include(pedido => pedido.HistorialEstados)", source);
        Assert.Contains("pedido.EmpresaId == empresaId", source);
    }

    [Theory]
    [InlineData("src/CapitalPos.Infrastructure/Persistence/Repositories/EfCategoriaRepository.cs")]
    [InlineData("src/CapitalPos.Infrastructure/Persistence/Repositories/EfMarcaRepository.cs")]
    [InlineData("src/CapitalPos.Infrastructure/Persistence/Repositories/EfSedeRepository.cs")]
    [InlineData("src/CapitalPos.Infrastructure/Persistence/Repositories/EfPuntoVentaRepository.cs")]
    [InlineData("src/CapitalPos.Infrastructure/Persistence/Repositories/EfSerieComprobanteRepository.cs")]
    [InlineData("src/CapitalPos.Infrastructure/Persistence/Repositories/EfSesionCajaRepository.cs")]
    [InlineData("src/CapitalPos.Infrastructure/Persistence/Repositories/EfProductoPresentacionRepository.cs")]
    [InlineData("src/CapitalPos.Infrastructure/Persistence/Repositories/EfReglaPrecioMayoristaRepository.cs")]
    public void Repositorios_sede_punto_venta_filtran_por_empresa(string relativePath)
    {
        var source = File.ReadAllText(ResolverRutaRepo(relativePath));

        Assert.Contains("AsNoTracking()", source);
        Assert.Contains("EmpresaId", source);
        Assert.Contains("== empresaId", source);
    }

    [Fact]
    public void Ef_usuario_empresa_repository_filtra_por_usuario_y_empresa_en_metodos_de_pertenencia()
    {
        var source = File.ReadAllText(ResolverRutaRepo(
            "src/CapitalPos.Infrastructure/Persistence/Repositories/EfUsuarioEmpresaRepository.cs"));

        Assert.Contains("ObtenerPorUsuarioYEmpresaAsync", source);
        Assert.Contains("usuarioEmpresa.UsuarioId == usuarioId", source);
        Assert.Contains("usuarioEmpresa.EmpresaId == empresaId", source);
        Assert.Contains("ExisteAsignacionAsync", source);
    }

    private static string ResolverRutaRepo(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "CapitalPos.Api.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(directory.FullName, relativePath);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se pudo resolver la raiz del repositorio.");
    }
}
