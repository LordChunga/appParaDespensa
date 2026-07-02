#nullable enable

using Microsoft.Data.Sqlite;

namespace PosLocal.Data;

public interface IPosDbContext
{
    string DatabasePath { get; }

    string ConnectionString { get; }

    Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);

    Task InitializeAsync(CancellationToken cancellationToken = default);
}
