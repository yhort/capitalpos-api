using CapitalPos.Api.ActiveCompany;
using CapitalPos.Api.Authorization;
using CapitalPos.Application.Catalogo;
using CapitalPos.Application.Seguridad;
using CapitalPos.Domain;

namespace CapitalPos.Api.Endpoints;

public static class CatalogoEndpoints
{
    public static IEndpointRouteBuilder MapCatalogoEndpoints(this IEndpointRouteBuilder app)
    {
        var categorias = app.MapGroup("/api/categorias")
            .WithTags("Categorias")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        categorias.MapGet("/", ListarCategoriasAsync)
            .WithName("ListarCategorias")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        categorias.MapPost("/", CrearCategoriaAsync)
            .WithName("CrearCategoria")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        var marcas = app.MapGroup("/api/marcas")
            .WithTags("Marcas")
            .RequireAuthorization()
            .AddEndpointFilter<EmpresaActivaEndpointFilter>();

        marcas.MapGet("/", ListarMarcasAsync)
            .WithName("ListarMarcas")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        marcas.MapPost("/", CrearMarcaAsync)
            .WithName("CrearMarca")
            .RequirePermisoEmpresa(PermisoEmpresa.OperarAlmacen);

        return app;
    }

    private static async Task<IResult> ListarCategoriasAsync(
        ListarCategoriasUseCase useCase,
        CancellationToken cancellationToken)
    {
        var categorias = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(categorias.Select(CategoriaResponse.From));
    }

    private static async Task<IResult> CrearCategoriaAsync(
        CrearCategoriaRequest request,
        CrearCategoriaUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!EndpointInputValidator.TryValidate(request, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        try
        {
            var categoria = await useCase.EjecutarAsync(request, cancellationToken);

            return Results.Created(
                $"/api/categorias/{categoria.Id}",
                CategoriaResponse.From(categoria));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }

    private static async Task<IResult> ListarMarcasAsync(
        ListarMarcasUseCase useCase,
        CancellationToken cancellationToken)
    {
        var marcas = await useCase.EjecutarAsync(cancellationToken);

        return Results.Ok(marcas.Select(MarcaResponse.From));
    }

    private static async Task<IResult> CrearMarcaAsync(
        CrearMarcaRequest request,
        CrearMarcaUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!EndpointInputValidator.TryValidate(request, out var error))
        {
            return Results.BadRequest(ErrorResponse.From(error));
        }

        try
        {
            var marca = await useCase.EjecutarAsync(request, cancellationToken);

            return Results.Created(
                $"/api/marcas/{marca.Id}",
                MarcaResponse.From(marca));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ErrorResponse.From(ex.Message));
        }
    }
}

public sealed record CategoriaResponse(
    Guid Id,
    Guid EmpresaId,
    Guid? CategoriaPadreId,
    string Nombre,
    bool Activa,
    DateTimeOffset FechaCreacion)
{
    public static CategoriaResponse From(Categoria categoria)
    {
        return new CategoriaResponse(
            categoria.Id,
            categoria.EmpresaId,
            categoria.CategoriaPadreId,
            categoria.Nombre,
            categoria.Activa,
            categoria.FechaCreacion);
    }
}

public sealed record MarcaResponse(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    bool Activa,
    DateTimeOffset FechaCreacion)
{
    public static MarcaResponse From(Marca marca)
    {
        return new MarcaResponse(
            marca.Id,
            marca.EmpresaId,
            marca.Nombre,
            marca.Activa,
            marca.FechaCreacion);
    }
}
