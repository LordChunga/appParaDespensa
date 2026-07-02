#nullable enable

namespace PosLocal.Services;

public interface IProductoCatalogoDialogService
{
    Task<ProductoUpsertRequest?> AbrirProductoManualAsync(
        ProductoUpsertRequest? productoActual,
        CancellationToken cancellationToken = default);

    Task AbrirGestionCategoriasAsync(CancellationToken cancellationToken = default);

    Task AbrirGestionProveedoresAsync(CancellationToken cancellationToken = default);

    Task AbrirNuevoComboAsync(CancellationToken cancellationToken = default);

    Task<string?> SolicitarRutaExportacionCsvAsync(CancellationToken cancellationToken = default);

    Task<string?> SolicitarRutaImportacionCsvAsync(CancellationToken cancellationToken = default);

    Task MostrarMensajeAsync(
        string titulo,
        string mensaje,
        CancellationToken cancellationToken = default);
}
