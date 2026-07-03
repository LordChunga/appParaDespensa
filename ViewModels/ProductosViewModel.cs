#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;
using PosLocal.Views;

namespace PosLocal.ViewModels;

public sealed partial class ProductosViewModel : ObservableObject
{
    private readonly IProductoCatalogoService _productoService;
    private readonly IProductoCatalogoDialogService _dialogService;
    private readonly IAppNavigationService _navigationService;

    public ProductosViewModel(
        IProductoCatalogoService productoService,
        IProductoCatalogoDialogService dialogService,
        IAppNavigationService navigationService)
    {
        _productoService = productoService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        Productos = new ObservableCollection<ProductoCatalogoItemViewModel>();
        Categorias = new ObservableCollection<CategoriaCatalogoDto>();
        Proveedores = new ObservableCollection<ProveedorCatalogoDto>();

        CargarProductosCommand = new AsyncRelayCommand(CargarProductosAsync);
        BuscarProductosCommand = new AsyncRelayCommand(BuscarProductosAsync, PuedeEjecutarOperacion);
        AbrirNuevoProductoCommand = new AsyncRelayCommand(AbrirNuevoProductoAsync, PuedeEjecutarOperacion);
        EditarProductoCommand = new AsyncRelayCommand<ProductoCatalogoItemViewModel?>(EditarProductoAsync, PuedeEditarProducto);
        CambiarEstadoProductoCommand = new AsyncRelayCommand<ProductoCatalogoItemViewModel?>(CambiarEstadoProductoAsync, PuedeEditarProducto);
        ExportarCsvCommand = new AsyncRelayCommand(ExportarCsvAsync, PuedeEjecutarOperacion);
        ImportarCsvCommand = new AsyncRelayCommand(ImportarCsvAsync, PuedeEjecutarOperacion);
        GestionarCategoriasCommand = new AsyncRelayCommand(GestionarCategoriasAsync, PuedeEjecutarOperacion);
        GestionarProveedoresCommand = new AsyncRelayCommand(GestionarProveedoresAsync, PuedeEjecutarOperacion);
        NuevoComboCommand = new AsyncRelayCommand(NuevoComboAsync, PuedeEjecutarOperacion);
        AplicarFiltrosCommand = new AsyncRelayCommand(AplicarFiltrosAsync, PuedeEjecutarOperacion);
        LimpiarFiltrosCommand = new AsyncRelayCommand(LimpiarFiltrosAsync, PuedeEjecutarOperacion);
        MostrarPuntoVentaCommand = new RelayCommand(_navigationService.NavigateTo<PuntoVentaView>);
        MostrarComprasCommand = new RelayCommand(_navigationService.NavigateTo<ComprasView>);
        MostrarProductosCommand = new RelayCommand(_navigationService.NavigateTo<ProductosView>);
        MostrarInventarioCommand = new RelayCommand(_navigationService.NavigateTo<InventarioProductosView>);
        MostrarReportesCommand = new RelayCommand(_navigationService.NavigateTo<ReportesView>);
    }

    public ObservableCollection<ProductoCatalogoItemViewModel> Productos { get; }

    public ObservableCollection<CategoriaCatalogoDto> Categorias { get; }

