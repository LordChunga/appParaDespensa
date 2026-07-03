#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class ProductoCatalogoItemViewModel : ObservableObject
{
    public ProductoCatalogoItemViewModel(ProductoCatalogoDto dto)
    {
        Id = dto.Id;
        Codigo = dto.Codigo;
        CodigoBarras = dto.CodigoBarras;
        Nombre = dto.Nombre;
        CategoriaNombre = dto.CategoriaNombre;
        CategoriaId = dto.CategoriaId;
        StockActual = dto.StockActual;
        StockMinimo = dto.StockMinimo;
        StockMaximo = dto.StockMaximo;
        UnidadMedida = dto.UnidadMedida;
        PrecioCosto = dto.PrecioCosto;
        PrecioVenta = dto.PrecioVenta;
        ProveedorNombre = dto.ProveedorNombre;
        ProveedorId = dto.ProveedorId;
        Activo = dto.Activo;
        VentaPorPeso = dto.VentaPorPeso;
        PesoBaseGramos = dto.PesoBaseGramos;
    }

    public int Id { get; }

    public string Codigo { get; }

    public string? CodigoBarras { get; }

    public string Nombre { get; }

    public string CategoriaNombre { get; }

    public int CategoriaId { get; }

    public decimal StockActual { get; }

    public decimal StockMinimo { get; }

    public decimal StockMaximo { get; }

    public string UnidadMedida { get; }

    public decimal PrecioCosto { get; }

    public decimal PrecioVenta { get; }

    public string? ProveedorNombre { get; }

    public int? ProveedorId { get; }

    public bool VentaPorPeso { get; }

    public decimal PesoBaseGramos { get; }

    [ObservableProperty]
    private bool _seleccionado;

    [ObservableProperty]
    private bool _activo;

    public string StockTexto => $"{StockActual:0.##} {UnidadMedida.ToLowerInvariant()}";

    public string PrecioVentaTexto => $"{PrecioVenta:C}";

    public string PrecioNetoTexto => $"{PrecioCosto:C}";

    public ProductoUpsertRequest ToUpsertRequest()
    {
        return new ProductoUpsertRequest(
            Id,
            Codigo,
            CodigoBarras,
            Nombre,
            null,
            CategoriaId,
            ProveedorId,
            StockActual,
            StockMinimo,
            StockMaximo,
            UnidadMedida,
            PrecioCosto,
            PrecioVenta,
            VentaPorPeso,
            PesoBaseGramos,
            Activo);
    }
}
