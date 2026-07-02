#nullable enable

using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed class RankingReporteItemViewModel
{
    public RankingReporteItemViewModel(RankingProductoReporteDto dto)
    {
        Producto = dto.Producto;
        Cantidad = dto.Cantidad;
        Total = dto.Total;
        Ganancia = dto.Ganancia;
    }

    public string Producto { get; }

    public decimal Cantidad { get; }

    public decimal Total { get; }

    public decimal Ganancia { get; }

    public string CantidadTexto => $"{Cantidad:0.##} u";
}
