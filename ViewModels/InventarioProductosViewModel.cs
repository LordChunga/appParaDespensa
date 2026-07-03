#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;
using PosLocal.Views;

namespace PosLocal.ViewModels;

public sealed partial class InventarioProductosViewModel : ObservableObject
{
    private readonly IInventarioService _inventarioService;
    private readonly IInventarioDialogService _dialogService;
    private readonly IAppNavigationService _navigationService;

    public InventarioProductosViewModel(
        IInventarioService inventarioService,
        IInventarioDialogService dialogService,
        IAppNavigationService navigationService)
    {
        _inventarioService = inventarioService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        Productos = new ObservableCollection<InventarioProductoItemViewModel>();
        TamañosPagina = new ObservableCollection<int> { 10, 25, 50, 100 };

        CargarCommand = new AsyncRelayCommand(CargarAsync);
        BuscarCommand = new AsyncRelayCommand(BuscarAsync, PuedeEjecutarOperacion);
        AplicarFiltrosCommand = new AsyncRelayCommand(BuscarAsync, PuedeEjecutarOperacion);
        PaginaAnteriorCommand = new AsyncRelayCommand(PaginaAnteriorAsync, PuedeIrPaginaAnterior);
        PaginaSiguienteCommand = new AsyncRelayCommand(PaginaSiguienteAsync, PuedeIrPaginaSiguiente);
        CambiarDisponibilidadCommand = new AsyncRelayCommand<InventarioProductoItemViewModel?>(CambiarDisponibilidadAsync, PuedeEditarProducto);
        AjustarStockCommand = new AsyncRelayCommand<InventarioProductoItemViewModel?>(AjustarStockAsync, PuedeEditarProducto);
        VerMovimientosCommand = new AsyncRelayCommand<InventarioProductoItemViewModel?>(VerMovimientosAsync, PuedeEditarProducto);
        MostrarPuntoVentaCommand = new RelayCommand(_navigationService.NavigateTo<PuntoVentaView>);
        MostrarComprasCommand = new RelayCommand(_navigationService.NavigateTo<ComprasView>);
        MostrarProductosCommand = new RelayCommand(_navigationService.NavigateTo<ProductosView>);
        MostrarInventarioCommand = new RelayCommand(_navigationService.NavigateTo<InventarioProductosView>);
        MostrarReportesCommand = new RelayCommand(_navigationService.NavigateTo<ReportesView>);
    }

    public ObservableCollection<InventarioProductoItemViewModel> Productos { get; }

