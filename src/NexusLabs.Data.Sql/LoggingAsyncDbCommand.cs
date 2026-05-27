using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NexusLabs.Framework;
using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql;

/// <summary>
/// An <see cref="IAsyncDbCommand"/> decorator that logs command execution via
/// <see cref="ILogger"/>. Logs command metadata (type, text length) by default; full command
/// text is included only when <see cref="LoggingAsyncDbCommandOptions.IncludeCommandText"/>
/// is set to <c>true</c>. This avoids accidental disclosure of inlined parameter values in
/// production logs.
/// </summary>
public sealed class LoggingAsyncDbCommand : IAsyncDbCommand
{
    [TransfersOwnership]
    private readonly IAsyncDbCommand _inner;
    private readonly ILogger _logger;
    private readonly LoggingAsyncDbCommandOptions _options;

    /// <summary>Creates a logging decorator around <paramref name="inner"/>.</summary>
    public LoggingAsyncDbCommand(
        IAsyncDbCommand inner,
        ILogger logger,
        LoggingAsyncDbCommandOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _logger = logger;
        _options = options ?? new LoggingAsyncDbCommandOptions();
    }

    /// <inheritdoc />
    [AllowNull]
    public string CommandText
    {
        get => _inner.CommandText;
        set => _inner.CommandText = value!;
    }

    /// <inheritdoc />
    public int CommandTimeout
    {
        get => _inner.CommandTimeout;
        set => _inner.CommandTimeout = value;
    }

    /// <inheritdoc />
    public CommandType CommandType
    {
        get => _inner.CommandType;
        set => _inner.CommandType = value;
    }

    /// <inheritdoc />
    public IDbConnection? Connection
    {
        get => _inner.Connection;
        set => _inner.Connection = value;
    }

    /// <inheritdoc />
    public IDataParameterCollection Parameters => _inner.Parameters;

    /// <inheritdoc />
    public IDbTransaction? Transaction
    {
        get => _inner.Transaction;
        set => _inner.Transaction = value;
    }

    /// <inheritdoc />
    public UpdateRowSource UpdatedRowSource
    {
        get => _inner.UpdatedRowSource;
        set => _inner.UpdatedRowSource = value;
    }

    /// <inheritdoc />
    public void Cancel() => _inner.Cancel();

    /// <inheritdoc />
    public IDbDataParameter CreateParameter() => _inner.CreateParameter();

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _inner.DisposeAsync();

    /// <inheritdoc />
    public int ExecuteNonQuery() => _inner.ExecuteNonQuery();

    /// <inheritdoc />
    public Task<int> ExecuteNonQueryAsync() => ExecuteNonQueryAsync(CancellationToken.None);

    /// <inheritdoc />
    public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        Log("ExecuteNonQueryAsync");
        return _inner.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IDataReader ExecuteReader() => _inner.ExecuteReader();

    /// <inheritdoc />
    public IDataReader ExecuteReader(CommandBehavior behavior) => _inner.ExecuteReader(behavior);

    /// <inheritdoc />
    public Task<IAsyncDbDataReader> ExecuteReaderAsync() => ExecuteReaderAsync(CancellationToken.None);

    /// <inheritdoc />
    public Task<IAsyncDbDataReader> ExecuteReaderAsync(CommandBehavior commandBehavior) =>
        ExecuteReaderAsync(commandBehavior, CancellationToken.None);

    /// <inheritdoc />
    public Task<IAsyncDbDataReader> ExecuteReaderAsync(
        CommandBehavior commandBehavior,
        CancellationToken cancellationToken)
    {
        Log("ExecuteReaderAsync");
        return _inner.ExecuteReaderAsync(commandBehavior, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IAsyncDbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
    {
        Log("ExecuteReaderAsync");
        return _inner.ExecuteReaderAsync(cancellationToken);
    }

    /// <inheritdoc />
    public object? ExecuteScalar() => _inner.ExecuteScalar();

    /// <inheritdoc />
    public Task<object> ExecuteScalarAsync() => ExecuteScalarAsync(CancellationToken.None);

    /// <inheritdoc />
    public Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        Log("ExecuteScalarAsync");
        return _inner.ExecuteScalarAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Prepare() => _inner.Prepare();

#pragma warning disable CA2254 // Template should be a static expression. Suppressed: the message template branches based on options; both branches use compile-time-constant templates.
    private void Log(string operation)
    {
        if (!_logger.IsEnabled(_options.LogLevel))
        {
            return;
        }

        if (_options.IncludeCommandText)
        {
            _logger.Log(
                _options.LogLevel,
                "DB {Operation}: {CommandText}",
                operation,
                _inner.CommandText);
        }
        else
        {
            _logger.Log(
                _options.LogLevel,
                "DB {Operation} (CommandTextLength={CommandTextLength})",
                operation,
                _inner.CommandText?.Length ?? 0);
        }
    }
#pragma warning restore CA2254
}
