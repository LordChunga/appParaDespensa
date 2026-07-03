#nullable enable

using System.Globalization;
using System.IO;
using System.Text;
using Dapper;
using PosLocal.Data;

namespace PosLocal.Services;

public sealed class ProductoCatalogoService : IProductoCatalogoService
{
    private static readonly CultureInfo CsvCulture = CultureInfo.GetCultureInfo("es-AR");
    private readonly IPosDbContext _dbContext;

    public ProductoCatalogoService(IPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductoCatalogoDto>> BuscarProductosAsync(
        ProductoCatalogoQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        var orderBy = BuildOrderBy(query.OrdenarPor, query.OrdenDescendente);
        var sql = ProductosBaseSql + @"
WHERE (
    @Search IS NULL
    OR p.Nombre LIKE @Search
    OR p.Codigo LIKE @Search
    OR p.CodigoBarras LIKE @Search
)
AND (@CategoriaId IS NULL OR p.CategoriaId = @CategoriaId)
AND (@ProveedorId IS NULL OR p.ProveedorId = @ProveedorId)
AND (@Activo IS NULL OR p.Activo = @Activo)
" + orderBy + ";";

        return (await connection.QueryAsync<ProductoCatalogoDto>(new CommandDefinition(
            sql,
            new
            {
                Search = BuildSearchTerm(query.TextoBusqueda),
                query.CategoriaId,
                query.ProveedorId,
                Activo = query.Activo is null ? (int?)null : query.Activo.Value ? 1 : 0
            },
            cancellationToken: cancellationToken))).AsList();
    }

    public async Task<int> GuardarProductoAsync(
        ProductoUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateProducto(request);

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        if (request.Id is null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                InsertProductoSql,
                ToProductoParameters(request),
                cancellationToken: cancellationToken));

            return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT last_insert_rowid();",
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            UpdateProductoSql,
            ToProductoParameters(request),
            cancellationToken: cancellationToken));

        return request.Id.Value;
    }

    public async Task CambiarEstadoProductoAsync(
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

    public async Task ExportarProductosCsvAsync(
        ProductoCatalogoQuery query,
        string rutaDestino,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rutaDestino))
        {
            throw new ArgumentException("La ruta de exportacion es obligatoria.", nameof(rutaDestino));
        }

        var productos = await BuscarProductosAsync(query, cancellationToken);
        var directory = Path.GetDirectoryName(rutaDestino);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(rutaDestino, BuildProductosCsv(productos), Encoding.UTF8, cancellationToken);
    }

    public async Task<ProductoCsvImportResult> ImportarProductosCsvAsync(
        string rutaOrigen,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rutaOrigen) || !File.Exists(rutaOrigen))
        {
            throw new FileNotFoundException("No se encontro el archivo CSV seleccionado.", rutaOrigen);
        }

        var lines = await File.ReadAllLinesAsync(rutaOrigen, Encoding.UTF8, cancellationToken);
        if (lines.Length == 0)
        {
            return new ProductoCsvImportResult(0, 0, 0, new[] { "El archivo CSV esta vacio." });
        }

        var header = ParseCsvLine(lines[0]);
        var index = BuildHeaderIndex(header);
        var required = new[] { "codigo de barras", "nombre", "categoria", "stock", "precio costo", "precio venta" };
        var missing = required.Where(column => !index.ContainsKey(column)).ToList();
        if (missing.Count > 0)
        {
            return new ProductoCsvImportResult(0, 0, 0, new[] { $"Faltan columnas requeridas: {string.Join(", ", missing)}." });
        }

        var errors = new List<string>();
        var seenBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var insertados = 0;
        var actualizados = 0;
        var omitidos = 0;

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        for (var lineNumber = 2; lineNumber <= lines.Length; lineNumber++)
        {
            var values = ParseCsvLine(lines[lineNumber - 1]);
            if (values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            try
            {
                var barcode = GetRequired(values, index, "codigo de barras");
                var name = GetRequired(values, index, "nombre");
                var categoryName = GetRequired(values, index, "categoria");
                var stock = ParseDecimal(GetRequired(values, index, "stock"), "Stock");
                var cost = ParseDecimal(GetRequired(values, index, "precio costo"), "Precio Costo");
                var salePrice = ParseDecimal(GetRequired(values, index, "precio venta"), "Precio Venta");
                var providerName = GetOptional(values, index, "proveedor");
                var code = GetOptional(values, index, "codigo") ?? barcode;
                var unit = GetOptional(values, index, "unidad") ?? "UNITARIO";

                if (!seenBarcodes.Add(barcode))
                {
                    omitidos++;
                    errors.Add($"Linea {lineNumber}: codigo de barras duplicado en el archivo ({barcode}).");
                    continue;
                }

                var categoriaId = await GetOrCreateCategoriaAsync(connection, transaction, categoryName, cancellationToken);
                int? proveedorId = string.IsNullOrWhiteSpace(providerName)
                    ? null
                    : await GetOrCreateProveedorAsync(connection, transaction, providerName, cancellationToken);

                var existingId = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
                    "SELECT Id FROM Productos WHERE CodigoBarras = @CodigoBarras;",
                    new { CodigoBarras = barcode },
                    transaction,
                    cancellationToken: cancellationToken));

                var request = new ProductoUpsertRequest(
                    existingId,
                    code,
                    barcode,
                    name,
                    null,
                    categoriaId,
                    proveedorId,
                    stock,
                    0m,
                    Math.Max(100m, stock),
                    unit,
                    cost,
                    salePrice,
                    false,
                    1000m,
                    true);

                if (existingId is null)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        InsertProductoSql,
                        ToProductoParameters(request),
                        transaction,
                        cancellationToken: cancellationToken));
                    insertados++;
                }
                else
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        UpdateProductoSql,
                        ToProductoParameters(request),
                        transaction,
                        cancellationToken: cancellationToken));
                    actualizados++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                omitidos++;
                errors.Add($"Linea {lineNumber}: {ex.Message}");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new ProductoCsvImportResult(insertados, actualizados, omitidos, errors);
    }

    public async Task<IReadOnlyList<CategoriaCatalogoDto>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<CategoriaCatalogoDto>(new CommandDefinition(
            CategoriasSql,
            cancellationToken: cancellationToken))).AsList();
    }

    public async Task<int> GuardarCategoriaAsync(
        CategoriaUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new ArgumentException("El nombre de la categoria es obligatorio.");
        }

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        if (request.Id is null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Categorias (Nombre, Descripcion, Activa) VALUES (@Nombre, @Descripcion, @Activa);",
                new { request.Nombre, request.Descripcion, Activa = request.Activa ? 1 : 0 },
                cancellationToken: cancellationToken));

            return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT last_insert_rowid();",
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Categorias SET Nombre = @Nombre, Descripcion = @Descripcion, Activa = @Activa, FechaActualizacion = CURRENT_TIMESTAMP WHERE Id = @Id;",
            new { request.Id, request.Nombre, request.Descripcion, Activa = request.Activa ? 1 : 0 },
            cancellationToken: cancellationToken));

        return request.Id.Value;
    }

    public async Task EliminarCategoriaAsync(
        int categoriaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        var assigned = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM Productos WHERE CategoriaId = @CategoriaId;",
            new { CategoriaId = categoriaId },
            cancellationToken: cancellationToken));

        if (assigned > 0)
        {
            throw new InvalidOperationException("No se puede eliminar una categoria con productos asignados.");
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Categorias WHERE Id = @CategoriaId;",
            new { CategoriaId = categoriaId },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ProveedorCatalogoDto>> ObtenerProveedoresAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<ProveedorCatalogoDto>(new CommandDefinition(
            ProveedoresSql,
            cancellationToken: cancellationToken))).AsList();
    }

    public async Task<int> GuardarProveedorAsync(
        ProveedorUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new ArgumentException("El nombre del proveedor es obligatorio.");
        }

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);

        if (request.Id is null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Proveedores (RazonSocial, NombreComercial, Telefono, Email, Notas, Activo) VALUES (@Nombre, @Nombre, @Telefono, @Email, @Notas, @Activo);",
                new { request.Nombre, request.Telefono, request.Email, request.Notas, Activo = request.Activo ? 1 : 0 },
                cancellationToken: cancellationToken));

            return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT last_insert_rowid();",
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Proveedores SET RazonSocial = @Nombre, NombreComercial = @Nombre, Telefono = @Telefono, Email = @Email, Notas = @Notas, Activo = @Activo, FechaActualizacion = CURRENT_TIMESTAMP WHERE Id = @Id;",
            new { request.Id, request.Nombre, request.Telefono, request.Email, request.Notas, Activo = request.Activo ? 1 : 0 },
            cancellationToken: cancellationToken));

        return request.Id.Value;
    }

    public async Task EliminarProveedorAsync(
        int proveedorId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        var assigned = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM Productos WHERE ProveedorId = @ProveedorId;",
            new { ProveedorId = proveedorId },
            cancellationToken: cancellationToken));

        if (assigned > 0)
        {
            throw new InvalidOperationException("No se puede eliminar un proveedor con productos asignados.");
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Proveedores WHERE Id = @ProveedorId;",
            new { ProveedorId = proveedorId },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ComboProductoDisponibleDto>> BuscarProductosParaComboAsync(
        string? textoBusqueda,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<ComboProductoDisponibleDto>(new CommandDefinition(
            @"
SELECT
    Id AS ProductoId,
    Nombre,
    Codigo,
    CodigoBarras,
    PrecioVenta,
    StockActual,
    UnidadMedida
FROM Productos
WHERE Activo = 1
AND (
    @Search IS NULL
    OR Nombre LIKE @Search
    OR Codigo LIKE @Search
    OR CodigoBarras LIKE @Search
)
ORDER BY Nombre;",
            new { Search = BuildSearchTerm(textoBusqueda) },
            cancellationToken: cancellationToken))).AsList();
    }

    public async Task<int> GuardarComboAsync(
        ComboUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            throw new ArgumentException("El codigo del combo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new ArgumentException("El nombre del combo es obligatorio.");
        }

        if (request.Detalles.Count == 0)
        {
            throw new ArgumentException("El combo debe tener al menos un producto.");
        }

        await using var connection = await _dbContext.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        int comboId;
        if (request.Id is null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                @"
INSERT INTO Combos (Codigo, CodigoBarras, Nombre, PrecioSugerido, PrecioCombo, Activo)
VALUES (@Codigo, @CodigoBarras, @Nombre, @PrecioSugerido, @PrecioCombo, @Activo);",
                new
                {
                    request.Codigo,
                    request.CodigoBarras,
                    request.Nombre,
                    request.PrecioSugerido,
                    request.PrecioCombo,
                    Activo = request.Activo ? 1 : 0
                },
                transaction,
                cancellationToken: cancellationToken));

            comboId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT last_insert_rowid();",
                transaction: transaction,
                cancellationToken: cancellationToken));
        }
        else
        {
            comboId = request.Id.Value;
            await connection.ExecuteAsync(new CommandDefinition(
                @"
UPDATE Combos
SET Codigo = @Codigo,
    CodigoBarras = @CodigoBarras,
    Nombre = @Nombre,
    PrecioSugerido = @PrecioSugerido,
    PrecioCombo = @PrecioCombo,
    Activo = @Activo,
    FechaActualizacion = CURRENT_TIMESTAMP
WHERE Id = @Id;",
                new
                {
                    Id = comboId,
                    request.Codigo,
                    request.CodigoBarras,
                    request.Nombre,
                    request.PrecioSugerido,
                    request.PrecioCombo,
                    Activo = request.Activo ? 1 : 0
                },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM ComboDetalles WHERE ComboId = @ComboId;",
                new { ComboId = comboId },
                transaction,
                cancellationToken: cancellationToken));
        }

        foreach (var detail in request.Detalles)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                @"
