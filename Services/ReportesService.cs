#nullable enable

using System.Globalization;
using System.IO;
using System.Text;
using Dapper;
using PosLocal.Data;

namespace PosLocal.Services;

public sealed class ReportesService : IReportesService
{
    private readonly IPosDbContext _dbContext;

    public ReportesService(IPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardReportesDto> ObtenerDashboardAsync(
        ReportesPeriodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeRange(request);
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            Desde = range.FechaDesde,
            Hasta = range.FechaHasta.AddDays(1)
        };

        var kpis = await connection.QuerySingleAsync<KpiReportesDto>(new CommandDefinition(
            KpisSql,
            parameters,
            cancellationToken: cancellationToken));

        var pagoMasUsado = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            PagoMasUsadoSql,
            parameters,
            cancellationToken: cancellationToken)) ?? "N/A";

        var productoPopular = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            ProductoMasPopularSql,
            parameters,
            cancellationToken: cancellationToken)) ?? "Sin ventas";

        var series = (await connection.QueryAsync<SerieDiariaReporteDto>(new CommandDefinition(
            SeriesDiariasSql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        var metodosPago = (await connection.QueryAsync<MetodoPagoReporteDto>(new CommandDefinition(
            MetodosPagoSql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        var productosGanancia = (await connection.QueryAsync<RankingProductoReporteDto>(new CommandDefinition(
            RankingGananciaSql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        var productosVendidos = (await connection.QueryAsync<RankingProductoReporteDto>(new CommandDefinition(
            RankingVendidosSql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        return new DashboardReportesDto(
            kpis,
            new ResumenSecundarioReportesDto(
                pagoMasUsado,
                productoPopular,
                "Sin vencimientos",
                "Plan Gratis",
                "Vence 08 de jul de 2026"),
            series,
            metodosPago,
            productosGanancia,
            productosVendidos);
    }

    public async Task ExportarDashboardCsvAsync(
        ReportesPeriodoRequest request,
        string rutaDestino,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rutaDestino))
        {
            throw new ArgumentException("La ruta de exportacion es obligatoria.", nameof(rutaDestino));
        }

        var dashboard = await ObtenerDashboardAsync(request, cancellationToken);
        var directory = Path.GetDirectoryName(rutaDestino);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(rutaDestino, BuildCsv(request, dashboard), Encoding.UTF8, cancellationToken);
    }

    private static ReportesPeriodoRequest NormalizeRange(ReportesPeriodoRequest request)
    {
        var desde = request.FechaDesde.Date;
        var hasta = request.FechaHasta.Date;

        return desde <= hasta
            ? new ReportesPeriodoRequest(desde, hasta)
            : new ReportesPeriodoRequest(hasta, desde);
    }

    private static string BuildCsv(ReportesPeriodoRequest request, DashboardReportesDto dashboard)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Reporte de Despensa Isabel");
        builder.AppendLine($"Desde,{request.FechaDesde:yyyy-MM-dd}");
        builder.AppendLine($"Hasta,{request.FechaHasta:yyyy-MM-dd}");
        builder.AppendLine();
        builder.AppendLine("KPI,Valor");
        builder.AppendLine($"Ingresos,{dashboard.Kpis.IngresosPeriodo.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Ganancia estimada,{dashboard.Kpis.GananciaEstimada.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Total ventas,{dashboard.Kpis.TotalVentas}");
        builder.AppendLine($"Ticket promedio,{dashboard.Kpis.TicketPromedio.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine();
        builder.AppendLine("Fecha,Ingresos,Ganancia");

        foreach (var item in dashboard.SeriesDiarias)
        {
            builder.AppendLine($"{item.Fecha:yyyy-MM-dd},{item.Ingresos.ToString(CultureInfo.InvariantCulture)},{item.Ganancia.ToString(CultureInfo.InvariantCulture)}");
        }

        builder.AppendLine();
        builder.AppendLine("MetodoPago,Total,CantidadVentas");
        foreach (var item in dashboard.MetodosPago)
        {
            builder.AppendLine($"{EscapeCsv(item.MetodoPago)},{item.Total.ToString(CultureInfo.InvariantCulture)},{item.CantidadVentas}");
        }

        builder.AppendLine();
        builder.AppendLine("Producto,Cantidad,Total,Ganancia");
        foreach (var item in dashboard.ProductosMasVendidos)
        {
            builder.AppendLine($"{EscapeCsv(item.Producto)},{item.Cantidad.ToString(CultureInfo.InvariantCulture)},{item.Total.ToString(CultureInfo.InvariantCulture)},{item.Ganancia.ToString(CultureInfo.InvariantCulture)}");
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private const string VentasPeriodoWhereSql = @"
WHERE v.Fecha >= @Desde
AND v.Fecha < @Hasta
AND v.Estado <> 'Anulada'";

    private const string LineProfitCteSql = @"
WITH Lineas AS (
    SELECT
        dv.Id,
        dv.VentaId,
        dv.ProductoNombre,
        dv.Cantidad,
        dv.TotalLinea,
        CASE
            WHEN dv.ProductoId IS NOT NULL THEN COALESCE(p.PrecioCosto, 0) * dv.Cantidad
            WHEN dv.ComboId IS NOT NULL THEN COALESCE((
                SELECT SUM(p2.PrecioCosto * cd.Cantidad * dv.Cantidad)
                FROM ComboDetalles cd
                INNER JOIN Productos p2 ON p2.Id = cd.ProductoId
                WHERE cd.ComboId = dv.ComboId
            ), 0)
            ELSE 0
        END AS CostoLinea
    FROM DetallesVenta dv
    LEFT JOIN Productos p ON p.Id = dv.ProductoId
),
VentaLineas AS (
    SELECT
        v.Id AS VentaId,
        v.Fecha,
        v.Total,
        v.Descuento,
        v.Recargo,
        v.MetodoPago,
        l.ProductoNombre,
        l.Cantidad,
        l.TotalLinea,
        l.CostoLinea,
        l.TotalLinea - l.CostoLinea AS GananciaLinea
    FROM Ventas v
    INNER JOIN Lineas l ON l.VentaId = v.Id
    " + VentasPeriodoWhereSql + @"
)";

    private const string KpisSql = @"
" + LineProfitCteSql + @",
VentasAgregadas AS (
    SELECT
        COALESCE(SUM(Total), 0) AS IngresosPeriodo,
        COUNT(1) AS TotalVentas,
        COALESCE(AVG(Total), 0) AS TicketPromedio,
        COALESCE(SUM(Descuento), 0) AS Descuentos,
        COALESCE(SUM(Recargo), 0) AS Recargos
    FROM Ventas v
    " + VentasPeriodoWhereSql + @"
),
GananciaAgregada AS (
    SELECT COALESCE(SUM(GananciaLinea), 0) AS GananciaLineas
    FROM VentaLineas
)
SELECT
    va.IngresosPeriodo,
    ga.GananciaLineas - va.Descuentos + va.Recargos AS GananciaEstimada,
    va.TotalVentas,
    va.TicketPromedio
FROM VentasAgregadas va
CROSS JOIN GananciaAgregada ga;";

    private const string PagoMasUsadoSql = @"
SELECT MetodoPago
FROM Ventas v
" + VentasPeriodoWhereSql + @"
GROUP BY MetodoPago
ORDER BY COUNT(1) DESC, SUM(Total) DESC
LIMIT 1;";

    private const string ProductoMasPopularSql = @"
SELECT dv.ProductoNombre
FROM DetallesVenta dv
INNER JOIN Ventas v ON v.Id = dv.VentaId
" + VentasPeriodoWhereSql + @"
GROUP BY dv.ProductoNombre
ORDER BY SUM(dv.Cantidad) DESC
LIMIT 1;";

    private const string SeriesDiariasSql = @"
" + LineProfitCteSql + @"
SELECT
    date(v.Fecha) AS Fecha,
    COALESCE(SUM(v.Total), 0) AS Ingresos,
    COALESCE((
        SELECT SUM(vl.GananciaLinea)
        FROM VentaLineas vl
        WHERE date(vl.Fecha) = date(v.Fecha)
    ), 0)
    - COALESCE(SUM(v.Descuento), 0)
    + COALESCE(SUM(v.Recargo), 0) AS Ganancia
FROM Ventas v
" + VentasPeriodoWhereSql + @"
GROUP BY date(v.Fecha)
ORDER BY date(v.Fecha);";

    private const string MetodosPagoSql = @"
SELECT
    MetodoPago,
    COALESCE(SUM(Total), 0) AS Total,
    COUNT(1) AS CantidadVentas
FROM Ventas v
" + VentasPeriodoWhereSql + @"
GROUP BY MetodoPago
ORDER BY Total DESC;";

    private const string RankingGananciaSql = @"
" + LineProfitCteSql + @"
SELECT
    ProductoNombre AS Producto,
    COALESCE(SUM(Cantidad), 0) AS Cantidad,
    COALESCE(SUM(TotalLinea), 0) AS Total,
    COALESCE(SUM(GananciaLinea), 0) AS Ganancia
FROM VentaLineas
GROUP BY ProductoNombre
ORDER BY Ganancia DESC
LIMIT 5;";

    private const string RankingVendidosSql = @"
" + LineProfitCteSql + @"
SELECT
    ProductoNombre AS Producto,
    COALESCE(SUM(Cantidad), 0) AS Cantidad,
    COALESCE(SUM(TotalLinea), 0) AS Total,
    COALESCE(SUM(GananciaLinea), 0) AS Ganancia
FROM VentaLineas
GROUP BY ProductoNombre
ORDER BY Cantidad DESC
LIMIT 5;";
}
