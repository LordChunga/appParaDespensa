#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PosLocal.ViewModels;

public sealed partial class MontoLibreViewModel : ObservableObject
{
    public MontoLibreViewModel()
    {
        AplicarCommand = new RelayCommand(Aplicar, PuedeAplicar);
        CancelarCommand = new RelayCommand(Cancelar);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AplicarCommand))]
    private decimal _monto;

    [ObservableProperty]
    private decimal? _resultado;

    [ObservableProperty]
    private bool _fueAplicado;

    [ObservableProperty]
    private bool _debeCerrar;

    public IRelayCommand AplicarCommand { get; }

    public IRelayCommand CancelarCommand { get; }

    private bool PuedeAplicar()
    {
        return Monto > 0m;
    }

    private void Aplicar()
    {
        Resultado = Monto;
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