    public ObservableCollection<int> TamañosPagina { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuscarCommand))]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    private InventarioProductoItemViewModel? _productoSeleccionado;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuscarCommand))]
    [NotifyCanExecuteChangedFor(nameof(AplicarFiltrosCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaAnteriorCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaSiguienteCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrandoTexto))]
    [NotifyPropertyChangedFor(nameof(TotalPaginas))]
    [NotifyCanExecuteChangedFor(nameof(PaginaAnteriorCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaSiguienteCommand))]
    private int _totalProductos;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrandoTexto))]
    [NotifyPropertyChangedFor(nameof(PaginaActualTexto))]
    [NotifyCanExecuteChangedFor(nameof(PaginaAnteriorCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaSiguienteCommand))]
    private int _paginaActual = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrandoTexto))]
    private int _tamanoPagina = 10;

    [ObservableProperty]
    private string _ordenarPor = "producto";

    [ObservableProperty]
    private bool _ordenDescendente;

    [ObservableProperty]
    private string _mensajeEstado = "Inventario listo para auditoria rapida.";

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalProductos / (double)TamanoPagina));

    public string PaginaActualTexto => PaginaActual.ToString();

    public string MostrandoTexto
    {
        get
        {
            if (TotalProductos == 0)
            {
                return "Mostrando 0 de 0 productos";
            }

            var visibles = Math.Min(PaginaActual * TamanoPagina, TotalProductos);
            return $"Mostrando {visibles} de {TotalProductos} productos";
        }
    }

    public IAsyncRelayCommand CargarCommand { get; }

    public IAsyncRelayCommand BuscarCommand { get; }

    public IAsyncRelayCommand AplicarFiltrosCommand { get; }

    public IAsyncRelayCommand PaginaAnteriorCommand { get; }

    public IAsyncRelayCommand PaginaSiguienteCommand { get; }

    public IAsyncRelayCommand<InventarioProductoItemViewModel?> CambiarDisponibilidadCommand { get; }

    public IAsyncRelayCommand<InventarioProductoItemViewModel?> AjustarStockCommand { get; }

    public IAsyncRelayCommand<InventarioProductoItemViewModel?> VerMovimientosCommand { get; }

    public IRelayCommand MostrarPuntoVentaCommand { get; }

    public IRelayCommand MostrarComprasCommand { get; }

    public IRelayCommand MostrarProductosCommand { get; }

    public IRelayCommand MostrarInventarioCommand { get; }

    public IRelayCommand MostrarReportesCommand { get; }

    partial void OnTamanoPaginaChanged(int value)
    {
        PaginaActual = 1;
    }

    private bool PuedeEjecutarOperacion()
    {
        return !IsBusy;
    }

    private bool PuedeEditarProducto(InventarioProductoItemViewModel? item)
    {
        return !IsBusy && item is not null;
    }

    private bool PuedeIrPaginaAnterior()
    {
        return !IsBusy && PaginaActual > 1;
    }

    private bool PuedeIrPaginaSiguiente()
    {
        return !IsBusy && PaginaActual < TotalPaginas;
    }

    private async Task CargarAsync()
    {
        await EjecutarOperacionAsync(CargarPaginaActualAsync);
    }

    private async Task BuscarAsync()
    {
        PaginaActual = 1;
        await EjecutarOperacionAsync(CargarPaginaActualAsync);
    }

    private async Task PaginaAnteriorAsync()
    {
        if (!PuedeIrPaginaAnterior())
        {
            return;
        }

        PaginaActual -= 1;
        await EjecutarOperacionAsync(CargarPaginaActualAsync);
    }

    private async Task PaginaSiguienteAsync()
    {
        if (!PuedeIrPaginaSiguiente())
        {
            return;
        }

        PaginaActual += 1;
        await EjecutarOperacionAsync(CargarPaginaActualAsync);
    }

    private async Task CambiarDisponibilidadAsync(InventarioProductoItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await EjecutarOperacionAsync(async cancellationToken =>
        {
            await _inventarioService.CambiarDisponibilidadAsync(item.ProductoId, item.Activo, cancellationToken);
            MensajeEstado = item.Activo
                ? $"{item.Nombre} disponible en POS."
                : $"{item.Nombre} desactivado para POS.";
        });
    }

    private async Task AjustarStockAsync(InventarioProductoItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var result = await _dialogService.SolicitarAjusteStockAsync(
                new AjusteStockDialogRequest(
                    item.ProductoId,
                    item.Nombre,
                    item.StockActual,
                    item.UnidadMedida),
                cancellationToken);

            if (result is null)
            {
                return;
            }

            await _inventarioService.AjustarStockAsync(
                new AjusteStockRequest(item.ProductoId, result.CantidadAjuste, result.Motivo, "Alejandro"),
                cancellationToken);

            await CargarPaginaActualAsync(cancellationToken);
            MensajeEstado = $"Stock ajustado para {item.Nombre}.";
        });
    }

    private async Task VerMovimientosAsync(InventarioProductoItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await EjecutarOperacionAsync(cancellationToken =>
            _dialogService.AbrirMovimientosProductoAsync(item.ProductoId, item.Nombre, cancellationToken));
    }

    private async Task CargarPaginaActualAsync(CancellationToken cancellationToken)
    {
        var result = await _inventarioService.BuscarProductosAsync(
            new InventarioProductoQuery(
                TextoBusqueda,
                PaginaActual,
                TamanoPagina,
                OrdenarPor,
                OrdenDescendente),
            cancellationToken);

        Productos.Clear();
        foreach (var producto in result.Items)
        {
            Productos.Add(new InventarioProductoItemViewModel(producto));
        }

        TotalProductos = result.TotalRegistros;
        MensajeEstado = TotalProductos == 0
            ? "No hay productos para los filtros actuales."
            : "Inventario actualizado.";
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
            MensajeEstado = "Ocurrio un error en inventario.";
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
        BuscarCommand.NotifyCanExecuteChanged();
        AplicarFiltrosCommand.NotifyCanExecuteChanged();
        PaginaAnteriorCommand.NotifyCanExecuteChanged();
        PaginaSiguienteCommand.NotifyCanExecuteChanged();
        CambiarDisponibilidadCommand.NotifyCanExecuteChanged();
        AjustarStockCommand.NotifyCanExecuteChanged();
        VerMovimientosCommand.NotifyCanExecuteChanged();
    }
}
