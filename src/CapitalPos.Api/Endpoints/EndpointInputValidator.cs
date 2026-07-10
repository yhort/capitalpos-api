using CapitalPos.Application.Empresas;
using CapitalPos.Application.Clientes;
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
