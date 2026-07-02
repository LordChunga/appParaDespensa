#nullable enable

using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;

namespace PosLocal.Data;

public sealed class PosDbContext : IPosDbContext
{
    public PosDbContext(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("La ruta de la base de datos es obligatoria.", nameof(databasePath));
        }

        DatabasePath = Path.GetFullPath(databasePath);

        var directoryPath = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        ConnectionString = connectionStringBuilder.ToString();
    }

    public string DatabasePath { get; }

    public string ConnectionString { get; }

    public async Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            "PRAGMA foreign_keys = ON;",
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            "PRAGMA busy_timeout = 5000;",
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "PRAGMA journal_mode = WAL;",
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return connection;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateCategoriasTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateProveedoresTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateProductosTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateVentasTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateDetallesVentaTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateComprasTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateDetallesCompraTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateMovimientosInventarioTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateCombosTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateComboDetallesTableSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            CreateIndexesSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            SeedDataSql,
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private const string CreateCategoriasTableSql = @"
CREATE TABLE IF NOT EXISTS Categorias (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL UNIQUE,
    Descripcion TEXT NULL,
    Activa INTEGER NOT NULL DEFAULT 1 CHECK (Activa IN (0, 1)),
    FechaCreacion TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion TEXT NULL
);";

    private const string CreateProveedoresTableSql = @"
CREATE TABLE IF NOT EXISTS Proveedores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RazonSocial TEXT NOT NULL,
    NombreComercial TEXT NULL,
    Documento TEXT NULL UNIQUE,
    Telefono TEXT NULL,
    Email TEXT NULL,
    Direccion TEXT NULL,
    Notas TEXT NULL,
    Activo INTEGER NOT NULL DEFAULT 1 CHECK (Activo IN (0, 1)),
    FechaCreacion TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion TEXT NULL
);";

    private const string CreateProductosTableSql = @"
CREATE TABLE IF NOT EXISTS Productos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CategoriaId INTEGER NOT NULL,
    ProveedorId INTEGER NULL,
    Codigo TEXT NOT NULL UNIQUE,
    CodigoBarras TEXT NULL UNIQUE,
    Nombre TEXT NOT NULL,
    Descripcion TEXT NULL,
    PrecioCosto NUMERIC NOT NULL DEFAULT 0 CHECK (PrecioCosto >= 0),
    PrecioVenta NUMERIC NOT NULL DEFAULT 0 CHECK (PrecioVenta >= 0),
    StockActual NUMERIC NOT NULL DEFAULT 0 CHECK (StockActual >= 0),
    StockMinimo NUMERIC NOT NULL DEFAULT 0 CHECK (StockMinimo >= 0),
    StockMaximo NUMERIC NOT NULL DEFAULT 100 CHECK (StockMaximo > 0),
    UnidadMedida TEXT NOT NULL DEFAULT 'UN',
    VentaPorPeso INTEGER NOT NULL DEFAULT 0 CHECK (VentaPorPeso IN (0, 1)),
    PesoBaseGramos NUMERIC NOT NULL DEFAULT 1000 CHECK (PesoBaseGramos > 0),
    Activo INTEGER NOT NULL DEFAULT 1 CHECK (Activo IN (0, 1)),
    FechaCreacion TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion TEXT NULL,
    FOREIGN KEY (CategoriaId) REFERENCES Categorias (Id) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (ProveedorId) REFERENCES Proveedores (Id) ON UPDATE CASCADE ON DELETE SET NULL
);";

    private const string CreateVentasTableSql = @"
CREATE TABLE IF NOT EXISTS Ventas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Numero TEXT NOT NULL UNIQUE,
    Cliente TEXT NOT NULL DEFAULT 'Consumidor Final',
    Fecha TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Subtotal NUMERIC NOT NULL DEFAULT 0 CHECK (Subtotal >= 0),
    Descuento NUMERIC NOT NULL DEFAULT 0 CHECK (Descuento >= 0),
    Recargo NUMERIC NOT NULL DEFAULT 0 CHECK (Recargo >= 0),
    Impuesto NUMERIC NOT NULL DEFAULT 0 CHECK (Impuesto >= 0),
    Total NUMERIC NOT NULL DEFAULT 0 CHECK (Total >= 0),
    MetodoPago TEXT NOT NULL,
    Estado TEXT NOT NULL DEFAULT 'Completada',
    Observaciones TEXT NULL,
    FechaCreacion TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";

    private const string CreateDetallesVentaTableSql = @"
CREATE TABLE IF NOT EXISTS DetallesVenta (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VentaId INTEGER NOT NULL,
    ProductoId INTEGER NULL,
    ComboId INTEGER NULL,
    ProductoCodigo TEXT NOT NULL,
    ProductoNombre TEXT NOT NULL,
    Cantidad NUMERIC NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario NUMERIC NOT NULL CHECK (PrecioUnitario >= 0),
    Descuento NUMERIC NOT NULL DEFAULT 0 CHECK (Descuento >= 0),
    Impuesto NUMERIC NOT NULL DEFAULT 0 CHECK (Impuesto >= 0),
    TotalLinea NUMERIC NOT NULL CHECK (TotalLinea >= 0),
    CHECK (ProductoId IS NOT NULL OR ComboId IS NOT NULL),
    FOREIGN KEY (VentaId) REFERENCES Ventas (Id) ON UPDATE CASCADE ON DELETE CASCADE,
    FOREIGN KEY (ProductoId) REFERENCES Productos (Id) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (ComboId) REFERENCES Combos (Id) ON UPDATE CASCADE ON DELETE RESTRICT
);";

    private const string CreateComprasTableSql = @"
