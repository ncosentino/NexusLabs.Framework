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
/// intervening <see cref="Close"/> or <see cref="DisposeAsync"/> will release the previously
/// held lease before adopting the new one, so a second open never leaks pool capacity.
/// </para>
/// </remarks>
public sealed class LeasedAsyncDbConnection : IAsyncDbConnection
{
    [TransfersOwnership]
    private readonly IAsyncDbConnection _inner;
    private readonly SemaphoreSlim _leaseSemaphore;
    private AsyncSemaphoreLease? _lease;
    private int _disposed;

    /// <summary>Creates a new lease decorator around <paramref name="inner"/>.</summary>
    /// <exception cref="ArgumentNullException">If either argument is null.</exception>
    public LeasedAsyncDbConnection(
        IAsyncDbConnection inner,
        SemaphoreSlim leaseSemaphore)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(leaseSemaphore);

        _inner = inner;
        _leaseSemaphore = leaseSemaphore;
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
    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        var lease = await _leaseSemaphore
            .AcquireAsync(cancellationToken)
            .ConfigureAwait(false);

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
