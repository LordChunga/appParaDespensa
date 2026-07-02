#nullable enable

using System.Windows;
using Microsoft.Win32;
using PosLocal.ViewModels;
using PosLocal.Views;

namespace PosLocal.Services;

public sealed class ProductoCatalogoDialogService : IProductoCatalogoDialogService
{
    private readonly IProductoCatalogoService _productoService;

    public ProductoCatalogoDialogService(IProductoCatalogoService productoService)
    {
        _productoService = productoService;
    }

    public Task<ProductoUpsertRequest?> AbrirProductoManualAsync(
        ProductoUpsertRequest? productoActual,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new ProductoManualViewModel(_productoService, this, productoActual);
        var window = new ProductoManualDialog
        {
            DataContext = viewModel,
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        window.ShowDialog();
        return Task.FromResult(viewModel.FueGuardado ? viewModel.Resultado : null);
    }

    public Task AbrirGestionCategoriasAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = new GestionCategoriasDialog
        {
            DataContext = new GestionCategoriasViewModel(_productoService, this),
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        window.ShowDialog();
        return Task.CompletedTask;
    }

    public Task AbrirGestionProveedoresAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = new GestionProveedoresDialog
        {
            DataContext = new GestionProveedoresViewModel(_productoService, this),
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        window.ShowDialog();
        return Task.CompletedTask;
    }

    public Task AbrirNuevoComboAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = new NuevoComboDialog
        {
            DataContext = new NuevoComboViewModel(_productoService, this),
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        window.ShowDialog();
        return Task.CompletedTask;
    }

    public Task<string?> SolicitarRutaExportacionCsvAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new SaveFileDialog
        {
            Title = "Exportar productos",
            Filter = "CSV compatible con Excel (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,
            FileName = $"productos-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
    }

    public Task<string?> SolicitarRutaImportacionCsvAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new OpenFileDialog
        {
            Title = "Importar productos",
            Filter = "CSV compatible con Excel (*.csv)|*.csv",
            DefaultExt = ".csv",
            Multiselect = false,
            CheckFileExists = true
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
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
}
