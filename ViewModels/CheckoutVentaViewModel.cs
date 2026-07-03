#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class CheckoutVentaViewModel : ObservableObject
{
    public CheckoutVentaViewModel(CheckoutVentaRequest request)
    {
        Cliente = request.Cliente;
        Subtotal = request.Subtotal;
        Descuento = request.Descuento;
        Recargo = request.Recargo;
        Total = request.Total;
        Items = new ObservableCollection<CheckoutVentaItemDto>(request.Items);

        SeleccionarEfectivoCommand = new RelayCommand(() => MetodoPago = MetodoPagoVenta.Efectivo);
        SeleccionarTarjetaCommand = new RelayCommand(() => MetodoPago = MetodoPagoVenta.Tarjeta);
        SeleccionarTransferenciaCommand = new RelayCommand(() => MetodoPago = MetodoPagoVenta.Transferencia);
        SeleccionarQrCommand = new RelayCommand(() => MetodoPago = MetodoPagoVenta.Qr);
        ConfirmarCommand = new RelayCommand(Confirmar, PuedeConfirmar);
        CancelarCommand = new RelayCommand(Cancelar);
    }

    public string Cliente { get; }

    public decimal Subtotal { get; }

    public decimal Descuento { get; }

    public decimal Recargo { get; }

    public decimal Total { get; }

    public ObservableCollection<CheckoutVentaItemDto> Items { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsEfectivo))]
    [NotifyPropertyChangedFor(nameof(EsTransferencia))]
    private MetodoPagoVenta _metodoPago = MetodoPagoVenta.Efectivo;

    [ObservableProperty]
    private bool _usaPagosMixtos;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmarCommand))]
    private decimal _montoRecibido;

    [ObservableProperty]
    private CheckoutVentaResult? _resultado;

    [ObservableProperty]
    private bool _fueConfirmado;

    [ObservableProperty]
    private bool _debeCerrar;

    public bool EsEfectivo => MetodoPago == MetodoPagoVenta.Efectivo;

    public bool EsTransferencia => MetodoPago == MetodoPagoVenta.Transferencia;

    public decimal Vuelto => MetodoPago == MetodoPagoVenta.Efectivo
        ? Math.Max(0m, MontoRecibido - Total)
        : 0m;

    public IRelayCommand SeleccionarEfectivoCommand { get; }

    public IRelayCommand SeleccionarTarjetaCommand { get; }

    public IRelayCommand SeleccionarTransferenciaCommand { get; }

    public IRelayCommand SeleccionarQrCommand { get; }

    public IRelayCommand ConfirmarCommand { get; }

    public IRelayCommand CancelarCommand { get; }

    partial void OnMetodoPagoChanged(MetodoPagoVenta value)
    {
        if (value != MetodoPagoVenta.Efectivo)
        {
            MontoRecibido = Total;
        }

        ConfirmarCommand.NotifyCanExecuteChanged();
    }

    private bool PuedeConfirmar()
    {
        return MetodoPago != MetodoPagoVenta.Efectivo || MontoRecibido >= Total;
    }

    private void Confirmar()
    {
        Resultado = new CheckoutVentaResult(MetodoPago, UsaPagosMixtos, MontoRecibido);
        FueConfirmado = true;
        DebeCerrar = true;
    }

    private void Cancelar()
    {
        Resultado = null;
        FueConfirmado = false;
        DebeCerrar = true;
    }
}