CREATE TABLE IF NOT EXISTS Compras (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProveedorId INTEGER NOT NULL,
    NumeroComprobante TEXT NULL,
    Fecha TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Usuario TEXT NOT NULL DEFAULT 'Alejandro',
    Subtotal NUMERIC NOT NULL DEFAULT 0 CHECK (Subtotal >= 0),
    Descuento NUMERIC NOT NULL DEFAULT 0 CHECK (Descuento >= 0),
    Impuesto NUMERIC NOT NULL DEFAULT 0 CHECK (Impuesto >= 0),
    Total NUMERIC NOT NULL DEFAULT 0 CHECK (Total >= 0),
    Estado TEXT NOT NULL DEFAULT 'Registrada',
    Observaciones TEXT NULL,
    FechaCreacion TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (ProveedorId, NumeroComprobante),
    FOREIGN KEY (ProveedorId) REFERENCES Proveedores (Id) ON UPDATE CASCADE ON DELETE RESTRICT
);";

    private const string CreateDetallesCompraTableSql = @"
CREATE TABLE IF NOT EXISTS DetallesCompra (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CompraId INTEGER NOT NULL,
    ProductoId INTEGER NOT NULL,
    ProductoCodigo TEXT NOT NULL,
    CodigoBarras TEXT NULL,
    ProductoNombre TEXT NOT NULL,
    Unidad TEXT NOT NULL DEFAULT 'UNITARIO',
    CostoUnitario NUMERIC NOT NULL CHECK (CostoUnitario >= 0),
    Cantidad NUMERIC NOT NULL CHECK (Cantidad > 0),
    Subtotal NUMERIC NOT NULL CHECK (Subtotal >= 0),
    FOREIGN KEY (CompraId) REFERENCES Compras (Id) ON UPDATE CASCADE ON DELETE CASCADE,
    FOREIGN KEY (ProductoId) REFERENCES Productos (Id) ON UPDATE CASCADE ON DELETE RESTRICT
);";

    private const string CreateMovimientosInventarioTableSql = @"
CREATE TABLE IF NOT EXISTS MovimientosInventario (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductoId INTEGER NOT NULL,
    CompraId INTEGER NULL,
    VentaId INTEGER NULL,
    Fecha TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Tipo TEXT NOT NULL,
    Cantidad NUMERIC NOT NULL,
    StockAnterior NUMERIC NOT NULL,
    StockNuevo NUMERIC NOT NULL,
    Usuario TEXT NOT NULL,
    Observaciones TEXT NULL,
    FOREIGN KEY (ProductoId) REFERENCES Productos (Id) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (CompraId) REFERENCES Compras (Id) ON UPDATE CASCADE ON DELETE SET NULL,
    FOREIGN KEY (VentaId) REFERENCES Ventas (Id) ON UPDATE CASCADE ON DELETE SET NULL
);";

    private const string CreateCombosTableSql = @"
CREATE TABLE IF NOT EXISTS Combos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Codigo TEXT NOT NULL UNIQUE,
    CodigoBarras TEXT NULL UNIQUE,
    Nombre TEXT NOT NULL,
    PrecioSugerido NUMERIC NOT NULL DEFAULT 0 CHECK (PrecioSugerido >= 0),
    PrecioCombo NUMERIC NOT NULL DEFAULT 0 CHECK (PrecioCombo >= 0),
    Activo INTEGER NOT NULL DEFAULT 1 CHECK (Activo IN (0, 1)),
    FechaCreacion TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion TEXT NULL
);";

    private const string CreateComboDetallesTableSql = @"
CREATE TABLE IF NOT EXISTS ComboDetalles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ComboId INTEGER NOT NULL,
    ProductoId INTEGER NOT NULL,
    Cantidad NUMERIC NOT NULL CHECK (Cantidad > 0),
    PrecioUnitarioSnapshot NUMERIC NOT NULL DEFAULT 0 CHECK (PrecioUnitarioSnapshot >= 0),
    UNIQUE (ComboId, ProductoId),
    FOREIGN KEY (ComboId) REFERENCES Combos (Id) ON UPDATE CASCADE ON DELETE CASCADE,
    FOREIGN KEY (ProductoId) REFERENCES Productos (Id) ON UPDATE CASCADE ON DELETE RESTRICT
);";

    private const string CreateIndexesSql = @"
