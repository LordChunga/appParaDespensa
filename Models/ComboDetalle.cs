#nullable enable

namespace PosLocal.Models;

public sealed class ComboDetalle
{
    public int Id { get; set; }

    public int ComboId { get; set; }

    public int ProductoId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitarioSnapshot { get; set; }
}
