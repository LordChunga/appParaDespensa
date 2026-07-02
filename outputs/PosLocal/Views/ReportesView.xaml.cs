using System.Windows;

namespace PosLocal.Views;

public partial class ReportesView : Window
{
    public ReportesView()
    {
        InitializeComponent();
    }

    private void AbrirPuntoVenta_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.NavigateTo<PuntoVentaView>(this);
        }
    }

    private void AbrirCompras_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.NavigateTo<ComprasView>(this);
        }
    }

    private void AbrirProductos_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.NavigateTo<ProductosView>(this);
        }
    }

    private void AbrirInventario_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.NavigateTo<InventarioProductosView>(this);
        }
    }
}