INSERT INTO ComboDetalles (ComboId, ProductoId, Cantidad, PrecioUnitarioSnapshot)
VALUES (@ComboId, @ProductoId, @Cantidad, @PrecioUnitarioSnapshot);",
                new
                {
                    ComboId = comboId,
                    detail.ProductoId,
                    detail.Cantidad,
                    detail.PrecioUnitarioSnapshot
                },
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
        return comboId;
    }

    private static string BuildOrderBy(string? column, bool desc)
    {
        var selectedColumn = column?.Trim().ToLowerInvariant() switch
        {
            "nombre" => "p.Nombre",
            "codigo" => "p.Codigo",
            "codigobarras" => "p.CodigoBarras",
            "categoria" => "c.Nombre",
            "stock" => "p.StockActual",
            "precio" => "p.PrecioVenta",
            "proveedor" => "pr.RazonSocial",
            _ => "p.Nombre"
        };

        return $"ORDER BY {selectedColumn} {(desc ? "DESC" : "ASC")}";
    }

    private static string? BuildSearchTerm(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? null
            : $"%{text.Trim()}%";
    }

    private static object ToProductoParameters(ProductoUpsertRequest request)
    {
        return new
        {
            request.Id,
            request.Codigo,
            request.CodigoBarras,
            request.Nombre,
            request.Descripcion,
            request.CategoriaId,
            request.ProveedorId,
            request.StockActual,
            request.StockMinimo,
            request.StockMaximo,
            request.UnidadMedida,
            request.PrecioCosto,
            request.PrecioVenta,
            VentaPorPeso = request.VentaPorPeso ? 1 : 0,
            request.PesoBaseGramos,
            Activo = request.Activo ? 1 : 0
        };
    }

    private static void ValidateProducto(ProductoUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            throw new ArgumentException("El codigo del producto es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new ArgumentException("El nombre del producto es obligatorio.");
        }

        if (request.CategoriaId <= 0)
        {
            throw new ArgumentException("Debe seleccionar una categoria.");
        }

        if (request.StockActual < 0m || request.PrecioCosto < 0m || request.PrecioVenta < 0m)
        {
            throw new ArgumentException("Stock y precios no pueden ser negativos.");
        }
    }

    private static string BuildProductosCsv(IReadOnlyList<ProductoCatalogoDto> productos)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Codigo de Barras,Nombre,Categoria,Stock,Precio Costo,Precio Venta,Proveedor,Codigo,Unidad,Activo");

        foreach (var producto in productos)
        {
            builder
                .Append(EscapeCsv(producto.CodigoBarras)).Append(',')
                .Append(EscapeCsv(producto.Nombre)).Append(',')
                .Append(EscapeCsv(producto.CategoriaNombre)).Append(',')
                .Append(producto.StockActual.ToString(CsvCulture)).Append(',')
                .Append(producto.PrecioCosto.ToString(CsvCulture)).Append(',')
                .Append(producto.PrecioVenta.ToString(CsvCulture)).Append(',')
                .Append(EscapeCsv(producto.ProveedorNombre)).Append(',')
                .Append(EscapeCsv(producto.Codigo)).Append(',')
                .Append(EscapeCsv(producto.UnidadMedida)).Append(',')
                .Append(producto.Activo ? "Activo" : "Inactivo")
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

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private static Dictionary<string, int> BuildHeaderIndex(IReadOnlyList<string> header)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
        {
            result[NormalizeHeader(header[i])] = i;
        }

        return result;
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace("á", "a", StringComparison.Ordinal)
            .Replace("é", "e", StringComparison.Ordinal)
            .Replace("í", "i", StringComparison.Ordinal)
            .Replace("ó", "o", StringComparison.Ordinal)
            .Replace("ú", "u", StringComparison.Ordinal);

        return normalized;
    }

    private static string GetRequired(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> index, string column)
    {
        var value = GetOptional(values, index, column);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"La columna '{column}' es obligatoria.");
        }

        return value;
    }

    private static string? GetOptional(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> index, string column)
    {
        var normalized = NormalizeHeader(column);
        return index.TryGetValue(normalized, out var columnIndex) && columnIndex < values.Count
            ? values[columnIndex]
            : null;
    }

    private static decimal ParseDecimal(string value, string column)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CsvCulture, out var result)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        throw new FormatException($"{column} tiene un valor numerico invalido.");
    }

    private static async Task<int> GetOrCreateCategoriaAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        string name,
        CancellationToken cancellationToken)
    {
        var existingId = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT Id FROM Categorias WHERE Nombre = @Nombre;",
            new { Nombre = name },
            transaction,
            cancellationToken: cancellationToken));

        if (existingId is not null)
        {
            return existingId.Value;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Categorias (Nombre, Activa) VALUES (@Nombre, 1);",
            new { Nombre = name },
            transaction,
            cancellationToken: cancellationToken));

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT last_insert_rowid();",
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task<int> GetOrCreateProveedorAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        string name,
        CancellationToken cancellationToken)
    {
        var existingId = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT Id FROM Proveedores WHERE RazonSocial = @Nombre;",
            new { Nombre = name },
            transaction,
            cancellationToken: cancellationToken));

        if (existingId is not null)
        {
            return existingId.Value;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Proveedores (RazonSocial, NombreComercial, Activo) VALUES (@Nombre, @Nombre, 1);",
            new { Nombre = name },
            transaction,
            cancellationToken: cancellationToken));

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT last_insert_rowid();",
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private const string ProductosBaseSql = @"
SELECT
    p.Id,
    p.Codigo,
    p.CodigoBarras,
    p.Nombre,
    c.Nombre AS CategoriaNombre,
    p.CategoriaId,
    p.StockActual,
    p.StockMinimo,
    p.StockMaximo,
    p.UnidadMedida,
    p.PrecioCosto,
    p.PrecioVenta,
    pr.RazonSocial AS ProveedorNombre,
    p.ProveedorId,
    p.Activo,
    p.VentaPorPeso,
    p.PesoBaseGramos
