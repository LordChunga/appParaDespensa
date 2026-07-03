#nullable enable

using Dapper;
using PosLocal.Data;

namespace PosLocal.Services;

public sealed class DatabaseSeederService : IDatabaseSeederService
{
    private readonly IPosDbContext _dbContext;

    public DatabaseSeederService(IPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await EnsureSystemRecordsAsync(connection, transaction, cancellationToken);

        var productosCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM Productos WHERE Id <> @ManualAmountProductId;",
            new { SystemProductIds.ManualAmountProductId },
            transaction,
            cancellationToken: cancellationToken));

        if (productosCount > 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var golosinasId = await InsertCategoriaAsync(
            connection,
            transaction,
            "Golosinas",
            "Productos dulces, chocolates, alfajores y caramelos.",
            cancellationToken);

        var fiambreriaId = await InsertCategoriaAsync(
            connection,
            transaction,
            "Fiambrer\u00EDa",
            "Productos frescos vendidos por unidad o por peso.",
            cancellationToken);

        var productos = new[]
        {
            new SeedProducto(
                golosinasId,
                "GOL-ALF-001",
                "7790580123456",
                "Alfajor Triple Chocolate",
                "Alfajor triple relleno con ba\u00F1o de chocolate.",
                350m,
                650m,
                48m,
                "UN",
                false),
            new SeedProducto(
                golosinasId,
                "GOL-CHO-001",
                "7790975123457",
                "Chocolate con Leche 100g",
                "Tableta de chocolate con leche.",
                1200m,
                1900m,
                24m,
                "UN",
                false),
            new SeedProducto(
                golosinasId,
                "GOL-CAR-001",
                "7791122334455",
                "Caramelos Masticables 100g",
                "Bolsa de caramelos surtidos.",
                500m,
                850m,
                30m,
                "UN",
                false),
            new SeedProducto(
                fiambreriaId,
                "FIA-QUE-001",
                "7798123456789",
                "Queso Cremoso",
                "Queso cremoso fresco vendido por kilo.",
                4200m,
                6900m,
                18.5m,
                "KG",
                true),
            new SeedProducto(
                fiambreriaId,
                "FIA-JAM-001",
                "7794567890123",
                "Jam\u00F3n Cocido 200g",
                "Feteado de jam\u00F3n cocido envasado.",
                1800m,
                2800m,
                20m,
                "UN",
                false)
        };

        var productoIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var producto in productos)
        {
            productoIds[producto.Codigo] = await InsertProductoAsync(
                connection,
                transaction,
                producto,
                cancellationToken);
        }

        await InsertVentaAsync(
            connection,
            transaction,
            new SeedVenta(
                "V-DEMO-001",
                "Consumidor Final",
                DateTime.Now.AddDays(-1).Date.AddHours(18).AddMinutes(10),
                4750m,
                0m,
                0m,
                4750m,
                "Efectivo",
                "Completada",
                new[]
                {
                    new SeedDetalle(productoIds["GOL-ALF-001"], "GOL-ALF-001", "Alfajor Triple Chocolate", 2m, 650m, 1300m),
                    new SeedDetalle(productoIds["FIA-QUE-001"], "FIA-QUE-001", "Queso Cremoso", 0.5m, 6900m, 3450m)
                }),
            cancellationToken);

        await InsertVentaAsync(
            connection,
            transaction,
            new SeedVenta(
                "V-DEMO-002",
                "Consumidor Final",
                DateTime.Now.AddDays(-2).Date.AddHours(11).AddMinutes(35),
                4700m,
                0m,
                0m,
                4700m,
                "Transferencia",
                "RequiereValidacion",
                new[]
                {
                    new SeedDetalle(productoIds["GOL-CHO-001"], "GOL-CHO-001", "Chocolate con Leche 100g", 1m, 1900m, 1900m),
                    new SeedDetalle(productoIds["FIA-JAM-001"], "FIA-JAM-001", "Jam\u00F3n Cocido 200g", 1m, 2800m, 2800m)
                }),
            cancellationToken);

        await InsertVentaAsync(
            connection,
            transaction,
            new SeedVenta(
                "V-DEMO-003",
                "Consumidor Final",
                DateTime.Now.AddDays(-4).Date.AddHours(16).AddMinutes(5),
                3200m,
                200m,
                0m,
                3000m,
                "Tarjeta",
                "Completada",
                new[]
                {
                    new SeedDetalle(productoIds["GOL-CAR-001"], "GOL-CAR-001", "Caramelos Masticables 100g", 3m, 850m, 2550m),
                    new SeedDetalle(productoIds["GOL-ALF-001"], "GOL-ALF-001", "Alfajor Triple Chocolate", 1m, 650m, 650m)
                }),
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureSystemRecordsAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            @"
INSERT INTO Categorias (Id, Nombre, Descripcion, Activa)
VALUES (@Id, @Nombre, @Descripcion, 1)
ON CONFLICT(Id) DO UPDATE SET
    Nombre = excluded.Nombre,
    Descripcion = excluded.Descripcion,
    Activa = 1;",
            new
            {
                Id = SystemProductIds.SystemCategoryId,
                Nombre = "Sistema",
                Descripcion = "Categoria reservada para items internos del POS."
            },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            @"
INSERT INTO Productos (
    Id,
    CategoriaId,
    Codigo,
    CodigoBarras,
    Nombre,
    Descripcion,
    PrecioCosto,
    PrecioVenta,
    StockActual,
    StockMinimo,
    StockMaximo,
    UnidadMedida,
    VentaPorPeso,
    PesoBaseGramos,
    Activo)
VALUES (
    @Id,
    @CategoriaId,
    @Codigo,
    NULL,
    @Nombre,
    @Descripcion,
    0,
    0,
    999999999,
    0,
    999999999,
    'UN',
    0,
    1000,
    1)
ON CONFLICT(Id) DO UPDATE SET
    CategoriaId = excluded.CategoriaId,
    Codigo = excluded.Codigo,
    Nombre = excluded.Nombre,
    Descripcion = excluded.Descripcion,
    StockActual = excluded.StockActual,
    Activo = 1;",
            new
            {
                Id = SystemProductIds.ManualAmountProductId,
                CategoriaId = SystemProductIds.SystemCategoryId,
                Codigo = SystemProductIds.ManualAmountCode,
                Nombre = SystemProductIds.ManualAmountName,
                Descripcion = "Producto reservado para registrar montos manuales sin romper DetallesVenta."
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task<int> InsertCategoriaAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        string nombre,
        string descripcion,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            @"
INSERT INTO Categorias (Nombre, Descripcion, Activa)
VALUES (@Nombre, @Descripcion, 1);",
            new { Nombre = nombre, Descripcion = descripcion },
            transaction,
            cancellationToken: cancellationToken));

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT last_insert_rowid();",
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task<int> InsertProductoAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        SeedProducto producto,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            @"
INSERT INTO Productos (
    CategoriaId,
    Codigo,
    CodigoBarras,
    Nombre,
    Descripcion,
    PrecioCosto,
    PrecioVenta,
    StockActual,
    StockMinimo,
    StockMaximo,
    UnidadMedida,
    VentaPorPeso,
    PesoBaseGramos,
    Activo)
