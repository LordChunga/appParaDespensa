#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using PosLocal.Services;
using PosLocal.Views;

namespace PosLocal.ViewModels;

public sealed partial class ReportesViewModel : ObservableObject
{
    private readonly IReportesService _reportesService;
    private readonly IReportesDialogService _dialogService;
    private readonly IAppNavigationService _navigationService;
    private bool _reloadRequested;

    public ReportesViewModel(
        IReportesService reportesService,
        IReportesDialogService dialogService,
        IAppNavigationService navigationService)
    {
        _reportesService = reportesService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        Kpis = new ObservableCollection<KpiReporteCardViewModel>();
        ResumenSecundario = new ObservableCollection<ResumenReporteCardViewModel>();
        ProductosMasGanancia = new ObservableCollection<RankingReporteItemViewModel>();
        ProductosMasVendidos = new ObservableCollection<RankingReporteItemViewModel>();

        var firstDayOfCurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _fechaHasta = firstDayOfCurrentMonth.AddDays(-1);
        _fechaDesde = new DateTime(_fechaHasta.Value.Year, _fechaHasta.Value.Month, 1);

        CargarReportesCommand = new AsyncRelayCommand(CargarReportesAsync);
        AplicarFiltrosCommand = new AsyncRelayCommand(CargarReportesAsync, PuedeEjecutarOperacion);
        ExportarReporteCommand = new AsyncRelayCommand(ExportarReporteAsync, PuedeEjecutarOperacion);
        MostrarPuntoVentaCommand = new RelayCommand(_navigationService.NavigateTo<PuntoVentaView>);
        MostrarComprasCommand = new RelayCommand(_navigationService.NavigateTo<ComprasView>);
        MostrarProductosCommand = new RelayCommand(_navigationService.NavigateTo<ProductosView>);
        MostrarInventarioCommand = new RelayCommand(_navigationService.NavigateTo<InventarioProductosView>);
        MostrarReportesCommand = new RelayCommand(_navigationService.NavigateTo<ReportesView>);

        ResetVisualState();
    }

    public ObservableCollection<KpiReporteCardViewModel> Kpis { get; }

    public ObservableCollection<ResumenReporteCardViewModel> ResumenSecundario { get; }

    public ObservableCollection<RankingReporteItemViewModel> ProductosMasGanancia { get; }

    public ObservableCollection<RankingReporteItemViewModel> ProductosMasVendidos { get; }

    [ObservableProperty]
    private DateTime? _fechaDesde;

    [ObservableProperty]
    private DateTime? _fechaHasta;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AplicarFiltrosCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportarReporteCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _mensajeEstado = "Selecciona un per\u00edodo para analizar el rendimiento.";

    [ObservableProperty]
    private ISeries[] _ingresosGananciaSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _ingresosGananciaXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _moneyYAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _metodosPagoSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private bool _tieneDatosIngresos;

    [ObservableProperty]
    private bool _tieneMetodosPago;

    [ObservableProperty]
    private bool _tieneRankingGanancia;

    [ObservableProperty]
    private bool _tieneRankingVentas;

    public IAsyncRelayCommand CargarReportesCommand { get; }

    public IAsyncRelayCommand AplicarFiltrosCommand { get; }

    public IAsyncRelayCommand ExportarReporteCommand { get; }

    public IRelayCommand MostrarPuntoVentaCommand { get; }

    public IRelayCommand MostrarComprasCommand { get; }

    public IRelayCommand MostrarProductosCommand { get; }

    public IRelayCommand MostrarInventarioCommand { get; }

    public IRelayCommand MostrarReportesCommand { get; }

    partial void OnFechaDesdeChanged(DateTime? value)
    {
        SolicitarRecargaPorCambioFecha();
    }

    partial void OnFechaHastaChanged(DateTime? value)
    {
        SolicitarRecargaPorCambioFecha();
    }

    private void SolicitarRecargaPorCambioFecha()
    {
        if (IsBusy)
        {
            _reloadRequested = true;
            return;
        }

        _ = AplicarFiltrosCommand.ExecuteAsync(null);
    }

    private bool PuedeEjecutarOperacion()
    {
        return !IsBusy;
    }

