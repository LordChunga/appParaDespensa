#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed class CompraHistorialItemViewModel : ObservableObject
{
    public CompraHistorialItemViewModel(CompraHistorialDto dto)
    {
        CompraDetalleId = dto.CompraDetalleId;
        CompraId = dto.CompraId;
        CompraNumero = dto.CompraNumero;
        ProductoId = dto.ProductoId;
        ProductoNombre = dto.ProductoNombre;
        CodigoBarras = dto.CodigoBarras;
        Fecha = dto.Fecha;
        Unidad = dto.Unidad;
        CostoUnitario = dto.CostoUnitario;
        Cantidad = dto.Cantidad;
        Usuario = dto.Usuario;
        ProveedorNombre = dto.ProveedorNombre;
    }

    public int CompraDetalleId { get; }

    public int CompraId { get; }

    public string CompraNumero { get; }

    public int ProductoId { get; }

    public string ProductoNombre { get; }

    public string CodigoBarras { get; }

    public DateTime Fecha { get; }

    public string Unidad { get; }

    public decimal CostoUnitario { get; }

    public decimal Cantidad { get; }

    public string Usuario { get; }

    public string ProveedorNombre { get; }

    public decimal Subtotal => CostoUnitario * Cantidad;

    public string CantidadTexto => $"x{Cantidad:0.##}";
}
