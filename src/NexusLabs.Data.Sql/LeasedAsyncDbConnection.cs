using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using NexusLabs.Framework;
using NexusLabs.Framework.Data;
using NexusLabs.Framework.Threading;

namespace NexusLabs.Data.Sql;

/// <summary>
/// An <see cref="IAsyncDbConnection"/> decorator that acquires an
/// <see cref="AsyncSemaphoreLease"/> on open and releases it on close or dispose. The lease
/// is also released if open itself fails, so failed opens do not silently consume pool capacity.
/// Acquisition is bounded by a caller-supplied timeout; if the budget elapses with no slot
/// available a <see cref="ConnectionPoolExhaustedException"/> is thrown and no slot is taken.
/// </summary>
/// <remarks>
/// <para>
/// Composition order matters when stacking decorators: place this decorator INNERMOST (closest
/// to the underlying provider connection) so that lease wait time is observable by an outer
/// <see cref="OpenTrackingDecorator"/> or logger.
/// </para>
/// <para>
/// The caller owns the <see cref="SemaphoreSlim"/> lifecycle. For a pool-cap pattern where
/// rogue over-release should fail fast, construct the semaphore with
/// <c>new SemaphoreSlim(limit, limit)</c>.
/// </para>
/// <para>
/// Calling <see cref="OpenAsync(CancellationToken)"/> twice on the same instance without an
/// intervening <see cref="Close"/> or <see cref="DisposeAsync"/> throws
/// <see cref="InvalidOperationException"/> in the common single-threaded case and does not
/// acquire a second slot &mdash; behaviour parallels <c>System.Data</c> connectors that
/// reject re-open on an already-open connection. Under concurrent races past the early
/// guard, the loser's lease is released via <see cref="Interlocked.Exchange{T}(ref T, T)"/>
/// so no slot is leaked. Callers should still treat this type as not safe for concurrent
/// open from multiple threads (matching <c>IDbConnection</c> conventions).
/// </para>
/// <para>
/// Use <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> as the acquisition timeout to
/// opt out of bounded waiting (matching the cancellation-only behaviour of earlier versions).
/// </para>
/// </remarks>
public sealed class LeasedAsyncDbConnection : IAsyncDbConnection
{
    [TransfersOwnership]
    private readonly IAsyncDbConnection _inner;
    private readonly SemaphoreSlim _leaseSemaphore;
    private readonly TimeSpan _acquisitionTimeout;
    private AsyncSemaphoreLease? _lease;
    private int _disposed;

    /// <summary>Creates a new lease decorator around <paramref name="inner"/>.</summary>
    /// <param name="inner">The wrapped connection. Disposed by this decorator.</param>
    /// <param name="leaseSemaphore">
    /// Externally-owned semaphore controlling pool capacity. The caller is responsible for
    /// its lifecycle.
    /// </param>
    /// <param name="acquisitionTimeout">
    /// Maximum time to wait for a pool slot during <see cref="OpenAsync(CancellationToken)"/>.
    /// Use <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to wait indefinitely (relying on
    /// the caller's <see cref="CancellationToken"/>). <see cref="TimeSpan.Zero"/> attempts an
    /// immediate non-blocking acquire.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="inner"/> or <paramref name="leaseSemaphore"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="acquisitionTimeout"/> is negative and is not
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>.
    /// </exception>
    public LeasedAsyncDbConnection(
        IAsyncDbConnection inner,
        SemaphoreSlim leaseSemaphore,
        TimeSpan acquisitionTimeout)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(leaseSemaphore);

        if (acquisitionTimeout < TimeSpan.Zero && acquisitionTimeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(
                nameof(acquisitionTimeout),
                acquisitionTimeout,
                "Acquisition timeout must be non-negative or Timeout.InfiniteTimeSpan.");
        }

        _inner = inner;
        _leaseSemaphore = leaseSemaphore;
        _acquisitionTimeout = acquisitionTimeout;
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
        ReleaseLeaseOnce();
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
            ReleaseLeaseOnce();
        }
    }

    /// <inheritdoc />
    public void Open() => OpenAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task OpenAsync() => OpenAsync(CancellationToken.None);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// If this decorator already holds a lease (i.e. <see cref="OpenAsync(CancellationToken)"/>
    /// has been called and no intervening <see cref="Close"/> or <see cref="DisposeAsync"/>
    /// has run). Call <c>Close</c> or dispose the decorator before reopening.
    /// </exception>
    /// <exception cref="ConnectionPoolExhaustedException">
    /// If the configured acquisition timeout elapses before a pool slot is available.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// If <paramref name="cancellationToken"/> is cancelled while waiting for a slot.
    /// </exception>
    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _lease) is not null)
        {
            throw new InvalidOperationException(
                "Connection is already open; Close or DisposeAsync first before reopening.");
        }

        var lease = await _leaseSemaphore
            .TryAcquireAsync(_acquisitionTimeout, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ConnectionPoolExhaustedException(_acquisitionTimeout);

        try
        {
            await _inner.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lease.Dispose();
            throw;
        }

        var prior = Interlocked.Exchange(ref _lease, lease);
        prior?.Dispose();
    }

    private void ReleaseLeaseOnce()
    {
        var existing = Interlocked.Exchange(ref _lease, null);
        existing?.Dispose();
    }
}
