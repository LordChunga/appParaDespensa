#nullable enable

namespace PosLocal.Services;

public interface IReportesService
{
    Task<DashboardReportesDto> ObtenerDashboardAsync(
        ReportesPeriodoRequest request,
        CancellationToken cancellationToken = default);

    Task ExportarDashboardCsvAsync(
        ReportesPeriodoRequest request,
        string rutaDestino,
        CancellationToken cancellationToken = default);
}
