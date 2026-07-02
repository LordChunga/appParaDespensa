#nullable enable

namespace PosLocal.Services;

public interface IInventarioService
{
    Task<InventarioProductoResult> BuscarProductosAsync(
        InventarioProductoQuery query,
        CancellationToken cancellationToken = default);

    Task CambiarDisponibilidadAsync(
        int productoId,
        bool activo,
        CancellationToken cancellationToken = default);

    Task AjustarStockAsync(
        AjusteStockRequest request,
        CancellationToken cancellationToken = default);
}
