#nullable enable

namespace PosLocal.Services;

public sealed record ReportesPeriodoRequest(
    DateTime FechaDesde,
    DateTime FechaHasta);

public sealed record KpiReportesDto(
    decimal IngresosPeriodo,
    decimal GananciaEstimada,
    int TotalVentas,
    decimal TicketPromedio);

public sealed record ResumenSecundarioReportesDto(
    string PagoMasUsado,
    string ProductoMasPopular,
    string ProximoVencimiento,
    string PlanActivo,
    string PlanSubtitulo);

public sealed record SerieDiariaReporteDto(
    DateTime Fecha,
    decimal Ingresos,
    decimal Ganancia);

public sealed record MetodoPagoReporteDto(
    string MetodoPago,
    decimal Total,
    int CantidadVentas);

public sealed record RankingProductoReporteDto(
    string Producto,
    decimal Cantidad,
    decimal Total,
    decimal Ganancia);

public sealed record DashboardReportesDto(
    KpiReportesDto Kpis,
    ResumenSecundarioReportesDto ResumenSecundario,
    IReadOnlyList<SerieDiariaReporteDto> SeriesDiarias,
    IReadOnlyList<MetodoPagoReporteDto> MetodosPago,
    IReadOnlyList<RankingProductoReporteDto> ProductosMasGanancia,
    IReadOnlyList<RankingProductoReporteDto> ProductosMasVendidos);