FROM Productos p
INNER JOIN Categorias c ON c.Id = p.CategoriaId
LEFT JOIN Proveedores pr ON pr.Id = p.ProveedorId
";

    private const string InsertProductoSql = @"
INSERT INTO Productos (
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
VALUES (
    @CategoriaId,
    @ProveedorId,
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
    @Activo);";

    private const string UpdateProductoSql = @"
UPDATE Productos
SET CategoriaId = @CategoriaId,
    ProveedorId = @ProveedorId,
    Codigo = @Codigo,
    CodigoBarras = @CodigoBarras,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    PrecioCosto = @PrecioCosto,
    PrecioVenta = @PrecioVenta,
    StockActual = @StockActual,
    StockMinimo = @StockMinimo,
    StockMaximo = @StockMaximo,
    UnidadMedida = @UnidadMedida,
    VentaPorPeso = @VentaPorPeso,
    PesoBaseGramos = @PesoBaseGramos,
    Activo = @Activo,
    FechaActualizacion = CURRENT_TIMESTAMP
WHERE Id = @Id;";

    private const string CategoriasSql = @"
SELECT
    c.Id,
    c.Nombre,
    c.Descripcion,
    c.Activa,
    COUNT(p.Id) AS ProductosAsignados
FROM Categorias c
LEFT JOIN Productos p ON p.CategoriaId = c.Id
GROUP BY c.Id, c.Nombre, c.Descripcion, c.Activa
ORDER BY c.Nombre;";

    private const string ProveedoresSql = @"
SELECT
    pr.Id,
    pr.RazonSocial AS Nombre,
    pr.Telefono,
    pr.Email,
    pr.Notas,
    pr.Activo,
    COUNT(p.Id) AS ProductosAsignados
FROM Proveedores pr
LEFT JOIN Productos p ON p.ProveedorId = pr.Id
GROUP BY pr.Id, pr.RazonSocial, pr.Telefono, pr.Email, pr.Notas, pr.Activo
ORDER BY pr.RazonSocial;";
}
