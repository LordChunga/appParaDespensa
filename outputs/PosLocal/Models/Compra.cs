#nullable enable

namespace PosLocal.Models;

public sealed class Compra
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string? NumeroComprobante { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string Usuario { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; } = "Registrada";

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
