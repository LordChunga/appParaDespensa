#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class ProductoManualViewModel : ObservableObject
{
    private readonly IProductoCatalogoService _productoService;
    private readonly IProductoCatalogoDialogService _dialogService;
    private readonly ProductoUpsertRequest? _productoActual;

    public ProductoManualViewModel(
        IProductoCatalogoService productoService,
        IProductoCatalogoDialogService dialogService,
        ProductoUpsertRequest? productoActual = null)
    {
        _productoService = productoService;
        _dialogService = dialogService;
        _productoActual = productoActual;

        Categorias = new ObservableCollection<CategoriaCatalogoDto>();
        Proveedores = new ObservableCollection<ProveedorCatalogoDto>();

        InicializarCommand = new AsyncRelayCommand(InicializarAsync);
        GuardarCommand = new RelayCommand(Guardar, PuedeGuardar);
        CancelarCommand = new RelayCommand(Cancelar);
    }

    public ObservableCollection<CategoriaCatalogoDto> Categorias { get; }

    public ObservableCollection<ProveedorCatalogoDto> Proveedores { get; }

    public int? ProductoId => _productoActual?.Id;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string _codigo = string.Empty;

    [ObservableProperty]
    private string? _codigoBarras;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string? _descripcion;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private CategoriaCatalogoDto? _categoriaSeleccionada;

    [ObservableProperty]
    private ProveedorCatalogoDto? _proveedorSeleccionado;

    [ObservableProperty]
    private decimal _stockActual;

    [ObservableProperty]
    private decimal _stockMinimo;

    [ObservableProperty]
    private decimal _stockMaximo = 100m;

    [ObservableProperty]
    private string _unidadMedida = "UNITARIO";

    [ObservableProperty]
    private decimal _precioCosto;

    [ObservableProperty]
    private decimal _precioVenta;

    [ObservableProperty]
    private bool _ventaPorPeso;

    [ObservableProperty]
    private decimal _pesoBaseGramos = 1000m;

    [ObservableProperty]
    private bool _activo = true;

    [ObservableProperty]
    private ProductoUpsertRequest? _resultado;

    [ObservableProperty]
    private bool _fueGuardado;

    [ObservableProperty]
    private bool _debeCerrar;

    public IAsyncRelayCommand InicializarCommand { get; }

    public IRelayCommand GuardarCommand { get; }

    public IRelayCommand CancelarCommand { get; }

    private async Task InicializarAsync()
    {
        try
        {
            var categorias = await _productoService.ObtenerCategoriasAsync();
            Categorias.Clear();
            foreach (var categoria in categorias)
            {
                Categorias.Add(categoria);
            }

            var proveedores = await _productoService.ObtenerProveedoresAsync();
            Proveedores.Clear();
            foreach (var proveedor in proveedores)
            {
                Proveedores.Add(proveedor);
            }

            if (_productoActual is not null)
            {
                Codigo = _productoActual.Codigo;
                CodigoBarras = _productoActual.CodigoBarras;
                Nombre = _productoActual.Nombre;
                Descripcion = _productoActual.Descripcion;
                CategoriaSeleccionada = Categorias.FirstOrDefault(x => x.Id == _productoActual.CategoriaId);
                ProveedorSeleccionado = Proveedores.FirstOrDefault(x => x.Id == _productoActual.ProveedorId);
                StockActual = _productoActual.StockActual;
                StockMinimo = _productoActual.StockMinimo;
                StockMaximo = _productoActual.StockMaximo;
                UnidadMedida = _productoActual.UnidadMedida;
                PrecioCosto = _productoActual.PrecioCosto;
                PrecioVenta = _productoActual.PrecioVenta;
                VentaPorPeso = _productoActual.VentaPorPeso;
                PesoBaseGramos = _productoActual.PesoBaseGramos;
                Activo = _productoActual.Activo;
            }
        }
        catch (Exception ex)
        {
            await _dialogService.MostrarMensajeAsync("Error", ex.Message);
        }
    }

    private bool PuedeGuardar()
    {
        return !string.IsNullOrWhiteSpace(Codigo)
            && !string.IsNullOrWhiteSpace(Nombre)
            && CategoriaSeleccionada is not null;
    }

    private void Guardar()
    {
        Resultado = new ProductoUpsertRequest(
            ProductoId,
            Codigo.Trim(),
            string.IsNullOrWhiteSpace(CodigoBarras) ? null : CodigoBarras.Trim(),
            Nombre.Trim(),
            Descripcion,
            CategoriaSeleccionada!.Id,
            ProveedorSeleccionado?.Id,
            StockActual,
            StockMinimo,
            StockMaximo <= 0m ? 100m : StockMaximo,
            string.IsNullOrWhiteSpace(UnidadMedida) ? "UNITARIO" : UnidadMedida.Trim(),
            PrecioCosto,
            PrecioVenta,
            VentaPorPeso,
            PesoBaseGramos <= 0m ? 1000m : PesoBaseGramos,
            Activo);

        FueGuardado = true;
        DebeCerrar = true;
    }

    private void Cancelar()
    {
        Resultado = null;
        FueGuardado = false;
        DebeCerrar = true;
    }
}
