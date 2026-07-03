#nullable enable

namespace PosLocal.Services;

public interface IPosProductoService
{
    Task<ProductoVentaDto?> BuscarPorTextoOCodigoAsync(
        string criterio,
        CancellationToken cancellationToken = default);

    Task<ProductoVentaDto?> BuscarPorCodigoBarrasAsync(
        string codigoBarras,
        CancellationToken cancellationToken = default);
}
