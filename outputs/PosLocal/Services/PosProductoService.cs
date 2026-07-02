#nullable enable

using Dapper;
using PosLocal.Data;

namespace PosLocal.Services;

public sealed class PosProductoService : IPosProductoService
{
    private readonly IPosDbContext _dbContext;

    public PosProductoService(IPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductoVentaDto?> BuscarPorTextoOCodigoAsync(
        string criterio,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(criterio))
        {
            return null;
        }

        var search = $"%{criterio.Trim()}%";
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        var producto = await connection.QueryFirstOrDefaultAsync<ProductoVentaDto>(new CommandDefinition(
            ProductoPorTextoSql,
            new { Search = search },
            cancellationToken: cancellationToken));

        if (producto is not null)
        {
            return producto;
        }

        return await connection.QueryFirstOrDefaultAsync<ProductoVentaDto>(new CommandDefinition(
            ComboPorTextoSql,
            new { Search = search },
            cancellationToken: cancellationToken));
    }

    public async Task<ProductoVentaDto?> BuscarPorCodigoBarrasAsync(
        string codigoBarras,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
        {
            return null;
        }

        var barcode = codigoBarras.Trim();
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        var producto = await connection.QueryFirstOrDefaultAsync<ProductoVentaDto>(new CommandDefinition(
            ProductoPorCodigoBarrasSql,
            new { CodigoBarras = barcode },
            cancellationToken: cancellationToken));

        if (producto is not null)
        {
            return producto;
        }

        return await connection.QueryFirstOrDefaultAsync<ProductoVentaDto>(new CommandDefinition(
            ComboPorCodigoBarrasSql,
            new { CodigoBarras = barcode },
            cancellationToken: cancellationToken));
    }

    private const string ProductoSelectSql = @"
SELECT
    p.Id,
    p.Codigo,
    p.CodigoBarras,
    p.Nombre,
    p.PrecioVenta,
    p.StockActual,
    p.UnidadMedida,
    p.VentaPorPeso,
    p.PesoBaseGramos,
    NULL AS ComboId,
    0 AS EsCombo
FROM Productos p
";

    private const string ProductoPorTextoSql = ProductoSelectSql + @"
WHERE p.Activo = 1
AND (
    p.Nombre LIKE @Search
    OR p.Codigo LIKE @Search
    OR p.CodigoBarras LIKE @Search
)
ORDER BY p.Nombre
LIMIT 1;";

    private const string ProductoPorCodigoBarrasSql = ProductoSelectSql + @"
WHERE p.Activo = 1 AND p.CodigoBarras = @CodigoBarras
LIMIT 1;";

    private const string ComboSelectSql = @"
SELECT
    0 AS Id,
    c.Codigo,
    c.CodigoBarras,
    c.Nombre,
    c.PrecioCombo AS PrecioVenta,
    COALESCE(MIN(p.StockActual / cd.Cantidad), 0) AS StockActual,
    'COMBO' AS UnidadMedida,
    0 AS VentaPorPeso,
    1 AS PesoBaseGramos,
    c.Id AS ComboId,
    1 AS EsCombo
FROM Combos c
INNER JOIN ComboDetalles cd ON cd.ComboId = c.Id
INNER JOIN Productos p ON p.Id = cd.ProductoId
";

    private const string ComboPorTextoSql = ComboSelectSql + @"
WHERE c.Activo = 1
AND (
    c.Nombre LIKE @Search
    OR c.Codigo LIKE @Search
    OR c.CodigoBarras LIKE @Search
)
GROUP BY c.Id, c.Codigo, c.CodigoBarras, c.Nombre, c.PrecioCombo
ORDER BY c.Nombre
LIMIT 1;";

    private const string ComboPorCodigoBarrasSql = ComboSelectSql + @"
WHERE c.Activo = 1 AND c.CodigoBarras = @CodigoBarras
GROUP BY c.Id, c.Codigo, c.CodigoBarras, c.Nombre, c.PrecioCombo
LIMIT 1;";
}
