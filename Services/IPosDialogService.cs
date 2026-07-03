#nullable enable

namespace PosLocal.Services;

public interface IPosDialogService
{
    Task<PesoProductoResult?> SolicitarPesoAsync(
        PesoProductoRequest request,
        CancellationToken cancellationToken = default);

    Task<decimal?> SolicitarMontoLibreAsync(
        CancellationToken cancellationToken = default);

    Task<ExtrasVentaResult?> GestionarExtrasAsync(
        ExtrasVentaRequest request,
        CancellationToken cancellationToken = default);

    Task<CheckoutVentaResult?> ProcesarCheckoutAsync(
        CheckoutVentaRequest request,
        CancellationToken cancellationToken = default);

    Task MostrarMensajeAsync(
        string titulo,
        string mensaje,
        CancellationToken cancellationToken = default);
}
