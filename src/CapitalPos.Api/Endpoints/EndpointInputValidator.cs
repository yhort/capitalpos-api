using CapitalPos.Application.Empresas;
using CapitalPos.Application.Clientes;
using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Application.Inventario;
using CapitalPos.Application.Productos;
using CapitalPos.Application.Usuarios;
using CapitalPos.Application.Ventas;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class EndpointInputValidator
{
    public static bool TryValidate(CrearEmpresaRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.Ruc))
        {
            error = "El RUC es obligatorio.";
            return false;
        }

        if (request.Ruc.Length != 11 || request.Ruc.Any(static character => !char.IsDigit(character)))
        {
            error = "El RUC debe tener 11 digitos.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RazonSocial))
        {
            error = "La razon social es obligatoria.";
            return false;
        }

        if (request.RazonSocial.Length > 200)
        {
            error = "La razon social no debe exceder 200 caracteres.";
            return false;
        }

        if (request.NombreComercial is { Length: > 200 })
        {
            error = "El nombre comercial no debe exceder 200 caracteres.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(CrearUsuarioRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            error = "El nombre es obligatorio.";
            return false;
        }

        if (request.Nombre.Length > 100)
        {
            error = "El nombre no debe exceder 100 caracteres.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Apellido))
        {
            error = "El apellido es obligatorio.";
            return false;
        }

        if (request.Apellido.Length > 100)
        {
            error = "El apellido no debe exceder 100 caracteres.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Correo))
        {
            error = "El correo es obligatorio.";
            return false;
        }

        if (request.Correo.Length > 254 || !request.Correo.Contains('@', StringComparison.Ordinal))
        {
            error = "El correo debe tener un formato valido.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(AsignarUsuarioEmpresaRequest request, out string error)
    {
        if (request.UsuarioId == Guid.Empty)
        {
            error = "El identificador del usuario es obligatorio.";
            return false;
        }

        if (request.EmpresaId == Guid.Empty)
        {
            error = "El identificador de la empresa es obligatorio.";
            return false;
        }

        return TryValidateRol(request.Rol, out error);
    }

    public static bool TryValidate(CambiarRolUsuarioEmpresaRequest request, out string error)
    {
        return TryValidateRol(request.Rol, out error);
    }

    public static bool TryValidate(CrearProductoRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            error = "El nombre del producto es obligatorio.";
            return false;
        }

        if (request.Nombre.Length > 200)
        {
            error = "El nombre del producto no debe exceder 200 caracteres.";
            return false;
        }

        if (request.CodigoSku is { Length: > 80 })
        {
            error = "El SKU del producto no debe exceder 80 caracteres.";
            return false;
        }

        if (request.CodigoBarras is { Length: > 80 })
        {
            error = "El codigo de barras del producto no debe exceder 80 caracteres.";
            return false;
        }

        if (request.PrecioVenta <= 0)
        {
            error = "El precio de venta debe ser mayor que cero.";
            return false;
        }

        if (request.Costo < 0)
        {
            error = "El costo no puede ser negativo.";
            return false;
        }

        if (request.CategoriaId == Guid.Empty)
        {
            error = "El identificador de la categoria no puede ser vacio.";
            return false;
        }

        if (request.MarcaId == Guid.Empty)
        {
            error = "El identificador de la marca no puede ser vacio.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(CrearProductoVarianteRequest request, out string error)
    {
        if (request.ProductoId == Guid.Empty)
        {
            error = "El identificador del producto es obligatorio.";
            return false;
        }

        if (request.Talla is { Length: > 50 })
        {
            error = "La talla de la variante no debe exceder 50 caracteres.";
            return false;
        }

        if (request.Color is { Length: > 80 })
        {
            error = "El color de la variante no debe exceder 80 caracteres.";
            return false;
        }

        if (request.CodigoSku is { Length: > 80 })
        {
            error = "El SKU de la variante no debe exceder 80 caracteres.";
            return false;
        }

        if (request.CodigoBarras is { Length: > 80 })
        {
            error = "El codigo de barras de la variante no debe exceder 80 caracteres.";
            return false;
        }

        if (request.StockActual < 0)
        {
            error = "El stock actual de la variante no puede ser negativo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Talla) &&
            string.IsNullOrWhiteSpace(request.Color) &&
            string.IsNullOrWhiteSpace(request.CodigoSku) &&
            string.IsNullOrWhiteSpace(request.CodigoBarras))
        {
            error = "La variante debe tener talla, color, SKU o codigo de barras.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(CrearClienteRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.TipoDocumento))
        {
            error = "El tipo de documento del cliente es obligatorio.";
            return false;
        }

        if (request.TipoDocumento.Length > 20)
        {
            error = "El tipo de documento del cliente no debe exceder 20 caracteres.";
            return false;
        }

        if (request.NumeroDocumento is { Length: > 20 })
        {
            error = "El numero de documento del cliente no debe exceder 20 caracteres.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.NombreRazonSocial))
        {
            error = "El nombre o razon social del cliente es obligatorio.";
            return false;
        }

        if (request.NombreRazonSocial.Length > 200)
        {
            error = "El nombre o razon social del cliente no debe exceder 200 caracteres.";
            return false;
        }

        if (request.Direccion is { Length: > 250 })
        {
            error = "La direccion del cliente no debe exceder 250 caracteres.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(CrearVentaRequest request, out string error)
    {
        if (request.ClienteId == Guid.Empty)
        {
            error = "El identificador del cliente no puede estar vacio.";
            return false;
        }

        if (request.Fecha == default(DateTimeOffset))
        {
            error = "La fecha de venta no es valida.";
            return false;
        }

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            error = "La venta debe tener al menos un detalle.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.CanalVenta) &&
            (!Enum.TryParse<CanalVenta>(
                request.CanalVenta.Trim(),
                ignoreCase: true,
                out var canalVenta) ||
            !Enum.IsDefined(canalVenta)))
        {
            error = "El canal de venta no es valido.";
            return false;
        }

        if (request.PuntoVentaId == Guid.Empty)
        {
            error = "El identificador del punto de venta es obligatorio.";
            return false;
        }

        if (request.VendedorId == Guid.Empty)
        {
            error = "El identificador del vendedor no puede estar vacio.";
            return false;
        }

        foreach (var detalle in request.Detalles)
        {
            if (!TryValidate(detalle, out error))
            {
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(EmitirCpeDesdeVentaRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.TipoComprobante))
        {
            error = "El tipo de comprobante es obligatorio.";
            return false;
        }

        if (request.TipoComprobante.Length > 2)
        {
            error = "El tipo de comprobante no debe exceder 2 caracteres.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RucEmisor))
        {
            error = "El RUC emisor es obligatorio.";
            return false;
        }

        if (request.RucEmisor.Length != 11 || request.RucEmisor.Any(static character => !char.IsDigit(character)))
        {
            error = "El RUC emisor debe tener 11 digitos.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(GuardarConfiguracionFiscalEmpresaRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.Ruc))
        {
            error = "El RUC es obligatorio.";
            return false;
        }

        if (request.Ruc.Length != 11 || request.Ruc.Any(static character => !char.IsDigit(character)))
        {
            error = "El RUC debe tener 11 digitos.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RazonSocial))
        {
            error = "La razon social es obligatoria.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Ubigeo))
        {
            error = "El ubigeo es obligatorio.";
            return false;
        }

        if (request.Ubigeo.Length != 6 || request.Ubigeo.Any(static character => !char.IsDigit(character)))
        {
            error = "El ubigeo debe tener 6 digitos.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Direccion))
        {
            error = "La direccion es obligatoria.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Departamento))
        {
            error = "El departamento es obligatorio.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Provincia))
        {
            error = "La provincia es obligatoria.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Distrito))
        {
            error = "El distrito es obligatorio.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(AjustarStockProductoRequest request, out string error)
    {
        if (request.SedeId == Guid.Empty)
        {
            error = "El identificador de la sede es obligatorio.";
            return false;
        }

        if (request.ProductoId == Guid.Empty)
        {
            error = "El identificador del producto es obligatorio.";
            return false;
        }

        if (request.ProductoVarianteId == Guid.Empty)
        {
            error = "El identificador de la variante no puede estar vacio.";
            return false;
        }

        if (request.CantidadDisponible < 0)
        {
            error = "La cantidad disponible no puede ser negativa.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidate(CrearVentaDetalleRequest request, out string error)
    {
        if (request.ProductoId == Guid.Empty)
        {
            error = "El identificador del producto es obligatorio.";
            return false;
        }

        if (request.ProductoVarianteId == Guid.Empty)
        {
            error = "El identificador de la variante no puede estar vacio.";
            return false;
        }

        if (request.Cantidad <= 0)
        {
            error = "La cantidad debe ser mayor que cero.";
            return false;
        }

        if (request.PrecioUnitario <= 0)
        {
            error = "El precio unitario debe ser mayor que cero.";
            return false;
        }

        if (request.Igv < 0)
        {
            error = "El IGV no puede ser negativo.";
            return false;
        }

        if (request.Total <= 0)
        {
            error = "El total del detalle debe ser mayor que cero.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateRol(RolEmpresa rol, out string error)
    {
        if (!Enum.IsDefined(rol))
        {
            error = "El rol indicado no es valido.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
