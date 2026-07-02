#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class GestionProveedoresViewModel : ObservableObject
{
    private readonly IProductoCatalogoService _productoService;
    private readonly IProductoCatalogoDialogService _dialogService;

    public GestionProveedoresViewModel(
        IProductoCatalogoService productoService,
        IProductoCatalogoDialogService dialogService)
    {
        _productoService = productoService;
        _dialogService = dialogService;
        Proveedores = new ObservableCollection<ProveedorCatalogoItemViewModel>();

        CargarCommand = new AsyncRelayCommand(CargarAsync);
        NuevoCommand = new RelayCommand(Nuevo);
        GuardarCommand = new AsyncRelayCommand(GuardarAsync, PuedeGuardar);
        EliminarCommand = new AsyncRelayCommand(EliminarAsync, PuedeEliminar);
    }

    public ObservableCollection<ProveedorCatalogoItemViewModel> Proveedores { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    [NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
    private ProveedorCatalogoItemViewModel? _proveedorSeleccionado;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string? _telefono;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _notas;

    [ObservableProperty]
    private bool _activo = true;

    [ObservableProperty]
    private string _mensajeEstado = "Administra proveedores del catalogo.";

    public IAsyncRelayCommand CargarCommand { get; }

    public IRelayCommand NuevoCommand { get; }

    public IAsyncRelayCommand GuardarCommand { get; }

    public IAsyncRelayCommand EliminarCommand { get; }

    partial void OnProveedorSeleccionadoChanged(ProveedorCatalogoItemViewModel? value)
    {
        Nombre = value?.Nombre ?? string.Empty;
        Telefono = value?.Telefono;
        Email = value?.Email;
        Notas = value?.Notas;
        Activo = value?.Activo ?? true;
    }

    private async Task CargarAsync()
    {
        var proveedores = await _productoService.ObtenerProveedoresAsync();
        Proveedores.Clear();
        foreach (var proveedor in proveedores)
        {
            Proveedores.Add(new ProveedorCatalogoItemViewModel(proveedor));
        }
    }

    private void Nuevo()
    {
        ProveedorSeleccionado = null;
        Nombre = string.Empty;
        Telefono = null;
        Email = null;
        Notas = null;
        Activo = true;
    }

    private bool PuedeGuardar()
    {
        return !string.IsNullOrWhiteSpace(Nombre);
    }

    private async Task GuardarAsync()
    {
        try
        {
            await _productoService.GuardarProveedorAsync(new ProveedorUpsertRequest(
                ProveedorSeleccionado?.Id,
                Nombre.Trim(),
                Telefono,
                Email,
                Notas,
                Activo));

            MensajeEstado = "Proveedor guardado.";
            await CargarAsync();
            Nuevo();
        }
        catch (Exception ex)
        {
            await _dialogService.MostrarMensajeAsync("Error", ex.Message);
        }
    }

    private bool PuedeEliminar()
    {
        return ProveedorSeleccionado is not null;
    }

    private async Task EliminarAsync()
    {
        if (ProveedorSeleccionado is null)
        {
            return;
        }

        try
        {
            await _productoService.EliminarProveedorAsync(ProveedorSeleccionado.Id);
            MensajeEstado = "Proveedor eliminado.";
            await CargarAsync();
            Nuevo();
        }
        catch (Exception ex)
        {
            await _dialogService.MostrarMensajeAsync("No se pudo eliminar", ex.Message);
        }
    }
}
