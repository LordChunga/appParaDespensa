#nullable enable

using Dapper;
using Microsoft.Data.Sqlite;
using PosLocal.Data;

namespace PosLocal.Services;

public sealed class PosVentaService : IPosVentaService
{
    private readonly IPosDbContext _dbContext;

    public PosVentaService(IPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> RegistrarVentaAsync(
        RegistrarVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new ArgumentException("La venta debe tener al menos un item.", nameof(request));
        }

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var numero = $"V-{DateTime.Now:yyyyMMdd-HHmmssfff}";

        await connection.ExecuteAsync(new CommandDefinition(
            InsertVentaSql,
            new
            {
                Numero = numero,
                request.Cliente,
                request.Fecha,
                request.Subtotal,
                request.Descuento,
                request.Recargo,
                request.Impuesto,
                request.Total,
                MetodoPago = request.MetodoPago.ToString(),
                request.Estado,
                Observaciones = request.MetodoPago == MetodoPagoVenta.Transferencia
                    ? "Pago por transferencia pendiente de conciliacion"
                    : null
            },
            transaction,
            cancellationToken: cancellationToken));

        var ventaId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT last_insert_rowid();",
            transaction: transaction,
            cancellationToken: cancellationToken));

        foreach (var item in request.Items)
        {
            await InsertarDetalleAsync(connection, transaction, ventaId, item, cancellationToken);

            if (item.ProductoId is not null
                && item.ProductoId.Value != SystemProductIds.ManualAmountProductId)
            {
                await DescontarProductoAsync(
                    connection,
                    transaction,
                    item.ProductoId.Value,
                    ventaId,
                    item.Cantidad,
                    "Venta",
                    request.Cliente,
                    cancellationToken);
            }
            else if (item.ComboId is not null)
            {
                await DescontarComboAsync(
                    connection,
                    transaction,
                    item.ComboId.Value,
                    ventaId,
                    item.Cantidad,
                    request.Cliente,
                    cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return ventaId;
    }

    public async Task<IReadOnlyList<TransferenciaPendienteDto>> ObtenerTransferenciasPendientesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<TransferenciaPendienteDto>(new CommandDefinition(
            @"
SELECT
    Id AS VentaId,
    Numero,
    Fecha,
    Cliente,
    Total,
    Estado
FROM Ventas
WHERE MetodoPago = 'Transferencia'
AND Estado IN ('PagoPendiente', 'RequiereValidacion')
ORDER BY Fecha DESC;",
            cancellationToken: cancellationToken))).AsList();
    }

    public async Task AprobarTransferenciaAsync(
        int ventaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            @"
UPDATE Ventas
SET Estado = 'Completada',
    Observaciones = COALESCE(Observaciones, '') || ' | Transferencia conciliada'
WHERE Id = @VentaId
AND MetodoPago = 'Transferencia';",
            new { VentaId = ventaId },
            cancellationToken: cancellationToken));
    }

    private static async Task InsertarDetalleAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        int ventaId,
        RegistrarVentaItemRequest item,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            @"
INSERT INTO DetallesVenta (
    VentaId,
    ProductoId,
    ComboId,
    ProductoCodigo,
    ProductoNombre,
    Cantidad,
    PrecioUnitario,
    Descuento,
    Impuesto,
    TotalLinea)
VALUES (
    @VentaId,
    @ProductoId,
    @ComboId,
    @ProductoCodigo,
    @ProductoNombre,
    @Cantidad,
    @PrecioUnitario,
    @Descuento,
    @Impuesto,
    @TotalLinea);",
            new
            {
                VentaId = ventaId,
                item.ProductoId,
                item.ComboId,
                ProductoCodigo = item.Codigo,
                ProductoNombre = item.Nombre,
                item.Cantidad,
                item.PrecioUnitario,
                item.Descuento,
                item.Impuesto,
                item.TotalLinea
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task DescontarComboAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        int comboId,
        int ventaId,
        decimal cantidadCombo,
        string usuario,
        CancellationToken cancellationToken)
    {
        var detalles = (await connection.QueryAsync<ComboStockRow>(new CommandDefinition(
            @"
SELECT
    cd.ProductoId,
    cd.Cantidad,
    p.StockActual
FROM ComboDetalles cd
INNER JOIN Productos p ON p.Id = cd.ProductoId
WHERE cd.ComboId = @ComboId;",
            new { ComboId = comboId },
            transaction,
            cancellationToken: cancellationToken))).AsList();

        if (detalles.Count == 0)
        {
            throw new InvalidOperationException("El combo no tiene productos configurados.");
        }

        foreach (var detalle in detalles)
        {
            var requerido = detalle.Cantidad * cantidadCombo;
            await DescontarProductoAsync(
                connection,
                transaction,
                detalle.ProductoId,
                ventaId,
                requerido,
                "VentaCombo",
                usuario,
                cancellationToken);
        }
    }

    private static async Task DescontarProductoAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        int productoId,
        int ventaId,
        decimal cantidad,
        string tipoMovimiento,
        string usuario,
        CancellationToken cancellationToken)
    {
        var stockAnterior = await connection.ExecuteScalarAsync<decimal?>(new CommandDefinition(
            "SELECT StockActual FROM Productos WHERE Id = @ProductoId;",
            new { ProductoId = productoId },
            transaction,
            cancellationToken: cancellationToken));

        if (stockAnterior is null)
        {
            throw new InvalidOperationException("No se encontro el producto para descontar stock.");
        }

        if (stockAnterior.Value < cantidad)
        {
            throw new InvalidOperationException("Stock insuficiente para completar la venta.");
        }

        var stockNuevo = stockAnterior.Value - cantidad;

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Productos SET StockActual = @StockNuevo, FechaActualizacion = CURRENT_TIMESTAMP WHERE Id = @ProductoId;",
            new { ProductoId = productoId, StockNuevo = stockNuevo },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            @"
INSERT INTO MovimientosInventario (
    ProductoId,
    VentaId,
    Fecha,
    Tipo,
    Cantidad,
    StockAnterior,
    StockNuevo,
    Usuario,
    Observaciones)
VALUES (
    @ProductoId,
    @VentaId,
    CURRENT_TIMESTAMP,
    @Tipo,
    @Cantidad,
    @StockAnterior,
    @StockNuevo,
    @Usuario,
    @Observaciones);",
            new
            {
                ProductoId = productoId,
                VentaId = ventaId,
                Tipo = tipoMovimiento,
                Cantidad = -cantidad,
                StockAnterior = stockAnterior.Value,
                StockNuevo = stockNuevo,
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario,
                Observaciones = "Descuento automatico por venta"
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    private const string InsertVentaSql = @"
INSERT INTO Ventas (
    Numero,
    Cliente,
    Fecha,
    Subtotal,
    Descuento,
    Recargo,
    Impuesto,
    Total,
    MetodoPago,
    Estado,
    Observaciones)
VALUES (
    @Numero,
    @Cliente,
    @Fecha,
    @Subtotal,
    @Descuento,
    @Recargo,
    @Impuesto,
    @Total,
    @MetodoPago,
    @Estado,
    @Observaciones);";

    private sealed class ComboStockRow
    {
        public int ProductoId { get; init; }

        public decimal Cantidad { get; init; }

        public decimal StockActual { get; init; }
    }
}
