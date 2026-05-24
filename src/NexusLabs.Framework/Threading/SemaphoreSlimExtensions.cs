using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Threading;

/// <summary>
/// Extension augmentations on <see cref="SemaphoreSlim"/>. Uses C# 14
/// <c>extension(SemaphoreSlim)</c> blocks so the helpers are callable in the same
/// instance-method shape as the built-in BCL members (<c>sem.WaitAsync(ct)</c>,
/// <c>sem.AcquireAsync(ct)</c>).
/// </summary>
public static class SemaphoreSlimExtensions
{
    extension(SemaphoreSlim semaphore)
    {
        /// <summary>
        /// Waits for a slot on this semaphore and returns an <see cref="AsyncSemaphoreLease"/>
        /// whose disposal releases the slot. Pair with <c>using</c> to bind release to scope
        /// exit:
        /// <code>
        /// using var lease = await _semaphore.AcquireAsync(ct);
        /// </code>
        /// If <paramref name="cancellationToken"/> is cancelled while waiting, no slot is
        /// acquired and an <see cref="OperationCanceledException"/> is thrown.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the semaphore is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation is requested while waiting.</exception>
        /// <exception cref="ObjectDisposedException">If the semaphore has been disposed.</exception>
        public async Task<AsyncSemaphoreLease> AcquireAsync(
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(semaphore);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new AsyncSemaphoreLease(semaphore);
        }
    }
}
