#nullable enable

namespace PosLocal.Services;

public interface IReportesDialogService
{
    Task<string?> SolicitarRutaExportacionAsync(CancellationToken cancellationToken = default);

    Task MostrarMensajeAsync(
        string titulo,
        string mensaje,
        CancellationToken cancellationToken = default);
}
