#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class GestionExtrasViewModel : ObservableObject
{
    private readonly decimal _descuentoActual;
    private readonly decimal _recargoActual;

    public GestionExtrasViewModel(ExtrasVentaRequest request)
    {
        Subtotal = request.Subtotal;
        _descuentoActual = request.DescuentoActual;
        _recargoActual = request.RecargoActual;

        SeleccionarDescuentoCommand = new RelayCommand(() => TipoAjuste = TipoAjusteVenta.Descuento);
        SeleccionarRecargoCommand = new RelayCommand(() => TipoAjuste = TipoAjusteVenta.Recargo);
        SeleccionarPorcentajeCommand = new RelayCommand(() => TipoCalculo = TipoCalculoAjuste.Porcentaje);
        SeleccionarMontoFijoCommand = new RelayCommand(() => TipoCalculo = TipoCalculoAjuste.MontoFijo);
        AplicarCommand = new RelayCommand(Aplicar);
        CancelarCommand = new RelayCommand(Cancelar);
    }

    public decimal Subtotal { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AjusteAplicar))]
    [NotifyPropertyChangedFor(nameof(NuevoTotal))]
    private TipoAjusteVenta _tipoAjuste = TipoAjusteVenta.Descuento;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AjusteAplicar))]
    [NotifyPropertyChangedFor(nameof(NuevoTotal))]
    private TipoCalculoAjuste _tipoCalculo = TipoCalculoAjuste.Porcentaje;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AjusteAplicar))]
    [NotifyPropertyChangedFor(nameof(NuevoTotal))]
    private decimal _valor;

    [ObservableProperty]
    private ExtrasVentaResult? _resultado;

    [ObservableProperty]
    private bool _fueAplicado;

    [ObservableProperty]
    private bool _debeCerrar;

    public decimal AjusteAplicar
    {
        get
        {
            var ajuste = TipoCalculo == TipoCalculoAjuste.Porcentaje
                ? Subtotal * (Valor / 100m)
                : Valor;

            return Math.Max(0m, ajuste);
        }
    }

    public decimal NuevoTotal
    {
        get
        {
            var descuento = TipoAjuste == TipoAjusteVenta.Descuento
                ? AjusteAplicar
                : _descuentoActual;

            var recargo = TipoAjuste == TipoAjusteVenta.Recargo
                ? AjusteAplicar
                : _recargoActual;

            return Math.Max(0m, Subtotal - descuento + recargo);
        }
    }

    public IRelayCommand SeleccionarDescuentoCommand { get; }

    public IRelayCommand SeleccionarRecargoCommand { get; }

    public IRelayCommand SeleccionarPorcentajeCommand { get; }

    public IRelayCommand SeleccionarMontoFijoCommand { get; }

    public IRelayCommand AplicarCommand { get; }

    public IRelayCommand CancelarCommand { get; }

    private void Aplicar()
    {
        var descuento = TipoAjuste == TipoAjusteVenta.Descuento ? AjusteAplicar : 0m;
        var recargo = TipoAjuste == TipoAjusteVenta.Recargo ? AjusteAplicar : 0m;

        Resultado = new ExtrasVentaResult(TipoAjuste, TipoCalculo, Valor, descuento, recargo);
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
