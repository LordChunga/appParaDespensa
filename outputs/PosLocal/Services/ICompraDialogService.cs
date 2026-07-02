#nullable enable

namespace PosLocal.Services;

public interface ICompraDialogService
{
    Task AbrirNuevaCompraAsync(CancellationToken cancellationToken = default);

    Task AbrirMovimientosInventarioAsync(
        int productoId,
        string productoNombre,
        CancellationToken cancellationToken = default);

    Task<string?> SolicitarRutaExportacionAsync(CancellationToken cancellationToken = default);

    Task MostrarMensajeAsync(
        string titulo,
        string mensaje,
        CancellationToken cancellationToken = default);
}
