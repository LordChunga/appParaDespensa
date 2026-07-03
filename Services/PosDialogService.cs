#nullable enable

using System.Windows;
using PosLocal.ViewModels;
using PosLocal.Views;

namespace PosLocal.Services;

public sealed class PosDialogService : IPosDialogService
{
    public Task<PesoProductoResult?> SolicitarPesoAsync(
        PesoProductoRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new IngresoPesoViewModel(request);
        var window = new IngresoPesoDialog
        {
            DataContext = viewModel,
            Owner = GetActiveWindow()
        };

        window.ShowDialog();
        return Task.FromResult(viewModel.FueAplicado ? viewModel.Resultado : null);
    }

    public Task<decimal?> SolicitarMontoLibreAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new MontoLibreViewModel();
        var window = new MontoLibreDialog
        {
            DataContext = viewModel,
            Owner = GetActiveWindow()
        };

        window.ShowDialog();
        return Task.FromResult(viewModel.FueAplicado ? viewModel.Resultado : null);
    }

    public Task<ExtrasVentaResult?> GestionarExtrasAsync(
        ExtrasVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new GestionExtrasViewModel(request);
        var window = new GestionExtrasDialog
        {
            DataContext = viewModel,
            Owner = GetActiveWindow()
        };

        window.ShowDialog();
        return Task.FromResult(viewModel.FueAplicado ? viewModel.Resultado : null);
    }

    public Task<CheckoutVentaResult?> ProcesarCheckoutAsync(
        CheckoutVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new CheckoutVentaViewModel(request)
        {
            MontoRecibido = request.Total
        };

        var window = new CheckoutVentaDialog
        {
            DataContext = viewModel,
            Owner = GetActiveWindow()
        };

        window.ShowDialog();
        return Task.FromResult(viewModel.FueConfirmado ? viewModel.Resultado : null);
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
