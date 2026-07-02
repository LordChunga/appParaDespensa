#nullable enable

namespace PosLocal.Models;

public sealed class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }

    public int? ProductoId { get; set; }

    public int? ComboId { get; set; }

    public string ProductoCodigo { get; set; } = string.Empty;

    public string ProductoNombre { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal TotalLinea { get; set; }
}
