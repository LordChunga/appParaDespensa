#nullable enable

namespace PosLocal.Models;

public sealed class Producto
{
    public int Id { get; set; }

    public int CategoriaId { get; set; }

    public int? ProveedorId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public decimal PrecioCosto { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal StockActual { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal StockMaximo { get; set; } = 100m;

    public string UnidadMedida { get; set; } = "UN";

    public bool VentaPorPeso { get; set; }

    public decimal PesoBaseGramos { get; set; } = 1000m;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaActualizacion { get; set; }
}
