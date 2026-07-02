#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed class ComboProductoDisponibleItemViewModel : ObservableObject
{
    public ComboProductoDisponibleItemViewModel(ComboProductoDisponibleDto dto)
    {
        ProductoId = dto.ProductoId;
        Nombre = dto.Nombre;
        Codigo = dto.Codigo;
        CodigoBarras = dto.CodigoBarras;
        PrecioVenta = dto.PrecioVenta;
        StockActual = dto.StockActual;
        UnidadMedida = dto.UnidadMedida;
    }

    public int ProductoId { get; }

    public string Nombre { get; }

    public string Codigo { get; }

    public string? CodigoBarras { get; }

    public decimal PrecioVenta { get; }

    public decimal StockActual { get; }

    public string UnidadMedida { get; }

    public string StockTexto => $"{StockActual:0.##} {UnidadMedida.ToLowerInvariant()}";
}
