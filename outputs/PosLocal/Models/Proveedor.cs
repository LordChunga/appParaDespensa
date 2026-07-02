#nullable enable

namespace PosLocal.Models;

public sealed class Proveedor
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public string? NombreComercial { get; set; }

    public string? Documento { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Direccion { get; set; }

    public string? Notas { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaActualizacion { get; set; }
}