    public ObservableCollection<ProveedorCatalogoDto> Proveedores { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuscarProductosCommand))]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    private CategoriaCatalogoDto? _categoriaSeleccionada;

    [ObservableProperty]
    private ProveedorCatalogoDto? _proveedorSeleccionado;

    [ObservableProperty]
    private bool? _estadoActivoSeleccionado;

    [ObservableProperty]
    private ProductoCatalogoItemViewModel? _productoSeleccionado;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuscarProductosCommand))]
    [NotifyCanExecuteChangedFor(nameof(AbrirNuevoProductoCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportarCsvCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportarCsvCommand))]
    [NotifyCanExecuteChangedFor(nameof(GestionarCategoriasCommand))]
    [NotifyCanExecuteChangedFor(nameof(GestionarProveedoresCommand))]
    [NotifyCanExecuteChangedFor(nameof(NuevoComboCommand))]
    [NotifyCanExecuteChangedFor(nameof(AplicarFiltrosCommand))]
    [NotifyCanExecuteChangedFor(nameof(LimpiarFiltrosCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _mensajeEstado = "Gestiona el inventario de Despensa Isabel.";

    [ObservableProperty]
    private string _ordenarPor = "nombre";

    [ObservableProperty]
    private bool _ordenDescendente;

    public IAsyncRelayCommand CargarProductosCommand { get; }

    public IAsyncRelayCommand BuscarProductosCommand { get; }

    public IAsyncRelayCommand AbrirNuevoProductoCommand { get; }

    public IAsyncRelayCommand<ProductoCatalogoItemViewModel?> EditarProductoCommand { get; }

    public IAsyncRelayCommand<ProductoCatalogoItemViewModel?> CambiarEstadoProductoCommand { get; }

    public IAsyncRelayCommand ExportarCsvCommand { get; }

    public IAsyncRelayCommand ImportarCsvCommand { get; }

    public IAsyncRelayCommand GestionarCategoriasCommand { get; }

    public IAsyncRelayCommand GestionarProveedoresCommand { get; }

    public IAsyncRelayCommand NuevoComboCommand { get; }

    public IAsyncRelayCommand AplicarFiltrosCommand { get; }

    public IAsyncRelayCommand LimpiarFiltrosCommand { get; }

    public IRelayCommand MostrarPuntoVentaCommand { get; }

    public IRelayCommand MostrarComprasCommand { get; }

    public IRelayCommand MostrarProductosCommand { get; }

    public IRelayCommand MostrarInventarioCommand { get; }

    public IRelayCommand MostrarReportesCommand { get; }

    private bool PuedeEjecutarOperacion()
    {
        return !IsBusy;
    }

    private bool PuedeEditarProducto(ProductoCatalogoItemViewModel? item)
    {
        return !IsBusy && item is not null;
    }

    private async Task CargarProductosAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            await CargarFiltrosAsync(cancellationToken);
            await CargarGrillaAsync(cancellationToken);
        });
    }

    private async Task BuscarProductosAsync()
    {
        await EjecutarOperacionAsync(CargarGrillaAsync);
    }

    private async Task AplicarFiltrosAsync()
    {
        await EjecutarOperacionAsync(CargarGrillaAsync);
    }

    private async Task LimpiarFiltrosAsync()
    {
        TextoBusqueda = string.Empty;
        CategoriaSeleccionada = null;
        ProveedorSeleccionado = null;
        EstadoActivoSeleccionado = null;
        await EjecutarOperacionAsync(CargarGrillaAsync);
    }

    private async Task AbrirNuevoProductoAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var request = await _dialogService.AbrirProductoManualAsync(null, cancellationToken);
            if (request is null)
            {
                return;
            }

            await _productoService.GuardarProductoAsync(request, cancellationToken);
            await CargarFiltrosAsync(cancellationToken);
            await CargarGrillaAsync(cancellationToken);
            MensajeEstado = "Producto creado correctamente.";
        });
    }

    private async Task EditarProductoAsync(ProductoCatalogoItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var request = await _dialogService.AbrirProductoManualAsync(item.ToUpsertRequest(), cancellationToken);
            if (request is null)
            {
                return;
            }

            await _productoService.GuardarProductoAsync(request, cancellationToken);
            await CargarGrillaAsync(cancellationToken);
            MensajeEstado = "Producto actualizado correctamente.";
        });
    }

    private async Task CambiarEstadoProductoAsync(ProductoCatalogoItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await EjecutarOperacionAsync(async cancellationToken =>
        {
            await _productoService.CambiarEstadoProductoAsync(item.Id, item.Activo, cancellationToken);
            MensajeEstado = item.Activo
                ? $"{item.Nombre} activado."
                : $"{item.Nombre} desactivado.";
        });
    }

    private async Task ExportarCsvAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var ruta = await _dialogService.SolicitarRutaExportacionCsvAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            await _productoService.ExportarProductosCsvAsync(BuildQuery(), ruta, cancellationToken);
            MensajeEstado = $"Productos exportados en {ruta}.";
        });
    }

    private async Task ImportarCsvAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var ruta = await _dialogService.SolicitarRutaImportacionCsvAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            var result = await _productoService.ImportarProductosCsvAsync(ruta, cancellationToken);
            await CargarFiltrosAsync(cancellationToken);
            await CargarGrillaAsync(cancellationToken);

            MensajeEstado = $"Importacion CSV: {result.Insertados} nuevos, {result.Actualizados} actualizados, {result.Omitidos} omitidos.";
            if (result.Errores.Count > 0)
            {
                await _dialogService.MostrarMensajeAsync(
                    "Importacion con observaciones",
                    string.Join(Environment.NewLine, result.Errores.Take(12)),
                    cancellationToken);
            }
        });
    }

    private async Task GestionarCategoriasAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            await _dialogService.AbrirGestionCategoriasAsync(cancellationToken);
            await CargarFiltrosAsync(cancellationToken);
            await CargarGrillaAsync(cancellationToken);
        });
    }

    private async Task GestionarProveedoresAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            await _dialogService.AbrirGestionProveedoresAsync(cancellationToken);
            await CargarFiltrosAsync(cancellationToken);
            await CargarGrillaAsync(cancellationToken);
        });
    }

    private async Task NuevoComboAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            await _dialogService.AbrirNuevoComboAsync(cancellationToken);
            MensajeEstado = "Combo procesado.";
        });
    }

    private async Task CargarFiltrosAsync(CancellationToken cancellationToken)
    {
        var categorias = await _productoService.ObtenerCategoriasAsync(cancellationToken);
        Categorias.Clear();
        foreach (var categoria in categorias)
        {
            Categorias.Add(categoria);
        }

        var proveedores = await _productoService.ObtenerProveedoresAsync(cancellationToken);
        Proveedores.Clear();
        foreach (var proveedor in proveedores)
        {
            Proveedores.Add(proveedor);
        }
    }

    private async Task CargarGrillaAsync(CancellationToken cancellationToken)
    {
        var productos = await _productoService.BuscarProductosAsync(BuildQuery(), cancellationToken);

        Productos.Clear();
        foreach (var producto in productos)
        {
            Productos.Add(new ProductoCatalogoItemViewModel(producto));
        }

        MensajeEstado = productos.Count == 0
            ? "No hay productos para los filtros actuales."
            : $"{productos.Count} producto(s) cargados.";
    }

    private ProductoCatalogoQuery BuildQuery()
    {
        return new ProductoCatalogoQuery(
            TextoBusqueda,
            CategoriaSeleccionada?.Id,
            ProveedorSeleccionado?.Id,
            EstadoActivoSeleccionado,
            OrdenarPor,
            OrdenDescendente);
    }

    private async Task EjecutarOperacionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            MensajeEstado = "Operacion cancelada.";
        }
        catch (Exception ex)
        {
            MensajeEstado = "Ocurrio un error en catalogo de productos.";
            await _dialogService.MostrarMensajeAsync("Error", ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
            NotifyCommandsCanExecuteChanged();
        }
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        BuscarProductosCommand.NotifyCanExecuteChanged();
        AbrirNuevoProductoCommand.NotifyCanExecuteChanged();
        EditarProductoCommand.NotifyCanExecuteChanged();
        CambiarEstadoProductoCommand.NotifyCanExecuteChanged();
        ExportarCsvCommand.NotifyCanExecuteChanged();
        ImportarCsvCommand.NotifyCanExecuteChanged();
        GestionarCategoriasCommand.NotifyCanExecuteChanged();
        GestionarProveedoresCommand.NotifyCanExecuteChanged();
        NuevoComboCommand.NotifyCanExecuteChanged();
        AplicarFiltrosCommand.NotifyCanExecuteChanged();
        LimpiarFiltrosCommand.NotifyCanExecuteChanged();
    }
}
