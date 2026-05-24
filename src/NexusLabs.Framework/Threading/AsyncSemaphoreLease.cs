using System;
using System.Threading;

namespace NexusLabs.Framework.Threading;

/// <summary>
/// Disposable lease over a <see cref="SemaphoreSlim"/> slot. Acquire one with the
/// <see cref="SemaphoreSlimExtensions.AcquireAsync(SemaphoreSlim, CancellationToken)"/>
/// extension and bind release to scope exit with <c>using</c>:
/// <code>
/// using var lease = await _semaphore.AcquireAsync(ct);
/// </code>
/// <see cref="Dispose"/> is idempotent and thread-safe; concurrent or repeated disposal will
/// release the slot at most once.
/// </summary>
/// <remarks>
/// The lease does NOT own the underlying <see cref="SemaphoreSlim"/>. The caller controls
/// the semaphore's lifetime and is responsible for disposing it. For a pool-cap pattern
/// where over-release should fail fast, construct the semaphore with
/// <c>new SemaphoreSlim(limit, limit)</c> so any rogue extra <see cref="SemaphoreSlim.Release()"/>
/// throws <see cref="SemaphoreFullException"/>.
/// </remarks>
public sealed class AsyncSemaphoreLease : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private int _released;

    internal AsyncSemaphoreLease(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
        {
            _semaphore.Release();
        }
    }
}

