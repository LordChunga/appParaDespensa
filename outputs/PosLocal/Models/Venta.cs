#nullable enable

namespace PosLocal.Models;

public sealed class Venta
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string Cliente { get; set; } = "Consumidor Final";

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Recargo { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string MetodoPago { get; set; } = "Efectivo";

    public string Estado { get; set; } = "Completada";

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
