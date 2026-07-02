#nullable enable

namespace PosLocal.Services;

public interface ICompraService
{
    Task<CompraHistorialResult> BuscarHistorialAsync(
        CompraHistorialQuery query,
        CancellationToken cancellationToken = default);

    Task<CompraDetalleStockDto?> ObtenerDetalleAsync(
        int compraDetalleId,
        CancellationToken cancellationToken = default);

    Task ExportarHistorialAsync(
        ExportarComprasRequest request,
        CancellationToken cancellationToken = default);
}
