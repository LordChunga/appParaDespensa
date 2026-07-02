#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed class CompraDetalleStockViewModel : ObservableObject
{
    public CompraDetalleStockViewModel(CompraDetalleStockDto dto)
    {
        CompraDetalleId = dto.CompraDetalleId;
        CompraId = dto.CompraId;
        CompraNumero = dto.CompraNumero;
        ProductoId = dto.ProductoId;
        ProductoNombre = dto.ProductoNombre;
        CodigoBarras = dto.CodigoBarras;
        Fecha = dto.Fecha;
        Unidad = dto.Unidad;
        CostoUnitario = dto.CostoUnitario;
        Cantidad = dto.Cantidad;
        Usuario = dto.Usuario;
        ProveedorNombre = dto.ProveedorNombre;
        ProveedorContacto = dto.ProveedorContacto;
        ProveedorTelefono = dto.ProveedorTelefono;
        UltimoPedidoTexto = dto.UltimoPedidoTexto;
        StockActual = dto.StockActual;
        StockMaximo = dto.StockMaximo <= 0m ? 1m : dto.StockMaximo;
        CantidadCritica = dto.CantidadCritica;
        ProximaEntregaEstimada = dto.ProximaEntregaEstimada;
    }

    public int CompraDetalleId { get; }

    public int CompraId { get; }

    public string CompraNumero { get; }

    public int ProductoId { get; }

    public string ProductoNombre { get; }

    public string CodigoBarras { get; }

    public DateTime Fecha { get; }

    public string Unidad { get; }

    public decimal CostoUnitario { get; }

    public decimal Cantidad { get; }

    public string Usuario { get; }

    public string ProveedorNombre { get; }

    public string ProveedorContacto { get; }

    public string ProveedorTelefono { get; }

    public string UltimoPedidoTexto { get; }

    public decimal StockActual { get; }

    public decimal StockMaximo { get; }

    public decimal CantidadCritica { get; }

    public DateTime? ProximaEntregaEstimada { get; }

    public decimal Subtotal => CostoUnitario * Cantidad;

    public bool AlertaCriticaActiva => StockActual <= CantidadCritica;

    public double StockPorcentaje => (double)Math.Clamp((StockActual / StockMaximo) * 100m, 0m, 100m);

    public string StockResumenTexto => $"Stock Actual: {StockActual:0.##} / Stock Max: {StockMaximo:0.##}";

    public string CantidadAlmacenTexto => $"Cantidad en Almacen: {StockActual:0.##} {Unidad}";

    public string CantidadCriticaTexto => AlertaCriticaActiva
        ? $"Cantidad Critica: {CantidadCritica:0.##} (Alerta Activa)"
        : $"Cantidad Critica: {CantidadCritica:0.##}";

    public string ProximaEntregaTexto => ProximaEntregaEstimada is null
        ? "Proxima Entrega Estimada: No Programada"
        : $"Proxima Entrega Estimada: {ProximaEntregaEstimada.Value:dd/MM/yyyy}";

    public string ProveedorContactoTexto => $"{ProveedorContacto} ({ProveedorTelefono})";

    public string AuditoriaTexto => $"ID de Compra #{CompraNumero}";
}
