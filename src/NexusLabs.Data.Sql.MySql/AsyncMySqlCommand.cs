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
/// Internal adapter that wraps a <see cref="MySqlCommand"/> and exposes it as a
/// <see cref="DbCommand"/> and <see cref="IAsyncDbCommand"/>. Every async override delegates to
/// the provider's native asynchronous path rather than using the base sync-over-async fallback.
/// </summary>
internal sealed class AsyncMySqlCommand :
    DbCommand,
    IAsyncDbCommand
{
    [TransfersOwnership]
    private readonly MySqlCommand _command;
    private AsyncMySqlConnection? _connection;
    private int _disposed;

    public AsyncMySqlCommand(
        MySqlCommand command,
        AsyncMySqlConnection? connection = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        _command = command;
        _connection = connection;
    }

    [AllowNull]
    public override string CommandText
    {
        get => _command.CommandText;
        set => _command.CommandText = value!;
    }

    public override int CommandTimeout
    {
        get => _command.CommandTimeout;
        set => _command.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _command.CommandType;
        set => _command.CommandType = value;
    }

    public override bool DesignTimeVisible
    {
        get => _command.DesignTimeVisible;
        set => _command.DesignTimeVisible = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _command.UpdatedRowSource;
        set => _command.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection is not null
            ? _connection
            : _command.Connection;
        set
        {
            switch (value)
            {
                case null:
                    _connection = null;
                    _command.Connection = null;
                    break;
                case AsyncMySqlConnection connection:
                    _connection = connection;
                    _command.Connection = connection.InnerConnection;
                    break;
                case MySqlConnection connection:
                    _connection = null;
                    _command.Connection = connection;
                    break;
                default:
                    throw new ArgumentException(
                        $"Connection must be a {nameof(MySqlConnection)} or " +
                        $"{nameof(AsyncMySqlConnection)}.",
                        nameof(value));
            }
        }
    }

    protected override DbParameterCollection DbParameterCollection =>
        _command.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => _command.Transaction;
        set => _command.Transaction = value switch
        {
            null => null,
            MySqlTransaction transaction => transaction,
            _ => throw new ArgumentException(
                $"Transaction must be a {nameof(MySqlTransaction)}.",
                nameof(value)),
        };
    }

    public override void Cancel() => _command.Cancel();

    public override int ExecuteNonQuery() => _command.ExecuteNonQuery();

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
        _command.ExecuteNonQueryAsync(cancellationToken);

    public override object? ExecuteScalar() => _command.ExecuteScalar();

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) =>
        _command.ExecuteScalarAsync(cancellationToken);

    public override void Prepare() => _command.Prepare();

    public override Task PrepareAsync(CancellationToken cancellationToken) =>
        _command.PrepareAsync(cancellationToken);

    protected override DbParameter CreateDbParameter() =>
        _command.CreateParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        _command.ExecuteReader(behavior);

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken) =>
        await _command
            .ExecuteReaderAsync(behavior, cancellationToken)
            .ConfigureAwait(false);

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _command.Dispose();
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

        try
        {
            await _command.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    async Task<IAsyncDbDataReader> IAsyncDbCommand.ExecuteReaderAsync() =>
        new AsyncMySqlDataReader((MySqlDataReader)await _command
            .ExecuteReaderAsync()
            .ConfigureAwait(false));

    async Task<IAsyncDbDataReader> IAsyncDbCommand.ExecuteReaderAsync(
        CommandBehavior commandBehavior) =>
        new AsyncMySqlDataReader((MySqlDataReader)await _command
            .ExecuteReaderAsync(commandBehavior)
            .ConfigureAwait(false));

    async Task<IAsyncDbDataReader> IAsyncDbCommand.ExecuteReaderAsync(
        CommandBehavior commandBehavior,
        CancellationToken cancellationToken) =>
        new AsyncMySqlDataReader((MySqlDataReader)await _command
            .ExecuteReaderAsync(commandBehavior, cancellationToken)
            .ConfigureAwait(false));

    async Task<IAsyncDbDataReader> IAsyncDbCommand.ExecuteReaderAsync(
        CancellationToken cancellationToken) =>
        new AsyncMySqlDataReader((MySqlDataReader)await _command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false));

    async Task<object> IAsyncDbCommand.ExecuteScalarAsync() =>
        (await _command.ExecuteScalarAsync().ConfigureAwait(false))!;

    async Task<object> IAsyncDbCommand.ExecuteScalarAsync(
        CancellationToken cancellationToken) =>
        (await _command
            .ExecuteScalarAsync(cancellationToken)
            .ConfigureAwait(false))!;
}
