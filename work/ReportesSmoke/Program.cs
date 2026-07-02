using System.IO;
using PosLocal.Data;
using PosLocal.Services;

var databasePath = Path.Combine(
    Path.GetTempPath(),
    $"poslocal-reportes-smoke-{Guid.NewGuid():N}.db");

try
{
    var dbContext = new PosDbContext(databasePath);
    await dbContext.InitializeAsync();

    var reportesService = new ReportesService(dbContext);
    var dashboard = await reportesService.ObtenerDashboardAsync(
        new ReportesPeriodoRequest(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 30)));

    Console.WriteLine($"Ventas={dashboard.Kpis.TotalVentas}; Ingresos={dashboard.Kpis.IngresosPeriodo}; Series={dashboard.SeriesDiarias.Count}");
}
finally
{
    TryDelete(databasePath);
    TryDelete(databasePath + "-wal");
    TryDelete(databasePath + "-shm");
}

static void TryDelete(string path)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}
