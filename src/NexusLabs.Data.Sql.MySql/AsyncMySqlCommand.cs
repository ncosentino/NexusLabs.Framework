using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using NexusLabs.Framework;
using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql.MySql;

/// <summary>
/// Internal adapter that wraps a <see cref="MySqlCommand"/> and exposes it as an
/// <see cref="IAsyncDbCommand"/>. Every async overload delegates to the underlying command's
/// own async path - never falls through to a sync-over-async base.
/// </summary>
internal sealed class AsyncMySqlCommand : IAsyncDbCommand
{
    [TransfersOwnership]
    private readonly MySqlCommand _command;

    public AsyncMySqlCommand(MySqlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _command = command;
    }

    [AllowNull]
    public string CommandText
    {
        get => _command.CommandText;
        set => _command.CommandText = value!;
    }

    public int CommandTimeout
    {
        get => _command.CommandTimeout;
        set => _command.CommandTimeout = value;
    }

    public CommandType CommandType
    {
        get => _command.CommandType;
        set => _command.CommandType = value;
    }

    public IDbConnection? Connection
    {
        get => _command.Connection;
        set => _command.Connection = (MySqlConnection?)value;
    }

    public IDataParameterCollection Parameters => _command.Parameters;

    public IDbTransaction? Transaction
    {
        get => _command.Transaction;
        set => _command.Transaction = (MySqlTransaction?)value;
    }

    public UpdateRowSource UpdatedRowSource
    {
        get => _command.UpdatedRowSource;
        set => _command.UpdatedRowSource = value;
    }

    public void Cancel() => _command.Cancel();

    public IDbDataParameter CreateParameter() => _command.CreateParameter();

    public void Dispose() => _command.Dispose();
    public ValueTask DisposeAsync() => _command.DisposeAsync();

    public int ExecuteNonQuery() => _command.ExecuteNonQuery();
    public Task<int> ExecuteNonQueryAsync() => _command.ExecuteNonQueryAsync();
    public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
        _command.ExecuteNonQueryAsync(cancellationToken);

    public IDataReader ExecuteReader() => _command.ExecuteReader();
    public IDataReader ExecuteReader(CommandBehavior behavior) => _command.ExecuteReader(behavior);

    public async Task<IAsyncDbDataReader> ExecuteReaderAsync()
    {
        var reader = await _command
            .ExecuteReaderAsync()
            .ConfigureAwait(false);
        return new AsyncMySqlDataReader((MySqlDataReader)reader);
    }

    public async Task<IAsyncDbDataReader> ExecuteReaderAsync(CommandBehavior commandBehavior)
    {
        var reader = await _command
            .ExecuteReaderAsync(commandBehavior)
            .ConfigureAwait(false);
        return new AsyncMySqlDataReader((MySqlDataReader)reader);
    }

    public async Task<IAsyncDbDataReader> ExecuteReaderAsync(
        CommandBehavior commandBehavior,
        CancellationToken cancellationToken)
    {
        var reader = await _command
            .ExecuteReaderAsync(commandBehavior, cancellationToken)
            .ConfigureAwait(false);
        return new AsyncMySqlDataReader((MySqlDataReader)reader);
    }

    public async Task<IAsyncDbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
    {
        var reader = await _command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        return new AsyncMySqlDataReader((MySqlDataReader)reader);
    }

    public object? ExecuteScalar() => _command.ExecuteScalar();
    public async Task<object> ExecuteScalarAsync()
    {
        var result = await _command.ExecuteScalarAsync().ConfigureAwait(false);
        return result!;
    }
    public async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        var result = await _command
            .ExecuteScalarAsync(cancellationToken)
            .ConfigureAwait(false);
        return result!;
    }

    public void Prepare() => _command.Prepare();
}
