#nullable enable

using Dapper;
using PosLocal.Data;

namespace PosLocal.Services;

public sealed class InventarioService : IInventarioService
{
    private readonly IPosDbContext _dbContext;

    public InventarioService(IPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventarioProductoResult> BuscarProductosAsync(
        InventarioProductoQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamanoPagina = Math.Clamp(query.TamanoPagina, 5, 100);
        var offset = (pagina - 1) * tamanoPagina;
        var orderBy = BuildOrderBy(query.OrdenarPor, query.OrdenDescendente);

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            Search = BuildSearchTerm(query.TextoBusqueda),
            Limit = tamanoPagina,
            Offset = offset
        };

        var items = (await connection.QueryAsync<InventarioProductoDto>(new CommandDefinition(
            InventarioSql + orderBy + " LIMIT @Limit OFFSET @Offset;",
            parameters,
            cancellationToken: cancellationToken))).AsList();

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            InventarioCountSql,
            parameters,
            cancellationToken: cancellationToken));

        return new InventarioProductoResult(items, total);
    }

    public async Task CambiarDisponibilidadAsync(
        int productoId,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Productos SET Activo = @Activo, FechaActualizacion = CURRENT_TIMESTAMP WHERE Id = @ProductoId;",
            new { ProductoId = productoId, Activo = activo ? 1 : 0 },
            cancellationToken: cancellationToken));
    }

    public async Task AjustarStockAsync(
        AjusteStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new ArgumentException("El motivo del ajuste es obligatorio.", nameof(request));
        }

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var stockAnterior = await connection.ExecuteScalarAsync<decimal?>(new CommandDefinition(
            "SELECT StockActual FROM Productos WHERE Id = @ProductoId;",
            new { request.ProductoId },
            transaction,
            cancellationToken: cancellationToken));

        if (stockAnterior is null)
        {
            throw new InvalidOperationException("No se encontro el producto a ajustar.");
        }

        var stockNuevo = stockAnterior.Value + request.CantidadAjuste;
        if (stockNuevo < 0m)
        {
            throw new InvalidOperationException("El ajuste deja el stock por debajo de cero.");
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Productos SET StockActual = @StockNuevo, FechaActualizacion = CURRENT_TIMESTAMP WHERE Id = @ProductoId;",
            new { request.ProductoId, StockNuevo = stockNuevo },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            @"
INSERT INTO MovimientosInventario (
    ProductoId,
    Fecha,
    Tipo,
    Cantidad,
    StockAnterior,
    StockNuevo,
    Usuario,
    Observaciones)
VALUES (
    @ProductoId,
    CURRENT_TIMESTAMP,
    'AjusteManual',
    @Cantidad,
    @StockAnterior,
    @StockNuevo,
    @Usuario,
    @Observaciones);",
            new
            {
                request.ProductoId,
                Cantidad = request.CantidadAjuste,
                StockAnterior = stockAnterior.Value,
                StockNuevo = stockNuevo,
                Usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "Alejandro" : request.Usuario,
                Observaciones = request.Motivo.Trim()
            },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }

    private static string? BuildSearchTerm(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? null
            : $"%{text.Trim()}%";
    }

    private static string BuildOrderBy(string? column, bool desc)
    {
        var selectedColumn = column?.Trim().ToLowerInvariant() switch
        {
            "producto" or "nombre" => "p.Nombre",
            "categoria" => "c.Nombre",
            "estado" => "p.Activo",
            "stock" => "p.StockActual",
            "proveedor" => "pr.RazonSocial",
            _ => "p.Nombre"
        };

        return $" ORDER BY {selectedColumn} {(desc ? "DESC" : "ASC")}";
    }

    private const string InventarioBaseFromSql = @"
FROM Productos p
INNER JOIN Categorias c ON c.Id = p.CategoriaId
LEFT JOIN Proveedores pr ON pr.Id = p.ProveedorId
WHERE (
    @Search IS NULL
    OR p.Nombre LIKE @Search
    OR p.Codigo LIKE @Search
    OR p.CodigoBarras LIKE @Search
)";

    private const string InventarioSql = @"
SELECT
    p.Id AS ProductoId,
    p.Nombre,
    p.Codigo,
    p.CodigoBarras,
    c.Nombre AS CategoriaNombre,
    p.StockActual,
    p.StockMinimo,
    p.UnidadMedida,
    COALESCE(pr.RazonSocial, 'Sin proveedor') AS ProveedorNombre,
    p.Activo
" + InventarioBaseFromSql;

    private const string InventarioCountSql = @"
SELECT COUNT(1)
" + InventarioBaseFromSql + ";";
}
