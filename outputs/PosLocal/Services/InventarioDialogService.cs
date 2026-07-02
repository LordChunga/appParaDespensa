#nullable enable

using System.Windows;
using PosLocal.ViewModels;
using PosLocal.Views;

namespace PosLocal.Services;

public sealed class InventarioDialogService : IInventarioDialogService
{
    public Task<AjusteStockDialogResult?> SolicitarAjusteStockAsync(
        AjusteStockDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new AjusteStockViewModel(request);
        var window = new AjusteStockDialog
        {
            DataContext = viewModel,
            Owner = GetActiveWindow()
        };

        window.ShowDialog();
        return Task.FromResult(viewModel.FueAplicado ? viewModel.Resultado : null);
    }

    public Task AbrirMovimientosProductoAsync(
        int productoId,
        string productoNombre,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MessageBox.Show(
            $"Se abrira Movimientos filtrado por producto #{productoId}: {productoNombre}.",
            "Movimientos de Inventario",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        return Task.CompletedTask;
    }

    public Task MostrarMensajeAsync(
        string titulo,
        string mensaje,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    private static Window? GetActiveWindow()
    {
        return Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
    }
}
