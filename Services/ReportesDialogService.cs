#nullable enable

using System.Windows;
using Microsoft.Win32;

namespace PosLocal.Services;

public sealed class ReportesDialogService : IReportesDialogService
{
    public Task<string?> SolicitarRutaExportacionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new SaveFileDialog
        {
            Title = "Exportar reporte",
            Filter = "CSV compatible con Excel (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,
            FileName = $"reporte-despensa-isabel-{DateTime.Now:yyyyMMdd-HHmm}.csv"
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
