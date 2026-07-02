#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;

namespace PosLocal.ViewModels;

public sealed class CarritoVentaItemViewModel : ObservableObject
{
    private decimal _cantidad;
    private decimal _precioUnitario;
    private decimal _descuento;
    private decimal _impuesto;

    public int? ProductoId { get; init; }

    public int? ComboId { get; init; }

    public bool EsCombo { get; init; }

    public string Codigo { get; init; } = string.Empty;

    public string? CodigoBarras { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string UnidadMedida { get; init; } = "UN";

    public decimal StockActual { get; init; }

    public bool EsPorPeso { get; init; }

    public decimal? PesoGramos { get; init; }

    public decimal Cantidad
    {
        get => _cantidad;
        set
        {
            if (SetProperty(ref _cantidad, value))
            {
                NotifyLineTotalsChanged();
            }
        }
    }

    public decimal PrecioUnitario
    {
        get => _precioUnitario;
        set
        {
            if (SetProperty(ref _precioUnitario, value))
            {
                NotifyLineTotalsChanged();
            }
        }
    }

    public decimal Descuento
    {
        get => _descuento;
        set
        {
            if (SetProperty(ref _descuento, value))
            {
                NotifyLineTotalsChanged();
            }
        }
    }

    public decimal Impuesto
    {
        get => _impuesto;
        set
        {
            if (SetProperty(ref _impuesto, value))
            {
                NotifyLineTotalsChanged();
            }
        }
    }

    public string NombreMostrado
    {
        get
        {
            if (!EsPorPeso || PesoGramos is null)
            {
                return Nombre;
            }

            var kilos = PesoGramos.Value / 1000m;
            return $"{Nombre} - {kilos:0.000} Kg";
        }
    }

    public string CodigoMostrado => string.IsNullOrWhiteSpace(CodigoBarras)
        ? Codigo
        : CodigoBarras;

    public string CantidadTexto => EsPorPeso && PesoGramos is not null
        ? $"{PesoGramos.Value / 1000m:0.000} Kg"
        : $"{Cantidad:0.##} {UnidadMedida.ToLowerInvariant()}";

    public string StockTexto => $"Stock: {StockActual:0.##} {UnidadMedida.ToLowerInvariant()}";

    public decimal TotalLinea => Math.Max(0m, (Cantidad * PrecioUnitario) - Descuento + Impuesto);

    public void Incrementar()
    {
        if (EsPorPeso)
        {
            return;
        }

        Cantidad += 1m;
    }

    public void Decrementar()
    {
        if (EsPorPeso)
        {
            return;
        }

        Cantidad = Math.Max(1m, Cantidad - 1m);
    }

    private void NotifyLineTotalsChanged()
    {
        OnPropertyChanged(nameof(CantidadTexto));
        OnPropertyChanged(nameof(TotalLinea));
    }
}
