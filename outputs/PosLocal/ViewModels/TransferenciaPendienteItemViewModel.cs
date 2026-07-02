#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed class TransferenciaPendienteItemViewModel : ObservableObject
{
    public TransferenciaPendienteItemViewModel(TransferenciaPendienteDto dto)
    {
        VentaId = dto.VentaId;
        Numero = dto.Numero;
        Fecha = dto.Fecha;
        Cliente = dto.Cliente;
        Total = dto.Total;
        Estado = dto.Estado;
    }

    public int VentaId { get; }

    public string Numero { get; }

    public DateTime Fecha { get; }

    public string Cliente { get; }

    public decimal Total { get; }

    public string Estado { get; }
}
