#nullable enable

namespace PosLocal.Services;

public interface IInventarioDialogService
{
    Task<AjusteStockDialogResult?> SolicitarAjusteStockAsync(
        AjusteStockDialogRequest request,
        CancellationToken cancellationToken = default);

    Task AbrirMovimientosProductoAsync(
        int productoId,
        string productoNombre,
        CancellationToken cancellationToken = default);

    Task MostrarMensajeAsync(
        string titulo,
        string mensaje,
        CancellationToken cancellationToken = default);
}
