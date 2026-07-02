#nullable enable

namespace PosLocal.Models;

public sealed class Combo
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal PrecioSugerido { get; set; }

    public decimal PrecioCombo { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaActualizacion { get; set; }
}
