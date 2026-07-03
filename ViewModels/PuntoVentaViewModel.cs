#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;
using PosLocal.Views;

namespace PosLocal.ViewModels;

public sealed partial class PuntoVentaViewModel : ObservableObject
{
    public const string SeccionPuntoVenta = "PuntoVenta";
    public const string SeccionTransferencias = "Transferencias";
    public const string SeccionSistema = "Sistema";

    private static readonly TimeSpan DoubleEnterWindow = TimeSpan.FromMilliseconds(900);

    private readonly IPosProductoService _productoService;
    private readonly IPosVentaService _ventaService;
    private readonly IPosDialogService _dialogService;
    private readonly IAppSettingsService _settingsService;
    private readonly IAppNavigationService _navigationService;
    private DateTime _ultimoEnterUtc = DateTime.MinValue;

    public PuntoVentaViewModel(
        IPosProductoService productoService,
        IPosVentaService ventaService,
        IPosDialogService dialogService,
        IAppSettingsService settingsService,
        IAppNavigationService navigationService)
    {
        _productoService = productoService;
        _ventaService = ventaService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _navigationService = navigationService;

        Clientes = new ObservableCollection<string> { "Consumidor Final" };
        Carrito = new ObservableCollection<CarritoVentaItemViewModel>();
        TransferenciasPendientes = new ObservableCollection<TransferenciaPendienteItemViewModel>();
        GeneralSettings = new ObservableCollection<SettingToggleItemViewModel>();
        PlanProSettings = new ObservableCollection<SettingToggleItemViewModel>();
        PlanIaSettings = new ObservableCollection<SettingToggleItemViewModel>();

        Carrito.CollectionChanged += OnCarritoCollectionChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;
        BuildSettingsItems();

        BuscarProductoCommand = new AsyncRelayCommand(BuscarProductoAsync, PuedeBuscarProducto);
        ProcesarCodigoBarrasCommand = new AsyncRelayCommand<string?>(ProcesarCodigoBarrasAsync);
        ConsultarPrecioCommand = new AsyncRelayCommand(ConsultarPrecioAsync, PuedeConsultarPrecio);
        IngresarPesoCommand = new AsyncRelayCommand(IngresarPesoAsync, PuedeIngresarPeso);
        AgregarMontoCommand = new AsyncRelayCommand(AgregarMontoAsync);
        IncrementarCantidadCommand = new RelayCommand<CarritoVentaItemViewModel>(IncrementarCantidad);
        DecrementarCantidadCommand = new RelayCommand<CarritoVentaItemViewModel>(DecrementarCantidad);
        EliminarItemCommand = new RelayCommand<CarritoVentaItemViewModel>(EliminarItem);
        GestionarExtrasCommand = new AsyncRelayCommand(GestionarExtrasAsync, PuedeProcesarVenta);
        ProcesarVentaCommand = new AsyncRelayCommand(ProcesarVentaAsync, PuedeProcesarVenta);
        ProcesarDobleEnterCommand = new AsyncRelayCommand(ProcesarDobleEnterAsync);
        MostrarPuntoVentaCommand = new RelayCommand(() => SeccionActiva = SeccionPuntoVenta);
        MostrarComprasCommand = new RelayCommand(_navigationService.NavigateTo<ComprasView>);
        MostrarProductosCommand = new RelayCommand(_navigationService.NavigateTo<ProductosView>);
        MostrarInventarioCommand = new RelayCommand(_navigationService.NavigateTo<InventarioProductosView>);
        MostrarReportesCommand = new RelayCommand(_navigationService.NavigateTo<ReportesView>);
        MostrarTransferenciasCommand = new AsyncRelayCommand(MostrarTransferenciasAsync);
        MostrarSistemaCommand = new RelayCommand(() => SeccionActiva = SeccionSistema);
        AprobarTransferenciaCommand = new AsyncRelayCommand<TransferenciaPendienteItemViewModel>(AprobarTransferenciaAsync);
    }

    public ObservableCollection<string> Clientes { get; }

    public ObservableCollection<CarritoVentaItemViewModel> Carrito { get; }

    public ObservableCollection<TransferenciaPendienteItemViewModel> TransferenciasPendientes { get; }

    public ObservableCollection<SettingToggleItemViewModel> GeneralSettings { get; }

