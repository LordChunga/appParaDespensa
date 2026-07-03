#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class IngresoPesoViewModel : ObservableObject
{
    public IngresoPesoViewModel(PesoProductoRequest request)
    {
        ProductoId = request.ProductoId;
        ProductoNombre = request.ProductoNombre;
        PrecioBase = request.PrecioBase;
        PesoBaseGramos = request.PesoBaseGramos <= 0m ? 1000m : request.PesoBaseGramos;

        AplicarCommand = new RelayCommand(Aplicar, PuedeAplicar);
        CancelarCommand = new RelayCommand(Cancelar);
    }

    public int ProductoId { get; }

    public string ProductoNombre { get; }

    public decimal PrecioBase { get; }

    public decimal PesoBaseGramos { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalEstimado))]
    [NotifyPropertyChangedFor(nameof(PesoKilogramosTexto))]
    [NotifyCanExecuteChangedFor(nameof(AplicarCommand))]
    private decimal _pesoGramos;

    [ObservableProperty]
    private PesoProductoResult? _resultado;

    [ObservableProperty]
    private bool _fueAplicado;

    [ObservableProperty]
    private bool _debeCerrar;

    public decimal TotalEstimado => Math.Max(0m, (PesoGramos / PesoBaseGramos) * PrecioBase);

    public string PesoKilogramosTexto => $"{PesoGramos / 1000m:0.000} Kg";

    public IRelayCommand AplicarCommand { get; }

    public IRelayCommand CancelarCommand { get; }

    private bool PuedeAplicar()
    {
        return PesoGramos > 0m;
    }

    private void Aplicar()
    {
        Resultado = new PesoProductoResult(PesoGramos);
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
