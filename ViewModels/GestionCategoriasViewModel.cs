#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class GestionCategoriasViewModel : ObservableObject
{
    private readonly IProductoCatalogoService _productoService;
    private readonly IProductoCatalogoDialogService _dialogService;

    public GestionCategoriasViewModel(
        IProductoCatalogoService productoService,
        IProductoCatalogoDialogService dialogService)
    {
        _productoService = productoService;
        _dialogService = dialogService;
        Categorias = new ObservableCollection<CategoriaCatalogoItemViewModel>();

        CargarCommand = new AsyncRelayCommand(CargarAsync);
        NuevaCommand = new RelayCommand(Nueva);
        GuardarCommand = new AsyncRelayCommand(GuardarAsync, PuedeGuardar);
        EliminarCommand = new AsyncRelayCommand(EliminarAsync, PuedeEliminar);
    }

    public ObservableCollection<CategoriaCatalogoItemViewModel> Categorias { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    [NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
    private CategoriaCatalogoItemViewModel? _categoriaSeleccionada;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string? _descripcion;

    [ObservableProperty]
    private bool _activa = true;

    [ObservableProperty]
    private string _mensajeEstado = "Administra categorias del catalogo.";

    public IAsyncRelayCommand CargarCommand { get; }

    public IRelayCommand NuevaCommand { get; }

    public IAsyncRelayCommand GuardarCommand { get; }

    public IAsyncRelayCommand EliminarCommand { get; }

    partial void OnCategoriaSeleccionadaChanged(CategoriaCatalogoItemViewModel? value)
    {
        Nombre = value?.Nombre ?? string.Empty;
        Descripcion = value?.Descripcion;
        Activa = value?.Activa ?? true;
    }

    private async Task CargarAsync()
    {
        var categorias = await _productoService.ObtenerCategoriasAsync();
        Categorias.Clear();
        foreach (var categoria in categorias)
        {
            Categorias.Add(new CategoriaCatalogoItemViewModel(categoria));
        }
    }

    private void Nueva()
    {
        CategoriaSeleccionada = null;
        Nombre = string.Empty;
        Descripcion = null;
        Activa = true;
    }

    private bool PuedeGuardar()
    {
        return !string.IsNullOrWhiteSpace(Nombre);
    }

    private async Task GuardarAsync()
    {
        try
        {
            await _productoService.GuardarCategoriaAsync(new CategoriaUpsertRequest(
                CategoriaSeleccionada?.Id,
                Nombre.Trim(),
                Descripcion,
                Activa));

            MensajeEstado = "Categoria guardada.";
            await CargarAsync();
            Nueva();
        }
        catch (Exception ex)
        {
            await _dialogService.MostrarMensajeAsync("Error", ex.Message);
        }
    }

    private bool PuedeEliminar()
    {
        return CategoriaSeleccionada is not null;
    }

    private async Task EliminarAsync()
    {
        if (CategoriaSeleccionada is null)
        {
            return;
        }

        try
        {
            await _productoService.EliminarCategoriaAsync(CategoriaSeleccionada.Id);
            MensajeEstado = "Categoria eliminada.";
            await CargarAsync();
            Nueva();
        }
        catch (Exception ex)
        {
            await _dialogService.MostrarMensajeAsync("No se pudo eliminar", ex.Message);
        }
    }
}
