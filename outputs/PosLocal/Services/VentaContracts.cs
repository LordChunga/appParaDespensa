#nullable enable

namespace PosLocal.Services;

public enum MetodoPagoVenta
{
    Efectivo,
    Tarjeta,
    Transferencia,
    Qr
}

public enum TipoAjusteVenta
{
    Descuento,
    Recargo
}

public enum TipoCalculoAjuste
{
    Porcentaje,
    MontoFijo
}

public sealed record RegistrarVentaItemRequest(
    int? ProductoId,
    int? ComboId,
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal Impuesto,
    decimal TotalLinea);

public sealed record RegistrarVentaRequest(
    string Cliente,
    DateTime Fecha,
    decimal Subtotal,
    decimal Descuento,
    decimal Recargo,
    decimal Impuesto,
    decimal Total,
    MetodoPagoVenta MetodoPago,
    string Estado,
    decimal MontoRecibido,
    decimal Vuelto,
    IReadOnlyList<RegistrarVentaItemRequest> Items);

public sealed record TransferenciaPendienteDto(
    int VentaId,
    string Numero,
    DateTime Fecha,
    string Cliente,
    decimal Total,
    string Estado);
