#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class AjusteStockViewModel : ObservableObject
{
    public AjusteStockViewModel(AjusteStockDialogRequest request)
    {
        ProductoId = request.ProductoId;
        ProductoNombre = request.ProductoNombre;
        StockActual = request.StockActual;
        UnidadMedida = request.UnidadMedida;

        SumarCommand = new RelayCommand(() => TipoAjuste = 1);
        RestarCommand = new RelayCommand(() => TipoAjuste = -1);
        AplicarCommand = new RelayCommand(Aplicar, PuedeAplicar);
        CancelarCommand = new RelayCommand(Cancelar);
    }

    public int ProductoId { get; }

    public string ProductoNombre { get; }

    public decimal StockActual { get; }

    public string UnidadMedida { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StockResultante))]
    [NotifyCanExecuteChangedFor(nameof(AplicarCommand))]
    private decimal _cantidad;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StockResultante))]
    private int _tipoAjuste = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AplicarCommand))]
    private string _motivo = string.Empty;

    [ObservableProperty]
    private AjusteStockDialogResult? _resultado;

    [ObservableProperty]
    private bool _fueAplicado;

    [ObservableProperty]
    private bool _debeCerrar;

    public decimal CantidadAjuste => Cantidad * TipoAjuste;

    public decimal StockResultante => StockActual + CantidadAjuste;

    public IRelayCommand SumarCommand { get; }

    public IRelayCommand RestarCommand { get; }

    public IRelayCommand AplicarCommand { get; }

    public IRelayCommand CancelarCommand { get; }

    partial void OnTipoAjusteChanged(int value)
    {
        AplicarCommand.NotifyCanExecuteChanged();
    }

    private bool PuedeAplicar()
    {
        return Cantidad > 0m
            && StockResultante >= 0m
            && !string.IsNullOrWhiteSpace(Motivo);
    }

    private void Aplicar()
    {
        Resultado = new AjusteStockDialogResult(CantidadAjuste, Motivo.Trim());
        FueAplicado = true;
        DebeCerrar = true;
    }

    private void Cancelar()
    {
        Resultado = null;
        FueAplicado = false;
        DebeCerrar = true;
    }
}
