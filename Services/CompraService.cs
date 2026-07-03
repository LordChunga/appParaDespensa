#nullable enable

using System.Globalization;
using System.IO;
using System.Text;
using Dapper;
using PosLocal.Data;

namespace PosLocal.Services;

public sealed class CompraService : ICompraService
{
    private readonly IPosDbContext _dbContext;

    public CompraService(IPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CompraHistorialResult> BuscarHistorialAsync(
        CompraHistorialQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamanoPagina = Math.Clamp(query.TamanoPagina, 1, 100);
        var offset = (pagina - 1) * tamanoPagina;
        var search = BuildSearchTerm(query.TextoBusqueda);

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            Search = search,
            Limit = tamanoPagina,
            Offset = offset
        };

        var items = (await connection.QueryAsync<CompraHistorialDto>(new CommandDefinition(
            HistorialSql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            HistorialCountSql,
            parameters,
            cancellationToken: cancellationToken));

        return new CompraHistorialResult(items, total);
    }

    public async Task<CompraDetalleStockDto?> ObtenerDetalleAsync(
        int compraDetalleId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CompraDetalleStockDto>(new CommandDefinition(
            DetalleSql,
            new { CompraDetalleId = compraDetalleId },
            cancellationToken: cancellationToken));
    }

    public async Task ExportarHistorialAsync(
        ExportarComprasRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RutaDestino))
        {
            throw new ArgumentException("La ruta de exportacion es obligatoria.", nameof(request));
        }

        var search = BuildSearchTerm(request.TextoBusqueda);
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        var items = (await connection.QueryAsync<CompraHistorialDto>(new CommandDefinition(
            HistorialExportSql,
            new { Search = search },
            cancellationToken: cancellationToken))).AsList();

        var directory = Path.GetDirectoryName(request.RutaDestino);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var csv = BuildCsv(items);
        await File.WriteAllTextAsync(request.RutaDestino, csv, Encoding.UTF8, cancellationToken);
    }

    private static string? BuildSearchTerm(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? null
            : $"%{text.Trim()}%";
    }

    private static string BuildCsv(IReadOnlyList<CompraHistorialDto> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Compra,Producto,CodigoBarras,Fecha,Unidad,CostoUnitario,Cantidad,Subtotal,Usuario,Proveedor");

        foreach (var item in items)
        {
            builder
                .Append(EscapeCsv(item.CompraNumero)).Append(',')
                .Append(EscapeCsv(item.ProductoNombre)).Append(',')
                .Append(EscapeCsv(item.CodigoBarras)).Append(',')
                .Append(EscapeCsv(item.Fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))).Append(',')
                .Append(EscapeCsv(item.Unidad)).Append(',')
                .Append(item.CostoUnitario.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(item.Cantidad.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append((item.CostoUnitario * item.Cantidad).ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(EscapeCsv(item.Usuario)).Append(',')
                .Append(EscapeCsv(item.ProveedorNombre))
                .AppendLine();
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

    private const string BaseHistorialFromSql = @"
FROM DetallesCompra dc
INNER JOIN Compras c ON c.Id = dc.CompraId
INNER JOIN Productos p ON p.Id = dc.ProductoId
INNER JOIN Proveedores pr ON pr.Id = c.ProveedorId
WHERE (
    @Search IS NULL
    OR dc.ProductoNombre LIKE @Search
    OR dc.CodigoBarras LIKE @Search
    OR dc.Unidad LIKE @Search
    OR c.Usuario LIKE @Search
    OR pr.RazonSocial LIKE @Search
)";

    private const string HistorialSql = @"
SELECT
    dc.Id AS CompraDetalleId,
    c.Id AS CompraId,
    COALESCE(c.NumeroComprobante, printf('COMP-%06d', c.Id)) AS CompraNumero,
    dc.ProductoId,
    dc.ProductoNombre,
    COALESCE(dc.CodigoBarras, p.CodigoBarras, p.Codigo) AS CodigoBarras,
    c.Fecha,
    dc.Unidad,
    dc.CostoUnitario,
    dc.Cantidad,
    c.Usuario,
    pr.RazonSocial AS ProveedorNombre
" + BaseHistorialFromSql + @"
ORDER BY c.Fecha DESC, dc.Id DESC
LIMIT @Limit OFFSET @Offset;";

    private const string HistorialCountSql = @"
SELECT COUNT(1)
" + BaseHistorialFromSql + ";";

    private const string HistorialExportSql = @"
SELECT
    dc.Id AS CompraDetalleId,
    c.Id AS CompraId,
    COALESCE(c.NumeroComprobante, printf('COMP-%06d', c.Id)) AS CompraNumero,
    dc.ProductoId,
    dc.ProductoNombre,
    COALESCE(dc.CodigoBarras, p.CodigoBarras, p.Codigo) AS CodigoBarras,
    c.Fecha,
    dc.Unidad,
    dc.CostoUnitario,
    dc.Cantidad,
    c.Usuario,
    pr.RazonSocial AS ProveedorNombre
" + BaseHistorialFromSql + @"
ORDER BY c.Fecha DESC, dc.Id DESC;";

    private const string DetalleSql = @"
WITH UltimoPedido AS (
    SELECT
        dc2.ProductoId,
        MAX(c2.Fecha) AS FechaUltimoPedido
    FROM DetallesCompra dc2
    INNER JOIN Compras c2 ON c2.Id = dc2.CompraId
    WHERE dc2.Id <> @CompraDetalleId
    GROUP BY dc2.ProductoId
)
SELECT
    dc.Id AS CompraDetalleId,
    c.Id AS CompraId,
    COALESCE(c.NumeroComprobante, printf('COMP-%06d', c.Id)) AS CompraNumero,
    dc.ProductoId,
    dc.ProductoNombre,
    COALESCE(dc.CodigoBarras, p.CodigoBarras, p.Codigo) AS CodigoBarras,
    c.Fecha,
    dc.Unidad,
    dc.CostoUnitario,
    dc.Cantidad,
    c.Usuario,
    pr.RazonSocial AS ProveedorNombre,
    COALESCE(pr.NombreComercial, pr.RazonSocial) AS ProveedorContacto,
    COALESCE(pr.Telefono, 'Sin telefono') AS ProveedorTelefono,
    CASE
        WHEN up.FechaUltimoPedido IS NULL THEN 'Sin pedidos previos'
        ELSE 'Hace ' || CAST(MAX(0, CAST(julianday('now') - julianday(up.FechaUltimoPedido) AS INTEGER)) AS TEXT) || ' dias'
    END AS UltimoPedidoTexto,
    p.StockActual,
    p.StockMaximo,
    p.StockMinimo AS CantidadCritica,
    NULL AS ProximaEntregaEstimada
FROM DetallesCompra dc
INNER JOIN Compras c ON c.Id = dc.CompraId
INNER JOIN Productos p ON p.Id = dc.ProductoId
INNER JOIN Proveedores pr ON pr.Id = c.ProveedorId
LEFT JOIN UltimoPedido up ON up.ProductoId = dc.ProductoId
WHERE dc.Id = @CompraDetalleId;";
}
