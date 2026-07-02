#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class NuevoComboViewModel : ObservableObject
{
    private readonly IProductoCatalogoService _productoService;
    private readonly IProductoCatalogoDialogService _dialogService;

    public NuevoComboViewModel(
        IProductoCatalogoService productoService,
        IProductoCatalogoDialogService dialogService)
    {
        _productoService = productoService;
        _dialogService = dialogService;

        ProductosDisponibles = new ObservableCollection<ComboProductoDisponibleItemViewModel>();
        Detalles = new ObservableCollection<ComboDetalleItemViewModel>();
        Detalles.CollectionChanged += OnDetallesCollectionChanged;

        CargarCommand = new AsyncRelayCommand(CargarAsync);
        BuscarProductosCommand = new AsyncRelayCommand(BuscarProductosAsync);
        AgregarProductoCommand = new RelayCommand<ComboProductoDisponibleItemViewModel?>(AgregarProducto);
        QuitarProductoCommand = new RelayCommand<ComboDetalleItemViewModel?>(QuitarProducto);
        GuardarCommand = new AsyncRelayCommand(GuardarAsync, PuedeGuardar);
        CancelarCommand = new RelayCommand(Cancelar);
    }

    public ObservableCollection<ComboProductoDisponibleItemViewModel> ProductosDisponibles { get; }

    public ObservableCollection<ComboDetalleItemViewModel> Detalles { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string _codigo = string.Empty;

    [ObservableProperty]
    private string? _codigoBarras;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _textoBusquedaProducto = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AhorroTexto))]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private decimal _precioCombo;

    [ObservableProperty]
    private bool _activo = true;

    [ObservableProperty]
    private bool _fueGuardado;

    [ObservableProperty]
    private bool _debeCerrar;

    [ObservableProperty]
    private string _mensajeEstado = "Arma un combo con productos existentes.";

    public decimal PrecioSugerido => Detalles.Sum(item => item.TotalLinea);

    public string AhorroTexto => PrecioSugerido > 0m
        ? $"Diferencia: {(PrecioSugerido - PrecioCombo):C}"
        : "Diferencia: $0,00";

    public IAsyncRelayCommand CargarCommand { get; }

    public IAsyncRelayCommand BuscarProductosCommand { get; }

    public IRelayCommand<ComboProductoDisponibleItemViewModel?> AgregarProductoCommand { get; }

    public IRelayCommand<ComboDetalleItemViewModel?> QuitarProductoCommand { get; }

    public IAsyncRelayCommand GuardarCommand { get; }

    public IRelayCommand CancelarCommand { get; }

    private async Task CargarAsync()
    {
        await BuscarProductosAsync();
    }

    private async Task BuscarProductosAsync()
    {
        var productos = await _productoService.BuscarProductosParaComboAsync(TextoBusquedaProducto);
        ProductosDisponibles.Clear();
        foreach (var producto in productos)
        {
            ProductosDisponibles.Add(new ComboProductoDisponibleItemViewModel(producto));
        }
    }

    private void AgregarProducto(ComboProductoDisponibleItemViewModel? producto)
    {
        if (producto is null)
        {
            return;
        }

        var existente = Detalles.FirstOrDefault(item => item.ProductoId == producto.ProductoId);
        if (existente is not null)
        {
            existente.Cantidad += 1m;
            return;
        }

        Detalles.Add(new ComboDetalleItemViewModel(producto));
        PrecioCombo = PrecioCombo <= 0m ? PrecioSugerido : PrecioCombo;
        MensajeEstado = $"{producto.Nombre} agregado al combo.";
    }

    private void QuitarProducto(ComboDetalleItemViewModel? detalle)
    {
        if (detalle is null)
        {
            return;
        }

        Detalles.Remove(detalle);
    }

    private bool PuedeGuardar()
    {
        return !string.IsNullOrWhiteSpace(Codigo)
            && !string.IsNullOrWhiteSpace(Nombre)
            && PrecioCombo >= 0m
            && Detalles.Count > 0;
    }

    private async Task GuardarAsync()
    {
        try
        {
            await _productoService.GuardarComboAsync(new ComboUpsertRequest(
                null,
                Codigo.Trim(),
                string.IsNullOrWhiteSpace(CodigoBarras) ? null : CodigoBarras.Trim(),
                Nombre.Trim(),
                PrecioSugerido,
                PrecioCombo,
                Activo,
                Detalles.Select(item => new ComboDetalleRequest(
                    item.ProductoId,
                    item.Cantidad,
                    item.PrecioUnitario)).ToList()));

            FueGuardado = true;
            MensajeEstado = "Combo guardado correctamente.";
            DebeCerrar = true;
        }
        catch (Exception ex)
        {
            await _dialogService.MostrarMensajeAsync("Error", ex.Message);
        }
    }

    private void Cancelar()
    {
        FueGuardado = false;
        DebeCerrar = true;
    }

    private void OnDetallesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ComboDetalleItemViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnDetallePropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ComboDetalleItemViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnDetallePropertyChanged;
            }
        }

        NotifyTotalsChanged();
    }

    private void OnDetallePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ComboDetalleItemViewModel.TotalLinea)
            or nameof(ComboDetalleItemViewModel.Cantidad))
        {
            NotifyTotalsChanged();
        }
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(PrecioSugerido));
        OnPropertyChanged(nameof(AhorroTexto));
        GuardarCommand.NotifyCanExecuteChanged();
    }
}
