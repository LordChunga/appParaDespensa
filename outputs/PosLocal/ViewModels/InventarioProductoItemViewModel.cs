#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class InventarioProductoItemViewModel : ObservableObject
{
    public InventarioProductoItemViewModel(InventarioProductoDto dto)
    {
        ProductoId = dto.ProductoId;
        Nombre = dto.Nombre;
        Codigo = dto.Codigo;
        CodigoBarras = dto.CodigoBarras;
        CategoriaNombre = dto.CategoriaNombre;
        StockActual = dto.StockActual;
        StockMinimo = dto.StockMinimo;
        UnidadMedida = dto.UnidadMedida;
        ProveedorNombre = string.IsNullOrWhiteSpace(dto.ProveedorNombre) ? "Sin proveedor" : dto.ProveedorNombre;
        Activo = dto.Activo;
    }

    public int ProductoId { get; }

    public string Nombre { get; }

    public string Codigo { get; }

    public string? CodigoBarras { get; }

    public string CategoriaNombre { get; }

    public decimal StockActual { get; }

    public decimal StockMinimo { get; }

    public string UnidadMedida { get; }

    public string ProveedorNombre { get; }

    [ObservableProperty]
    private bool _activo;

    public bool StockCritico => StockActual <= StockMinimo;

    public string StockTexto => $"{StockActual:0.##} {UnidadMedida.ToLowerInvariant()}";
}