    private async Task CargarReportesAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var request = BuildRequest();
            var dashboard = await _reportesService.ObtenerDashboardAsync(request, cancellationToken);
            ApplyDashboard(dashboard);
            MensajeEstado = $"Reportes actualizados: {request.FechaDesde:dd/MM/yyyy} - {request.FechaHasta:dd/MM/yyyy}.";
        });
    }

    private async Task ExportarReporteAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var ruta = await _dialogService.SolicitarRutaExportacionAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            await _reportesService.ExportarDashboardCsvAsync(BuildRequest(), ruta, cancellationToken);
            MensajeEstado = $"Reporte exportado en {ruta}.";
        });
    }

    private ReportesPeriodoRequest BuildRequest()
    {
        var hasta = (FechaHasta ?? DateTime.Today).Date;
        var desde = (FechaDesde ?? hasta.AddDays(-30)).Date;

        return desde <= hasta
            ? new ReportesPeriodoRequest(desde, hasta)
            : new ReportesPeriodoRequest(hasta, desde);
    }

    private void ApplyDashboard(DashboardReportesDto dashboard)
    {
        Kpis.Clear();
        Kpis.Add(new KpiReporteCardViewModel("$", "Ingresos del periodo", dashboard.Kpis.IngresosPeriodo.ToString("C")));
        Kpis.Add(new KpiReporteCardViewModel("\u2197", "Ganancia estimada", dashboard.Kpis.GananciaEstimada.ToString("C"), "?"));
        Kpis.Add(new KpiReporteCardViewModel("\uE9D2", "Total de Ventas", dashboard.Kpis.TotalVentas.ToString("N0"), iconFontFamily: "Segoe MDL2 Assets"));
        Kpis.Add(new KpiReporteCardViewModel("\uF0E3", "Ticket Promedio", dashboard.Kpis.TicketPromedio.ToString("C"), iconFontFamily: "Segoe MDL2 Assets"));

        ResumenSecundario.Clear();
        ResumenSecundario.Add(new ResumenReporteCardViewModel("\uE8C7", "Pago m\u00e1s usado", dashboard.ResumenSecundario.PagoMasUsado, "#4A2A16"));
        ResumenSecundario.Add(new ResumenReporteCardViewModel("\uE7B8", "Producto m\u00e1s popular", dashboard.ResumenSecundario.ProductoMasPopular, "#172A3A"));
        ResumenSecundario.Add(new ResumenReporteCardViewModel("\uE787", "Pr\u00f3ximo vencimiento", dashboard.ResumenSecundario.ProximoVencimiento, "#3A171B"));
        ResumenSecundario.Add(new ResumenReporteCardViewModel("\uE734", "Plan activo", dashboard.ResumenSecundario.PlanActivo, "#2A2140", dashboard.ResumenSecundario.PlanSubtitulo));

        var labels = dashboard.SeriesDiarias.Select(item => item.Fecha.ToString("dd/MM")).ToArray();
        var ingresos = dashboard.SeriesDiarias.Select(item => (double)item.Ingresos).ToArray();
        var ganancias = dashboard.SeriesDiarias.Select(item => (double)item.Ganancia).ToArray();

        IngresosGananciaSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Ingresos",
                Values = ingresos
            },
            new LineSeries<double>
            {
                Name = "Ganancia",
                Values = ganancias,
                GeometrySize = 10
            }
        };

        IngresosGananciaXAxes = new[]
        {
            new Axis
            {
                Labels = labels
            }
        };

        MoneyYAxes = new[]
        {
            new Axis
            {
                Labeler = value => value.ToString("C0")
            }
        };

        MetodosPagoSeries = dashboard.MetodosPago
            .Where(item => item.Total > 0m)
            .Select(item => (ISeries)new PieSeries<double>
            {
                Name = item.MetodoPago,
                Values = new[] { (double)item.Total },
                DataLabelsSize = 12
            })
            .ToArray();

        ProductosMasGanancia.Clear();
        foreach (var item in dashboard.ProductosMasGanancia)
        {
            ProductosMasGanancia.Add(new RankingReporteItemViewModel(item));
        }

        ProductosMasVendidos.Clear();
        foreach (var item in dashboard.ProductosMasVendidos)
        {
            ProductosMasVendidos.Add(new RankingReporteItemViewModel(item));
        }

        TieneDatosIngresos = dashboard.SeriesDiarias.Any(item => item.Ingresos > 0m || item.Ganancia > 0m);
        TieneMetodosPago = dashboard.MetodosPago.Any(item => item.Total > 0m);
        TieneRankingGanancia = dashboard.ProductosMasGanancia.Count > 0;
        TieneRankingVentas = dashboard.ProductosMasVendidos.Count > 0;
    }

    private void ResetVisualState()
    {
        Kpis.Clear();
        Kpis.Add(new KpiReporteCardViewModel("$", "Ingresos del periodo", "$ 0"));
        Kpis.Add(new KpiReporteCardViewModel("\u2197", "Ganancia estimada", "$ 0", "?"));
        Kpis.Add(new KpiReporteCardViewModel("\uE9D2", "Total de Ventas", "0", iconFontFamily: "Segoe MDL2 Assets"));
        Kpis.Add(new KpiReporteCardViewModel("\uF0E3", "Ticket Promedio", "$ 0", iconFontFamily: "Segoe MDL2 Assets"));

        ResumenSecundario.Clear();
        ResumenSecundario.Add(new ResumenReporteCardViewModel("\uE8C7", "Pago m\u00e1s usado", "N/A", "#4A2A16"));
        ResumenSecundario.Add(new ResumenReporteCardViewModel("\uE7B8", "Producto m\u00e1s popular", "Sin ventas", "#172A3A"));
        ResumenSecundario.Add(new ResumenReporteCardViewModel("\uE787", "Pr\u00f3ximo vencimiento", "Sin vencimientos", "#3A171B"));
        ResumenSecundario.Add(new ResumenReporteCardViewModel("\uE734", "Plan activo", "Plan Gratis", "#2A2140", "Vence 08 de jul de 2026"));

        IngresosGananciaSeries = Array.Empty<ISeries>();
        IngresosGananciaXAxes = Array.Empty<Axis>();
        MoneyYAxes = Array.Empty<Axis>();
        MetodosPagoSeries = Array.Empty<ISeries>();
        TieneDatosIngresos = false;
        TieneMetodosPago = false;
        TieneRankingGanancia = false;
        TieneRankingVentas = false;
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
            MensajeEstado = "Operaci\u00f3n cancelada.";
        }
        catch (Exception ex)
        {
            MensajeEstado = "Ocurri\u00f3 un error al cargar reportes.";
            await _dialogService.MostrarMensajeAsync("Error", ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
            AplicarFiltrosCommand.NotifyCanExecuteChanged();
            ExportarReporteCommand.NotifyCanExecuteChanged();

            if (_reloadRequested)
            {
                _reloadRequested = false;
                _ = AplicarFiltrosCommand.ExecuteAsync(null);
            }
        }
    }
}
