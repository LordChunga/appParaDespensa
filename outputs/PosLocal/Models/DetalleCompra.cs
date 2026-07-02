#nullable enable

namespace PosLocal.Models;

public sealed class DetalleCompra
{
    public int Id { get; set; }

    public int CompraId { get; set; }

    public int ProductoId { get; set; }

    public string ProductoCodigo { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public string Unidad { get; set; } = "UNITARIO";

    public decimal CostoUnitario { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Subtotal { get; set; }
}
