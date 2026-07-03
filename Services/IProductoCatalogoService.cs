#nullable enable

namespace PosLocal.Services;

public interface IProductoCatalogoService
{
    Task<IReadOnlyList<ProductoCatalogoDto>> BuscarProductosAsync(
        ProductoCatalogoQuery query,
        CancellationToken cancellationToken = default);

    Task<int> GuardarProductoAsync(
        ProductoUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task CambiarEstadoProductoAsync(
        int productoId,
        bool activo,
        CancellationToken cancellationToken = default);

    Task ExportarProductosCsvAsync(
        ProductoCatalogoQuery query,
        string rutaDestino,
        CancellationToken cancellationToken = default);

    Task<ProductoCsvImportResult> ImportarProductosCsvAsync(
        string rutaOrigen,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoriaCatalogoDto>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default);

    Task<int> GuardarCategoriaAsync(
        CategoriaUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task EliminarCategoriaAsync(
        int categoriaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProveedorCatalogoDto>> ObtenerProveedoresAsync(
        CancellationToken cancellationToken = default);

    Task<int> GuardarProveedorAsync(
        ProveedorUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task EliminarProveedorAsync(
        int proveedorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComboProductoDisponibleDto>> BuscarProductosParaComboAsync(
        string? textoBusqueda,
        CancellationToken cancellationToken = default);

    Task<int> GuardarComboAsync(
        ComboUpsertRequest request,
        CancellationToken cancellationToken = default);
}
