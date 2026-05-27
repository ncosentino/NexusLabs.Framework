using System;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using NexusLabs.Framework;
using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql;

/// <summary>
/// Diagnostics decorator that records the call stack and timestamp of every successful
/// <see cref="IAsyncDbConnection.OpenAsync(CancellationToken)"/> call, and removes the entry
/// on close or dispose. Failed opens do not leave entries behind.
/// </summary>
/// <remarks>
/// Used to debug "all connections in the pool are busy" scenarios by capturing where each
/// still-open connection was originally opened from. Opt in at construction time; debug-vs-release
/// is the consumer's choice rather than baked in via <c>#if DEBUG</c>.
/// </remarks>
public sealed class OpenTrackingDecorator : IAsyncDbConnection
{
    [TransfersOwnership]
    private readonly IAsyncDbConnection _inner;
    private readonly OpenConnectionTracker _tracker;
    private readonly TimeProvider _timeProvider;
    private readonly Guid _id = Guid.NewGuid();
    private int _registered;
    private int _disposed;

    /// <summary>
    /// Creates a tracking decorator around <paramref name="inner"/> that timestamps each
    /// open via <paramref name="timeProvider"/>.
    /// </summary>
    public OpenTrackingDecorator(
        IAsyncDbConnection inner,
        OpenConnectionTracker tracker,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(tracker);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _inner = inner;
        _tracker = tracker;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    [AllowNull]
    public string ConnectionString
    {
        get => _inner.ConnectionString;
        set => _inner.ConnectionString = value!;
    }

    /// <inheritdoc />
    public int ConnectionTimeout => _inner.ConnectionTimeout;

    /// <inheritdoc />
    public string Database => _inner.Database;

    /// <inheritdoc />
    public ConnectionState State => _inner.State;

    /// <inheritdoc />
    public IDbTransaction BeginTransaction() => _inner.BeginTransaction();

    /// <inheritdoc />
    public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);

    /// <inheritdoc />
    public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

    /// <inheritdoc />
    public void Close()
    {
        _inner.Close();
        Unregister();
    }

    /// <inheritdoc />
    public IAsyncDbCommand CreateAsyncCommand() => _inner.CreateAsyncCommand();

    /// <inheritdoc />
    public IDbCommand CreateCommand() => _inner.CreateCommand();

    /// <inheritdoc />
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            Unregister();
        }
    }

    /// <inheritdoc />
    public void Open() => OpenAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task OpenAsync() => OpenAsync(CancellationToken.None);

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        await _inner.OpenAsync(cancellationToken).ConfigureAwait(false);

        var entry = new OpenConnectionEntry(
            new StackTrace(fNeedFileInfo: true).ToString(),
            _timeProvider.GetUtcNow());
        _tracker.Register(_id, entry);
        Interlocked.Exchange(ref _registered, 1);
    }

    private void Unregister()
    {
        if (Interlocked.Exchange(ref _registered, 0) == 1)
        {
            _tracker.Unregister(_id);
        }
    }
}
