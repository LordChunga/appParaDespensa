#nullable enable

namespace PosLocal.Services;

public sealed record ProductoCatalogoQuery(
    string? TextoBusqueda,
    int? CategoriaId,
    int? ProveedorId,
    bool? Activo,
    string? OrdenarPor,
    bool OrdenDescendente);

public sealed record ProductoCatalogoDto(
    int Id,
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    string CategoriaNombre,
    int CategoriaId,
    decimal StockActual,
    decimal StockMinimo,
    decimal StockMaximo,
    string UnidadMedida,
    decimal PrecioCosto,
    decimal PrecioVenta,
    string? ProveedorNombre,
    int? ProveedorId,
    bool Activo,
    bool VentaPorPeso,
    decimal PesoBaseGramos);

public sealed record ProductoUpsertRequest(
    int? Id,
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    string? Descripcion,
    int CategoriaId,
    int? ProveedorId,
    decimal StockActual,
    decimal StockMinimo,
    decimal StockMaximo,
    string UnidadMedida,
    decimal PrecioCosto,
    decimal PrecioVenta,
    bool VentaPorPeso,
    decimal PesoBaseGramos,
    bool Activo);

public sealed record CategoriaCatalogoDto(
    int Id,
    string Nombre,
    string? Descripcion,
    bool Activa,
    int ProductosAsignados);

public sealed record CategoriaUpsertRequest(
    int? Id,
    string Nombre,
    string? Descripcion,
    bool Activa);

public sealed record ProveedorCatalogoDto(
    int Id,
    string Nombre,
    string? Telefono,
    string? Email,
    string? Notas,
    bool Activo,
    int ProductosAsignados);

public sealed record ProveedorUpsertRequest(
    int? Id,
    string Nombre,
    string? Telefono,
    string? Email,
    string? Notas,
    bool Activo);

public sealed record ProductoCsvImportResult(
    int Insertados,
    int Actualizados,
    int Omitidos,
    IReadOnlyList<string> Errores);

public sealed record ComboProductoDisponibleDto(
    int ProductoId,
    string Nombre,
    string Codigo,
    string? CodigoBarras,
    decimal PrecioVenta,
    decimal StockActual,
    string UnidadMedida);

public sealed record ComboDetalleRequest(
    int ProductoId,
    decimal Cantidad,
    decimal PrecioUnitarioSnapshot);

public sealed record ComboUpsertRequest(
    int? Id,
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    decimal PrecioSugerido,
    decimal PrecioCombo,
    bool Activo,
    IReadOnlyList<ComboDetalleRequest> Detalles);
