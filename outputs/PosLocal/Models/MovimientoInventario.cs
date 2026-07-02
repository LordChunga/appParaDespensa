#nullable enable

namespace PosLocal.Models;

public sealed class MovimientoInventario
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public int? CompraId { get; set; }

    public int? VentaId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string Tipo { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal StockAnterior { get; set; }

    public decimal StockNuevo { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}
