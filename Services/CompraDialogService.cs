#nullable enable

using System.Windows;
using Microsoft.Win32;

namespace PosLocal.Services;

public sealed class CompraDialogService : ICompraDialogService
{
    public Task AbrirNuevaCompraAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MessageBox.Show(
            "El formulario de alta de compra quedo reservado para el siguiente bloque de desarrollo.",
            "Nueva compra",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        return Task.CompletedTask;
    }

    public Task AbrirMovimientosInventarioAsync(
        int productoId,
        string productoNombre,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MessageBox.Show(
            $"Producto #{productoId}: {productoNombre}",
            "Movimientos de Inventario",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        return Task.CompletedTask;
    }

    public Task<string?> SolicitarRutaExportacionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new SaveFileDialog
        {
            Title = "Exportar historial de compras",
            Filter = "CSV compatible con Excel (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,
            FileName = $"compras-{DateTime.Now:yyyyMMdd-HHmm}.csv"
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