VALUES (
    @CategoriaId,
    @Codigo,
    @CodigoBarras,
    @Nombre,
    @Descripcion,
    @PrecioCosto,
    @PrecioVenta,
    @StockActual,
    @StockMinimo,
    @StockMaximo,
    @UnidadMedida,
    @VentaPorPeso,
    @PesoBaseGramos,
    1);",
            new
            {
                producto.CategoriaId,
                producto.Codigo,
                producto.CodigoBarras,
                producto.Nombre,
                producto.Descripcion,
                producto.PrecioCosto,
                producto.PrecioVenta,
                producto.StockActual,
                StockMinimo = producto.VentaPorPeso ? 2m : 5m,
                StockMaximo = producto.VentaPorPeso ? 50m : 100m,
                producto.UnidadMedida,
                VentaPorPeso = producto.VentaPorPeso ? 1 : 0,
                PesoBaseGramos = producto.VentaPorPeso ? 1000m : 1000m
            },
            transaction,
            cancellationToken: cancellationToken));

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT last_insert_rowid();",
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task InsertVentaAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        SeedVenta venta,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            @"
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
    0,
    @Total,
    @MetodoPago,
    @Estado,
    @Observaciones);",
            new
            {
                venta.Numero,
                venta.Cliente,
                venta.Fecha,
                venta.Subtotal,
                venta.Descuento,
                venta.Recargo,
                venta.Total,
                venta.MetodoPago,
                venta.Estado,
                Observaciones = venta.Estado == "RequiereValidacion"
                    ? "Venta demo por transferencia pendiente de conciliacion"
                    : "Venta demo para dashboard"
            },
            transaction,
            cancellationToken: cancellationToken));

        var ventaId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT last_insert_rowid();",
            transaction: transaction,
            cancellationToken: cancellationToken));

        foreach (var detalle in venta.Detalles)
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
    NULL,
    @ProductoCodigo,
    @ProductoNombre,
    @Cantidad,
    @PrecioUnitario,
    0,
    0,
    @TotalLinea);",
                new
                {
                    VentaId = ventaId,
                    ProductoId = detalle.ProductoId,
                    ProductoCodigo = detalle.ProductoCodigo,
                    ProductoNombre = detalle.ProductoNombre,
                    detalle.Cantidad,
                    detalle.PrecioUnitario,
                    detalle.TotalLinea
                },
                transaction,
                cancellationToken: cancellationToken));
        }
    }

    private sealed record SeedProducto(
        int CategoriaId,
        string Codigo,
        string CodigoBarras,
        string Nombre,
        string Descripcion,
        decimal PrecioCosto,
        decimal PrecioVenta,
        decimal StockActual,
        string UnidadMedida,
        bool VentaPorPeso);

    private sealed record SeedVenta(
        string Numero,
        string Cliente,
        DateTime Fecha,
        decimal Subtotal,
        decimal Descuento,
        decimal Recargo,
        decimal Total,
        string MetodoPago,
        string Estado,
        IReadOnlyList<SeedDetalle> Detalles);

    private sealed record SeedDetalle(
        int ProductoId,
        string ProductoCodigo,
        string ProductoNombre,
        decimal Cantidad,
        decimal PrecioUnitario,
        decimal TotalLinea);
}
