using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql.MySql;

/// <summary>
/// Internal adapter that wraps a <see cref="MySqlConnection"/> and exposes it as an
/// <see cref="IAsyncDbConnection"/>. Commands created via this connection are wrapped with
/// <see cref="AsyncMySqlCommand"/> so their async paths are also properly delegated.
/// </summary>
internal sealed class AsyncMySqlConnection : IAsyncDbConnection
{
    private readonly MySqlConnection _connection;

    public AsyncMySqlConnection(MySqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    [AllowNull]
    public string ConnectionString
    {
        get => _connection.ConnectionString;
        set => _connection.ConnectionString = value!;
    }

    public int ConnectionTimeout => _connection.ConnectionTimeout;

    public string Database => _connection.Database;

    public ConnectionState State => _connection.State;

    public IDbTransaction BeginTransaction() => _connection.BeginTransaction();

    public IDbTransaction BeginTransaction(IsolationLevel il) => _connection.BeginTransaction(il);

    public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

    public void Close() => _connection.Close();

    public IAsyncDbCommand CreateAsyncCommand() => new AsyncMySqlCommand(_connection.CreateCommand());

    public IDbCommand CreateCommand() => CreateAsyncCommand();

    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP007:Don't dispose injected",
        Justification = "Adapter pattern: this internal type wraps the MySqlConnection and owns its " +
                        "lifetime for as long as the adapter is in use. Forwarding Dispose/DisposeAsync " +
                        "is the explicit adapter contract.")]
    public void Dispose() => _connection.Dispose();
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP007:Don't dispose injected",
        Justification = "Adapter pattern: this internal type wraps the MySqlConnection and owns its " +
                        "lifetime for as long as the adapter is in use. Forwarding Dispose/DisposeAsync " +
                        "is the explicit adapter contract.")]
    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    public void Open() => _connection.Open();
    public Task OpenAsync() => _connection.OpenAsync();
    public Task OpenAsync(CancellationToken cancellationToken) =>
        _connection.OpenAsync(cancellationToken);
}
