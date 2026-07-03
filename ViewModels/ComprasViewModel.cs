#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class ComprasViewModel : ObservableObject
{
    private readonly ICompraService _compraService;
    private readonly ICompraDialogService _dialogService;

    public ComprasViewModel(
        ICompraService compraService,
        ICompraDialogService dialogService)
    {
        _compraService = compraService;
        _dialogService = dialogService;

        Compras = new ObservableCollection<CompraHistorialItemViewModel>();

        CargarComprasCommand = new AsyncRelayCommand(CargarComprasAsync);
        BuscarComprasCommand = new AsyncRelayCommand(BuscarComprasAsync, PuedeEjecutarOperacion);
        LimpiarBusquedaCommand = new AsyncRelayCommand(LimpiarBusquedaAsync, PuedeEjecutarOperacion);
        VerDetalleCommand = new AsyncRelayCommand<CompraHistorialItemViewModel?>(VerDetalleAsync, PuedeVerDetalle);
        NuevaCompraCommand = new AsyncRelayCommand(NuevaCompraAsync, PuedeEjecutarOperacion);
        ExportarHistorialCommand = new AsyncRelayCommand(ExportarHistorialAsync, PuedeExportar);
        PaginaAnteriorCommand = new AsyncRelayCommand(PaginaAnteriorAsync, PuedeIrPaginaAnterior);
        PaginaSiguienteCommand = new AsyncRelayCommand(PaginaSiguienteAsync, PuedeIrPaginaSiguiente);
        VerMovimientosInventarioCommand = new AsyncRelayCommand(VerMovimientosInventarioAsync, PuedeVerMovimientos);
    }

    public ObservableCollection<CompraHistorialItemViewModel> Compras { get; }

    public int TamanoPagina { get; } = 10;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuscarComprasCommand))]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuscarComprasCommand))]
    [NotifyCanExecuteChangedFor(nameof(LimpiarBusquedaCommand))]
    [NotifyCanExecuteChangedFor(nameof(NuevaCompraCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportarHistorialCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaAnteriorCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaSiguienteCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerMovimientosInventarioCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayDetalleSeleccionado))]
    [NotifyCanExecuteChangedFor(nameof(VerMovimientosInventarioCommand))]
    private CompraDetalleStockViewModel? _detalleSeleccionado;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(VerDetalleCommand))]
    private CompraHistorialItemViewModel? _compraSeleccionada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrandoTexto))]
    [NotifyPropertyChangedFor(nameof(TotalPaginas))]
    [NotifyCanExecuteChangedFor(nameof(ExportarHistorialCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaAnteriorCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaSiguienteCommand))]
    private int _totalCompras;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrandoTexto))]
    [NotifyPropertyChangedFor(nameof(PaginaActualTexto))]
    [NotifyCanExecuteChangedFor(nameof(PaginaAnteriorCommand))]
    [NotifyCanExecuteChangedFor(nameof(PaginaSiguienteCommand))]
    private int _paginaActual = 1;

    [ObservableProperty]
    private string _mensajeEstado = "Carga el historial o registra una nueva compra.";

    public bool HayDetalleSeleccionado => DetalleSeleccionado is not null;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalCompras / (double)TamanoPagina));

    public string PaginaActualTexto => PaginaActual.ToString();

    public string MostrandoTexto
    {
        get
        {
            if (TotalCompras == 0)
            {
                return "Mostrando 0 de 0 compras";
            }

            var visibles = Math.Min(PaginaActual * TamanoPagina, TotalCompras);
            return $"Mostrando {visibles} de {TotalCompras} compras";
        }
    }

    public IAsyncRelayCommand CargarComprasCommand { get; }

    public IAsyncRelayCommand BuscarComprasCommand { get; }

    public IAsyncRelayCommand LimpiarBusquedaCommand { get; }

    public IAsyncRelayCommand<CompraHistorialItemViewModel?> VerDetalleCommand { get; }

    public IAsyncRelayCommand NuevaCompraCommand { get; }

    public IAsyncRelayCommand ExportarHistorialCommand { get; }

    public IAsyncRelayCommand PaginaAnteriorCommand { get; }

    public IAsyncRelayCommand PaginaSiguienteCommand { get; }

    public IAsyncRelayCommand VerMovimientosInventarioCommand { get; }

    private bool PuedeEjecutarOperacion()
    {
        return !IsBusy;
    }

    private bool PuedeVerDetalle(CompraHistorialItemViewModel? item)
    {
        return !IsBusy && item is not null;
    }

    private bool PuedeExportar()
    {
        return !IsBusy && TotalCompras > 0;
    }

    private bool PuedeIrPaginaAnterior()
    {
        return !IsBusy && PaginaActual > 1;
    }

    private bool PuedeIrPaginaSiguiente()
    {
        return !IsBusy && PaginaActual < TotalPaginas;
    }

    private bool PuedeVerMovimientos()
    {
        return !IsBusy && DetalleSeleccionado is not null;
    }

    private async Task CargarComprasAsync()
    {
        await EjecutarOperacionAsync(CargarPaginaActualAsync);
    }

    private async Task BuscarComprasAsync()
    {
        PaginaActual = 1;
        DetalleSeleccionado = null;
        await EjecutarOperacionAsync(CargarPaginaActualAsync);
    }

    private async Task LimpiarBusquedaAsync()
    {
        TextoBusqueda = string.Empty;
        PaginaActual = 1;
        DetalleSeleccionado = null;
        await EjecutarOperacionAsync(CargarPaginaActualAsync);
    }

    private async Task VerDetalleAsync(CompraHistorialItemViewModel? compra)
    {
        if (compra is null)
        {
            return;
        }

        CompraSeleccionada = compra;

        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var detalle = await _compraService.ObtenerDetalleAsync(compra.CompraDetalleId, cancellationToken);

            if (detalle is null)
            {
                DetalleSeleccionado = null;
                MensajeEstado = "No se encontro el detalle de la compra seleccionada.";
                return;
            }

            DetalleSeleccionado = new CompraDetalleStockViewModel(detalle);
            MensajeEstado = DetalleSeleccionado.AlertaCriticaActiva
                ? $"Alerta activa: {DetalleSeleccionado.ProductoNombre} esta en cantidad critica."
                : $"Detalle cargado para {DetalleSeleccionado.ProductoNombre}.";
        });
    }

    private async Task NuevaCompraAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            await _dialogService.AbrirNuevaCompraAsync(cancellationToken);
            PaginaActual = 1;
            await CargarPaginaActualAsync(cancellationToken);
        });
    }

    private async Task ExportarHistorialAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var rutaDestino = await _dialogService.SolicitarRutaExportacionAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rutaDestino))
            {
                return;
            }

            await _compraService.ExportarHistorialAsync(
                new ExportarComprasRequest(TextoBusqueda, TotalCompras, rutaDestino),
                cancellationToken);

            MensajeEstado = $"Historial exportado en {rutaDestino}.";
        });
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

    private async Task VerMovimientosInventarioAsync()
    {
        if (DetalleSeleccionado is null)
        {
            return;
        }

        await EjecutarOperacionAsync(cancellationToken =>
            _dialogService.AbrirMovimientosInventarioAsync(
                DetalleSeleccionado.ProductoId,
                DetalleSeleccionado.ProductoNombre,
                cancellationToken));
    }

    private async Task CargarPaginaActualAsync(CancellationToken cancellationToken)
    {
        var result = await _compraService.BuscarHistorialAsync(
            new CompraHistorialQuery(TextoBusqueda, PaginaActual, TamanoPagina),
            cancellationToken);

        Compras.Clear();
        foreach (var compra in result.Items)
        {
            Compras.Add(new CompraHistorialItemViewModel(compra));
        }

        TotalCompras = result.TotalRegistros;
        MensajeEstado = TotalCompras == 0
            ? "No hay compras para los filtros actuales."
            : "Historial de compras actualizado.";
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
            MensajeEstado = "Ocurrio un error al procesar compras.";
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
        BuscarComprasCommand.NotifyCanExecuteChanged();
        LimpiarBusquedaCommand.NotifyCanExecuteChanged();
        VerDetalleCommand.NotifyCanExecuteChanged();
        NuevaCompraCommand.NotifyCanExecuteChanged();
        ExportarHistorialCommand.NotifyCanExecuteChanged();
        PaginaAnteriorCommand.NotifyCanExecuteChanged();
        PaginaSiguienteCommand.NotifyCanExecuteChanged();
        VerMovimientosInventarioCommand.NotifyCanExecuteChanged();
    }
}
