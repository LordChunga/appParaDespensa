#nullable enable

namespace PosLocal.Services;

public sealed record CompraHistorialQuery(
    string? TextoBusqueda,
    int Pagina,
    int TamanoPagina);

public sealed record CompraHistorialDto(
    int CompraDetalleId,
    int CompraId,
    string CompraNumero,
    int ProductoId,
    string ProductoNombre,
    string CodigoBarras,
    DateTime Fecha,
    string Unidad,
    decimal CostoUnitario,
    decimal Cantidad,
    string Usuario,
    string ProveedorNombre);

public sealed record CompraHistorialResult(
    IReadOnlyList<CompraHistorialDto> Items,
    int TotalRegistros);

public sealed record CompraDetalleStockDto(
    int CompraDetalleId,
    int CompraId,
    string CompraNumero,
    int ProductoId,
    string ProductoNombre,
    string CodigoBarras,
    DateTime Fecha,
    string Unidad,
    decimal CostoUnitario,
    decimal Cantidad,
    string Usuario,
    string ProveedorNombre,
    string ProveedorContacto,
    string ProveedorTelefono,
    string UltimoPedidoTexto,
    decimal StockActual,
    decimal StockMaximo,
    decimal CantidadCritica,
    DateTime? ProximaEntregaEstimada);

public sealed record ExportarComprasRequest(
    string? TextoBusqueda,
    int TotalRegistros,
    string RutaDestino);
