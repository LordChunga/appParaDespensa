#nullable enable

using System.Windows;

namespace PosLocal.Views;

public partial class PuntoVentaView : Window
{
    public PuntoVentaView()
    {
        InitializeComponent();
    }

    private void AbrirReportes_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.NavigateTo<ReportesView>(this);
        }
    }
}
