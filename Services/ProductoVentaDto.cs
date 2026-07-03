#nullable enable

namespace PosLocal.Services;

public sealed record ProductoVentaDto(
    int Id,
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    decimal PrecioVenta,
    decimal StockActual,
    string UnidadMedida,
    bool VentaPorPeso,
    decimal PesoBaseGramos,
    int? ComboId = null,
    bool EsCombo = false);
