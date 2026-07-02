#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;

namespace PosLocal.ViewModels;

public sealed partial class ComboDetalleItemViewModel : ObservableObject
{
    public ComboDetalleItemViewModel(ComboProductoDisponibleItemViewModel producto)
    {
        ProductoId = producto.ProductoId;
        Nombre = producto.Nombre;
        Codigo = producto.Codigo;
        PrecioUnitario = producto.PrecioVenta;
        StockActual = producto.StockActual;
        UnidadMedida = producto.UnidadMedida;
    }

    public int ProductoId { get; }

    public string Nombre { get; }

    public string Codigo { get; }

    public decimal PrecioUnitario { get; }

    public decimal StockActual { get; }

    public string UnidadMedida { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalLinea))]
    private decimal _cantidad = 1m;

    public decimal TotalLinea => Cantidad * PrecioUnitario;
}