    public ObservableCollection<SettingToggleItemViewModel> PlanProSettings { get; }

    public ObservableCollection<SettingToggleItemViewModel> PlanIaSettings { get; }

    public DateTime CajaHoraApertura { get; } = DateTime.Now;

    public decimal CajaEfectivoInicial { get; } = 1000m;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuscarProductoCommand))]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    private string _clienteSeleccionado = "Consumidor Final";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuscarProductoCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConsultarPrecioCommand))]
    [NotifyCanExecuteChangedFor(nameof(GestionarExtrasCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProcesarVentaCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConsultarPrecioCommand))]
    [NotifyCanExecuteChangedFor(nameof(IngresarPesoCommand))]
    private ProductoVentaDto? _productoSeleccionado;

    [ObservableProperty]
    private string _mensajeEstado = "Caja abierta. Escanea o busca un producto para comenzar.";

    [ObservableProperty]
    private string _seccionActiva = SeccionPuntoVenta;

    [ObservableProperty]
    private decimal _descuentoTotal;

    [ObservableProperty]
    private decimal _recargoTotal;

    public decimal Subtotal => Carrito.Sum(item => item.TotalLinea);

    public decimal Total => Math.Max(0m, Subtotal - DescuentoTotal + RecargoTotal);

    public int TotalItems => Carrito.Count;

    public bool CarritoVacio => Carrito.Count == 0;

    public string TotalItemsTexto => TotalItems == 1 ? "1 item" : $"{TotalItems} items";

    public int CantidadTransferenciasPendientes => TransferenciasPendientes.Count;

    public bool SistemaBalanzaActivo => _settingsService.Settings.SistemaBalanza;

    public string SettingsPath => _settingsService.SettingsPath;

    public IAsyncRelayCommand BuscarProductoCommand { get; }

    public IAsyncRelayCommand<string?> ProcesarCodigoBarrasCommand { get; }

    public IAsyncRelayCommand ConsultarPrecioCommand { get; }

    public IAsyncRelayCommand IngresarPesoCommand { get; }

    public IAsyncRelayCommand AgregarMontoCommand { get; }

    public IRelayCommand<CarritoVentaItemViewModel> IncrementarCantidadCommand { get; }

    public IRelayCommand<CarritoVentaItemViewModel> DecrementarCantidadCommand { get; }

    public IRelayCommand<CarritoVentaItemViewModel> EliminarItemCommand { get; }

    public IAsyncRelayCommand GestionarExtrasCommand { get; }

    public IAsyncRelayCommand ProcesarVentaCommand { get; }

    public IAsyncRelayCommand ProcesarDobleEnterCommand { get; }

    public IRelayCommand MostrarPuntoVentaCommand { get; }

    public IRelayCommand MostrarComprasCommand { get; }

    public IRelayCommand MostrarProductosCommand { get; }

    public IRelayCommand MostrarInventarioCommand { get; }

    public IRelayCommand MostrarReportesCommand { get; }

    public IAsyncRelayCommand MostrarTransferenciasCommand { get; }

    public IRelayCommand MostrarSistemaCommand { get; }

    public IAsyncRelayCommand<TransferenciaPendienteItemViewModel> AprobarTransferenciaCommand { get; }

    partial void OnDescuentoTotalChanged(decimal value)
    {
        NotifySaleTotalsChanged();
    }

    partial void OnRecargoTotalChanged(decimal value)
    {
        NotifySaleTotalsChanged();
    }

    private bool PuedeBuscarProducto()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(TextoBusqueda);
    }

    private bool PuedeConsultarPrecio()
    {
        return !IsBusy && (ProductoSeleccionado is not null || !string.IsNullOrWhiteSpace(TextoBusqueda));
    }

    private bool PuedeIngresarPeso()
    {
        return !IsBusy && SistemaBalanzaActivo && ProductoSeleccionado?.VentaPorPeso == true;
    }

    private bool PuedeProcesarVenta()
    {
        return !IsBusy && Carrito.Count > 0;
    }

    private async Task BuscarProductoAsync()
    {
        var criterio = TextoBusqueda.Trim();
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var producto = await _productoService
                .BuscarPorTextoOCodigoAsync(criterio, cancellationToken);

            if (producto is null)
            {
                ProductoSeleccionado = null;
                MensajeEstado = $"No se encontro producto para '{criterio}'.";
                return;
            }

            ProductoSeleccionado = producto;
            await AgregarProductoAlCarritoAsync(producto, cancellationToken);
            TextoBusqueda = string.Empty;
        });
    }

    private async Task ProcesarCodigoBarrasAsync(string? codigoBarras)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
        {
            return;
        }

        var codigoNormalizado = codigoBarras.Trim();
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var producto = await _productoService
                .BuscarPorCodigoBarrasAsync(codigoNormalizado, cancellationToken);

            if (producto is null)
            {
                ProductoSeleccionado = null;
                MensajeEstado = $"Codigo no encontrado: {codigoNormalizado}.";
                return;
            }

            ProductoSeleccionado = producto;
            await AgregarProductoAlCarritoAsync(producto, cancellationToken);
            TextoBusqueda = string.Empty;
        });
    }

    private async Task ConsultarPrecioAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var producto = ProductoSeleccionado;

            if (producto is null && !string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                producto = await _productoService
                    .BuscarPorTextoOCodigoAsync(TextoBusqueda.Trim(), cancellationToken);
            }

            if (producto is null)
            {
                MensajeEstado = "Selecciona o busca un producto para consultar precio.";
                return;
            }

            ProductoSeleccionado = producto;
            MensajeEstado = $"{producto.Nombre}: {producto.PrecioVenta:C}. Stock: {producto.StockActual:0.##} {producto.UnidadMedida}.";
        });
    }

    private async Task IngresarPesoAsync()
    {
        if (ProductoSeleccionado is null)
        {
            MensajeEstado = "Selecciona un producto por peso antes de ingresar gramos.";
            return;
        }

        await EjecutarOperacionAsync(
            cancellationToken => AgregarProductoPorPesoAsync(ProductoSeleccionado, cancellationToken));
    }

    private async Task AgregarMontoAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var monto = await _dialogService
                .SolicitarMontoLibreAsync(cancellationToken);

            if (monto is null or <= 0m)
            {
                return;
            }

            Carrito.Add(new CarritoVentaItemViewModel
            {
                ProductoId = SystemProductIds.ManualAmountProductId,
                Codigo = SystemProductIds.ManualAmountCode,
                CodigoBarras = null,
                Nombre = SystemProductIds.ManualAmountName,
                UnidadMedida = "UN",
                StockActual = 999999999m,
                Cantidad = 1m,
                PrecioUnitario = monto.Value
            });

            MensajeEstado = $"Monto manual agregado: {monto.Value:C}.";
        });
    }

    private async Task GestionarExtrasAsync()
    {
        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var request = new ExtrasVentaRequest(Subtotal, DescuentoTotal, RecargoTotal);
            var result = await _dialogService
                .GestionarExtrasAsync(request, cancellationToken);

            if (result is null)
            {
                return;
            }

            DescuentoTotal = result.Descuento;
            RecargoTotal = result.Recargo;

            var signo = result.TipoAjuste == TipoAjusteVenta.Recargo ? "+" : "-";
            MensajeEstado = $"Ajuste aplicado: {signo}{result.Valor:0.##}. Total actualizado.";
        });
    }

    private async Task ProcesarVentaAsync()
    {
        if (Carrito.Count == 0)
        {
            MensajeEstado = "Agrega productos antes de procesar la venta.";
            return;
        }

        await EjecutarOperacionAsync(async cancellationToken =>
        {
            var checkoutRequest = new CheckoutVentaRequest(
                ClienteSeleccionado,
                Subtotal,
                DescuentoTotal,
                RecargoTotal,
                Total,
                Carrito.Select(item => new CheckoutVentaItemDto(
                    item.NombreMostrado,
                    item.CantidadTexto,
                    item.TotalLinea)).ToList());

            var checkout = await _dialogService
                .ProcesarCheckoutAsync(checkoutRequest, cancellationToken);

            if (checkout is null)
            {
                return;
            }

            var estado = checkout.MetodoPago == MetodoPagoVenta.Transferencia
                ? "RequiereValidacion"
                : "Completada";

            var ventaRequest = new RegistrarVentaRequest(
                ClienteSeleccionado,
                DateTime.Now,
                Subtotal,
                DescuentoTotal,
                RecargoTotal,
                0m,
                Total,
                checkout.MetodoPago,
                estado,
                checkout.MontoRecibido,
                checkout.MetodoPago == MetodoPagoVenta.Efectivo
                    ? Math.Max(0m, checkout.MontoRecibido - Total)
                    : 0m,
                Carrito.Select(item => new RegistrarVentaItemRequest(
                    item.ProductoId,
                    item.ComboId,
                    item.Codigo,
                    item.CodigoBarras,
                    item.NombreMostrado,
                    item.Cantidad,
                    item.PrecioUnitario,
                    item.Descuento,
                    item.Impuesto,
                    item.TotalLinea)).ToList());

            var ventaId = await _ventaService
                .RegistrarVentaAsync(ventaRequest, cancellationToken);

            LimpiarVentaActual();

            MensajeEstado = checkout.MetodoPago == MetodoPagoVenta.Transferencia
                ? $"Venta #{ventaId} registrada como transferencia pendiente de validacion."
                : $"Venta #{ventaId} completada correctamente.";

            if (checkout.MetodoPago == MetodoPagoVenta.Transferencia)
            {
                await CargarTransferenciasPendientesAsync(cancellationToken);
            }
        });
    }

    private async Task ProcesarDobleEnterAsync()
    {
        var ahora = DateTime.UtcNow;

        if (ahora - _ultimoEnterUtc <= DoubleEnterWindow)
        {
            _ultimoEnterUtc = DateTime.MinValue;
            await ProcesarVentaAsync();
            return;
        }

        _ultimoEnterUtc = ahora;
        MensajeEstado = "Presiona Enter otra vez para procesar la venta.";
    }

    private async Task MostrarTransferenciasAsync()
    {
        SeccionActiva = SeccionTransferencias;
        await EjecutarOperacionAsync(CargarTransferenciasPendientesAsync);
    }

    private async Task AprobarTransferenciaAsync(TransferenciaPendienteItemViewModel? transferencia)
    {
        if (transferencia is null)
        {
            return;
        }

        await EjecutarOperacionAsync(async cancellationToken =>
        {
            await _ventaService
                .AprobarTransferenciaAsync(transferencia.VentaId, cancellationToken);

            await CargarTransferenciasPendientesAsync(cancellationToken);
            MensajeEstado = $"Transferencia #{transferencia.Numero} aprobada.";
        });
    }

    private async Task AgregarProductoAlCarritoAsync(
        ProductoVentaDto producto,
        CancellationToken cancellationToken)
    {
        if (producto.VentaPorPeso)
        {
            await AgregarProductoPorPesoAsync(producto, cancellationToken);
            return;
        }

        int? productoId = producto.EsCombo ? null : producto.Id;

        var itemExistente = Carrito.FirstOrDefault(item =>
            item.ProductoId == productoId
            && item.ComboId == producto.ComboId
            && !item.EsPorPeso);

        if (itemExistente is not null)
        {
            if (itemExistente.Cantidad + 1m > producto.StockActual)
            {
                MensajeEstado = $"Stock insuficiente para {producto.Nombre}.";
                return;
            }

            itemExistente.Incrementar();
            MensajeEstado = $"{producto.Nombre} sumo 1 unidad al carrito.";
            return;
        }

        if (producto.StockActual < 1m)
        {
            MensajeEstado = $"Sin stock disponible para {producto.Nombre}.";
            return;
        }

        Carrito.Add(new CarritoVentaItemViewModel
        {
            ProductoId = productoId,
            ComboId = producto.ComboId,
            EsCombo = producto.EsCombo,
            Codigo = producto.Codigo,
            CodigoBarras = producto.CodigoBarras,
            Nombre = producto.Nombre,
            UnidadMedida = producto.UnidadMedida,
            StockActual = producto.StockActual,
            Cantidad = 1m,
            PrecioUnitario = producto.PrecioVenta
        });

        MensajeEstado = $"{producto.Nombre} agregado al carrito.";
    }

    private async Task AgregarProductoPorPesoAsync(
        ProductoVentaDto producto,
        CancellationToken cancellationToken)
    {
        if (!SistemaBalanzaActivo)
        {
            MensajeEstado = "El sistema de balanza esta desactivado en Configuracion > Sistema.";
            return;
        }

        if (!producto.VentaPorPeso)
        {
            MensajeEstado = $"{producto.Nombre} no esta configurado para venta por peso.";
            return;
        }

        var pesoBase = producto.PesoBaseGramos <= 0m ? 1000m : producto.PesoBaseGramos;
        var result = await _dialogService
            .SolicitarPesoAsync(
                new PesoProductoRequest(producto.Id, producto.Nombre, producto.PrecioVenta, pesoBase),
                cancellationToken);

        if (result is null)
        {
            return;
        }

        if (result.PesoGramos <= 0m)
        {
            MensajeEstado = "El peso ingresado debe ser mayor a cero.";
            return;
        }

        var cantidadBase = result.PesoGramos / pesoBase;

        Carrito.Add(new CarritoVentaItemViewModel
        {
            ProductoId = producto.EsCombo ? null : producto.Id,
            ComboId = producto.ComboId,
            EsCombo = producto.EsCombo,
            Codigo = producto.Codigo,
            CodigoBarras = producto.CodigoBarras,
            Nombre = producto.Nombre,
            UnidadMedida = producto.UnidadMedida,
            StockActual = producto.StockActual,
            EsPorPeso = true,
            PesoGramos = result.PesoGramos,
            Cantidad = cantidadBase,
            PrecioUnitario = producto.PrecioVenta
        });

        MensajeEstado = $"{producto.Nombre} agregado por peso: {result.PesoGramos:0.##} g.";
    }

    private void IncrementarCantidad(CarritoVentaItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.Cantidad + 1m > item.StockActual)
        {
            MensajeEstado = $"Stock insuficiente para {item.Nombre}.";
            return;
        }

        item.Incrementar();
    }

    private void DecrementarCantidad(CarritoVentaItemViewModel? item)
    {
        item?.Decrementar();
    }

    private void EliminarItem(CarritoVentaItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        Carrito.Remove(item);
        MensajeEstado = $"{item.NombreMostrado} eliminado del carrito.";
    }

    private async Task CargarTransferenciasPendientesAsync(CancellationToken cancellationToken)
    {
        var pendientes = await _ventaService
            .ObtenerTransferenciasPendientesAsync(cancellationToken);

        TransferenciasPendientes.Clear();
        foreach (var pendiente in pendientes)
        {
            TransferenciasPendientes.Add(new TransferenciaPendienteItemViewModel(pendiente));
        }

        OnPropertyChanged(nameof(CantidadTransferenciasPendientes));
    }

    private void LimpiarVentaActual()
    {
        Carrito.Clear();
        ProductoSeleccionado = null;
        TextoBusqueda = string.Empty;
        DescuentoTotal = 0m;
        RecargoTotal = 0m;
        ClienteSeleccionado = "Consumidor Final";
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
            MensajeEstado = "Ocurrio un error al procesar la operacion.";
            await _dialogService
                .MostrarMensajeAsync("Error", ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
            NotifyCommandsCanExecuteChanged();
        }
    }

    private void OnCarritoCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (CarritoVentaItemViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnCarritoItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (CarritoVentaItemViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnCarritoItemPropertyChanged;
            }
        }

        NotifySaleTotalsChanged();
    }

    private void OnCarritoItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CarritoVentaItemViewModel.TotalLinea)
            or nameof(CarritoVentaItemViewModel.Cantidad)
            or nameof(CarritoVentaItemViewModel.PrecioUnitario)
            or nameof(CarritoVentaItemViewModel.Descuento)
            or nameof(CarritoVentaItemViewModel.Impuesto))
        {
            NotifySaleTotalsChanged();
        }
    }

    private void NotifySaleTotalsChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TotalItemsTexto));
        OnPropertyChanged(nameof(CarritoVacio));
        NotifyCommandsCanExecuteChanged();
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        BuscarProductoCommand.NotifyCanExecuteChanged();
        ConsultarPrecioCommand.NotifyCanExecuteChanged();
        IngresarPesoCommand.NotifyCanExecuteChanged();
        GestionarExtrasCommand.NotifyCanExecuteChanged();
        ProcesarVentaCommand.NotifyCanExecuteChanged();
    }

    private void BuildSettingsItems()
    {
        var settings = _settingsService.Settings;

        GeneralSettings.Add(new SettingToggleItemViewModel(
            "Habilitar sistema de caja",
            "Activa el control de sesiones de caja y registro de transacciones en efectivo.",
            "\uE7F4",
            settings.HabilitarSistemaCaja,
            value => SaveSetting(s => s.HabilitarSistemaCaja = value)));

        GeneralSettings.Add(new SettingToggleItemViewModel(
            "Guardado de \u00EDtems removidos",
            "Cuando est\u00E1 activo, se pedir\u00E1 un motivo cada vez que se elimine un producto del carrito POS.",
            "\uE8B7",
            settings.GuardadoItemsRemovidos,
            value => SaveSetting(s => s.GuardadoItemsRemovidos = value)));

        GeneralSettings.Add(new SettingToggleItemViewModel(
            "Productos con im\u00E1genes",
            "Activa un campo para subir im\u00E1genes, guardar ruta de archivo local o URL dentro de cada producto.",
            "\uEB9F",
            settings.ProductosConImagenes,
            value => SaveSetting(s => s.ProductosConImagenes = value)));

        GeneralSettings.Add(new SettingToggleItemViewModel(
            "C\u00E1lculo de precio por costo",
            "Muestra el bot\u00F3n para calcular precio final seg\u00FAn costo y margen de ganancia.",
            "\uE8EF",
            settings.CalculoPrecioPorCosto,
            value => SaveSetting(s => s.CalculoPrecioPorCosto = value)));

        GeneralSettings.Add(new SettingToggleItemViewModel(
            "Configurar impresi\u00F3n",
            "Si est\u00E1 activo, se desactiva el cierre autom\u00E1tico de la ventana de impresi\u00F3n a los 5 segundos.",
            "\uE749",
            settings.ConfigurarImpresion,
            value => SaveSetting(s => s.ConfigurarImpresion = value)));

        GeneralSettings.Add(new SettingToggleItemViewModel(
            "Redondeo de precios por peso/volumen",
            "Redondea el subtotal de productos por kilo, litro, gramo o mililitro al m\u00FAltiplo de $50 m\u00E1s cercano.",
            "\uE9D2",
            settings.RedondeoPreciosPesoVolumen,
            value => SaveSetting(s => s.RedondeoPreciosPesoVolumen = value)));

        PlanProSettings.Add(new SettingToggleItemViewModel(
            "Cat\u00E1logo online activo",
            "Hace visible un cat\u00E1logo p\u00FAblico para exportaci\u00F3n o sincronizaci\u00F3n.",
            "\uE774",
            settings.CatalogoOnlineActivo,
            value => SaveSetting(s => s.CatalogoOnlineActivo = value)));

        PlanProSettings.Add(new SettingToggleItemViewModel(
            "Listas de precios",
            "Habilita la gesti\u00F3n de m\u00FAltiples listas de precio para vender a distintos tipos de clientes.",
            "\uEA37",
            settings.ListasDePrecios,
            value => SaveSetting(s => s.ListasDePrecios = value)));

        PlanProSettings.Add(new SettingToggleItemViewModel(
            "Sistema de balanza",
            "Habilita el bot\u00F3n Ingresar Peso en el POS para cargar manualmente gramos o kilos.",
            "\uE9D2",
            settings.SistemaBalanza,
            value => SaveSetting(s => s.SistemaBalanza = value, affectsScaleSystem: true)));

        PlanIaSettings.Add(new SettingToggleItemViewModel(
            "M\u00F3dulo IA",
            "Sugerencias de precios, escaneo de facturas, an\u00E1lisis de mercado y detecci\u00F3n de baja rotaci\u00F3n.",
            "\uECAD",
            settings.ModuloIa,
            value => SaveSetting(s => s.ModuloIa = value)));
    }

    private void SaveSetting(Action<AppSettings> update, bool affectsScaleSystem = false)
    {
        update(_settingsService.Settings);
        _settingsService.Save();

        if (affectsScaleSystem)
        {
            OnPropertyChanged(nameof(SistemaBalanzaActivo));
            IngresarPesoCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SistemaBalanzaActivo));
        IngresarPesoCommand.NotifyCanExecuteChanged();
    }
}
