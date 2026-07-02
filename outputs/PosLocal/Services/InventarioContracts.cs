#nullable enable

namespace PosLocal.Services;

public sealed record InventarioProductoQuery(
    string? TextoBusqueda,
    int Pagina,
    int TamanoPagina,
    string? OrdenarPor,
    bool OrdenDescendente);

public sealed record InventarioProductoDto(
    int ProductoId,
    string Nombre,
    string Codigo,
    string? CodigoBarras,
    string CategoriaNombre,
    decimal StockActual,
    decimal StockMinimo,
    string UnidadMedida,
    string? ProveedorNombre,
    bool Activo);

public sealed record InventarioProductoResult(
    IReadOnlyList<InventarioProductoDto> Items,
    int TotalRegistros);

public sealed record AjusteStockRequest(
    int ProductoId,
    decimal CantidadAjuste,
    string Motivo,
    string Usuario);

public sealed record AjusteStockDialogRequest(
    int ProductoId,
    string ProductoNombre,
    decimal StockActual,
    string UnidadMedida);

public sealed record AjusteStockDialogResult(
    decimal CantidadAjuste,
    string Motivo);
