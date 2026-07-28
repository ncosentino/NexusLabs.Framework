using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using NexusLabs.Framework;
using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql.MySql;

/// <summary>
/// Internal adapter that wraps a <see cref="MySqlConnection"/> and exposes it as an
/// <see cref="DbConnection"/> and <see cref="IAsyncDbConnection"/>. Commands created via this
/// connection preserve the standard <see cref="DbCommand"/> runtime contract while delegating
/// provider-native asynchronous operations.
/// </summary>
internal sealed class AsyncMySqlConnection :
    DbConnection,
    IAsyncDbConnection
{
    [TransfersOwnership]
    private readonly MySqlConnection _connection;
    private int _disposed;

    public AsyncMySqlConnection(MySqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _connection.StateChange += HandleStateChange;
    }

    internal MySqlConnection InnerConnection => _connection;

    [AllowNull]
    public override string ConnectionString
    {
        get => _connection.ConnectionString;
        set => _connection.ConnectionString = value!;
    }

    public override int ConnectionTimeout => _connection.ConnectionTimeout;

    public override string Database => _connection.Database;

    public override string DataSource => _connection.DataSource;

    public override string ServerVersion => _connection.ServerVersion;

    public override ConnectionState State => _connection.State;

    public override bool CanCreateBatch => _connection.CanCreateBatch;

    public override void ChangeDatabase(string databaseName) =>
        _connection.ChangeDatabase(databaseName);

    public override Task ChangeDatabaseAsync(
        string databaseName,
        CancellationToken cancellationToken) =>
        _connection.ChangeDatabaseAsync(databaseName, cancellationToken);

    public override void Close() => _connection.Close();

    public override Task CloseAsync() => _connection.CloseAsync();

    public IAsyncDbCommand CreateAsyncCommand() =>
        (IAsyncDbCommand)CreateDbCommand();

    IDbTransaction IDbConnection.BeginTransaction() =>
        _connection.BeginTransaction();

    IDbTransaction IDbConnection.BeginTransaction(IsolationLevel isolationLevel) =>
        isolationLevel == IsolationLevel.Unspecified
            ? _connection.BeginTransaction()
            : _connection.BeginTransaction(isolationLevel);

    public override void EnlistTransaction(System.Transactions.Transaction? transaction) =>
        _connection.EnlistTransaction(transaction);

    public override DataTable GetSchema() => _connection.GetSchema();

    public override DataTable GetSchema(string collectionName) =>
        _connection.GetSchema(collectionName);

    public override DataTable GetSchema(
        string collectionName,
        string?[] restrictionValues) =>
        _connection.GetSchema(collectionName, restrictionValues);

    public override Task<DataTable> GetSchemaAsync(CancellationToken cancellationToken) =>
        _connection.GetSchemaAsync(cancellationToken);

    public override Task<DataTable> GetSchemaAsync(
        string collectionName,
        CancellationToken cancellationToken) =>
        _connection.GetSchemaAsync(collectionName, cancellationToken);

    public override Task<DataTable> GetSchemaAsync(
        string collectionName,
        string?[] restrictionValues,
        CancellationToken cancellationToken) =>
        _connection.GetSchemaAsync(collectionName, restrictionValues, cancellationToken);

    public override void Open() => _connection.Open();

    public override Task OpenAsync(CancellationToken cancellationToken) =>
        _connection.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        isolationLevel == IsolationLevel.Unspecified
            ? _connection.BeginTransaction()
            : _connection.BeginTransaction(isolationLevel);

    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        if (isolationLevel == IsolationLevel.Unspecified)
        {
            return await _connection
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return await _connection
            .BeginTransactionAsync(isolationLevel, cancellationToken)
            .ConfigureAwait(false);
    }

    protected override DbBatch CreateDbBatch() => _connection.CreateBatch();

    protected override DbCommand CreateDbCommand() =>
        new AsyncMySqlCommand(_connection.CreateCommand(), this);

    protected override DbProviderFactory DbProviderFactory =>
        MySqlClientFactory.Instance;

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _connection.StateChange -= HandleStateChange;
                _connection.Dispose();
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _connection.StateChange -= HandleStateChange;
        try
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void HandleStateChange(object sender, StateChangeEventArgs e)
    {
        _ = sender;
        OnStateChange(e);
    }
}