CREATE INDEX IF NOT EXISTS IX_Productos_CategoriaId ON Productos (CategoriaId);
CREATE INDEX IF NOT EXISTS IX_Productos_ProveedorId ON Productos (ProveedorId);
CREATE INDEX IF NOT EXISTS IX_Productos_Nombre ON Productos (Nombre);
CREATE INDEX IF NOT EXISTS IX_Productos_CodigoBarras ON Productos (CodigoBarras);
CREATE INDEX IF NOT EXISTS IX_Ventas_Fecha ON Ventas (Fecha);
CREATE INDEX IF NOT EXISTS IX_DetallesVenta_VentaId ON DetallesVenta (VentaId);
CREATE INDEX IF NOT EXISTS IX_DetallesVenta_ProductoId ON DetallesVenta (ProductoId);
CREATE INDEX IF NOT EXISTS IX_DetallesVenta_ComboId ON DetallesVenta (ComboId);
CREATE INDEX IF NOT EXISTS IX_Compras_ProveedorId ON Compras (ProveedorId);
CREATE INDEX IF NOT EXISTS IX_Compras_Fecha ON Compras (Fecha);
CREATE INDEX IF NOT EXISTS IX_Compras_Usuario ON Compras (Usuario);
CREATE INDEX IF NOT EXISTS IX_DetallesCompra_CompraId ON DetallesCompra (CompraId);
CREATE INDEX IF NOT EXISTS IX_DetallesCompra_ProductoId ON DetallesCompra (ProductoId);
CREATE INDEX IF NOT EXISTS IX_DetallesCompra_ProductoNombre ON DetallesCompra (ProductoNombre);
CREATE INDEX IF NOT EXISTS IX_DetallesCompra_CodigoBarras ON DetallesCompra (CodigoBarras);
CREATE INDEX IF NOT EXISTS IX_MovimientosInventario_ProductoId ON MovimientosInventario (ProductoId);
CREATE INDEX IF NOT EXISTS IX_MovimientosInventario_Fecha ON MovimientosInventario (Fecha);
CREATE INDEX IF NOT EXISTS IX_Combos_CodigoBarras ON Combos (CodigoBarras);
CREATE INDEX IF NOT EXISTS IX_Combos_Nombre ON Combos (Nombre);
CREATE INDEX IF NOT EXISTS IX_ComboDetalles_ComboId ON ComboDetalles (ComboId);
CREATE INDEX IF NOT EXISTS IX_ComboDetalles_ProductoId ON ComboDetalles (ProductoId);";

    private const string SeedDataSql = @"
INSERT INTO Categorias (Id, Nombre, Descripcion, Activa)
SELECT 1, 'General', 'Categoria predeterminada para productos sin clasificar', 1
WHERE NOT EXISTS (SELECT 1 FROM Categorias WHERE Id = 1);

INSERT INTO Proveedores (Id, RazonSocial, NombreComercial, Documento, Telefono, Email, Direccion, Notas, Activo)
SELECT 1, 'Distribuidora Dulce Sur', 'Dulce Sur', 'PROV-0001', '+54 351 1234567', 'ventas@dulcesur.local', 'Cordoba, Argentina', 'Proveedor principal de golosinas', 1
WHERE NOT EXISTS (SELECT 1 FROM Proveedores WHERE Id = 1);

INSERT INTO Productos (
    Id,
    CategoriaId,
    ProveedorId,
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
SELECT
    1,
    1,
    1,
    'ALF-BLOCK',
    '368264923997',
    'alfajor block',
    'Alfajor block unidad',
    1500,
    2200,
    15,
    5,
    100,
    'UNITARIO',
    0,
    1000,
    1
WHERE NOT EXISTS (SELECT 1 FROM Productos WHERE Id = 1);

INSERT INTO Compras (
    Id,
    ProveedorId,
    NumeroComprobante,
    Fecha,
    Usuario,
    Subtotal,
    Descuento,
    Impuesto,
    Total,
    Estado,
    Observaciones)
SELECT
    1,
    1,
    '2026-06-30-001',
    '2026-06-30 10:00:00',
    'Alejandro',
    15000,
    0,
    0,
    15000,
    'Registrada',
    'Compra inicial de ejemplo'
WHERE NOT EXISTS (SELECT 1 FROM Compras WHERE Id = 1);

INSERT INTO DetallesCompra (
    Id,
    CompraId,
    ProductoId,
    ProductoCodigo,
    CodigoBarras,
    ProductoNombre,
    Unidad,
    CostoUnitario,
    Cantidad,
    Subtotal)
SELECT
    1,
    1,
    1,
    'ALF-BLOCK',
    '368264923997',
    'alfajor block',
    'UNITARIO',
    1500,
    10,
    15000
WHERE NOT EXISTS (SELECT 1 FROM DetallesCompra WHERE Id = 1);

INSERT INTO MovimientosInventario (
    Id,
    ProductoId,
    CompraId,
    Fecha,
    Tipo,
    Cantidad,
    StockAnterior,
    StockNuevo,
    Usuario,
    Observaciones)
SELECT
    1,
    1,
    1,
    '2026-06-30 10:00:00',
    'Compra',
    10,
    5,
    15,
    'Alejandro',
    'Ingreso por compra 2026-06-30-001'
WHERE NOT EXISTS (SELECT 1 FROM MovimientosInventario WHERE Id = 1);";
}
