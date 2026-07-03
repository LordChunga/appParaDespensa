#nullable enable

namespace PosLocal.Services;

public sealed record PesoProductoRequest(
    int ProductoId,
    string ProductoNombre,
    decimal PrecioBase,
    decimal PesoBaseGramos);

public sealed record PesoProductoResult(decimal PesoGramos);

public sealed record ExtrasVentaRequest(
    decimal Subtotal,
    decimal DescuentoActual,
    decimal RecargoActual);

public sealed record ExtrasVentaResult(
    TipoAjusteVenta TipoAjuste,
    TipoCalculoAjuste TipoCalculo,
    decimal Valor,
    decimal Descuento,
    decimal Recargo);

public sealed record CheckoutVentaItemDto(
    string Nombre,
    string CantidadTexto,
    decimal TotalLinea);

public sealed record CheckoutVentaRequest(
    string Cliente,
    decimal Subtotal,
    decimal Descuento,
    decimal Recargo,
    decimal Total,
    IReadOnlyList<CheckoutVentaItemDto> Items);

public sealed record CheckoutVentaResult(
    MetodoPagoVenta MetodoPago,
    bool UsaPagosMixtos,
    decimal MontoRecibido);
