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
        // Ergonomic-default companion to AcquireAsync(TimeSpan, CancellationToken) — gives
        // top-level callers a no-token entry point. The mandatory-CT overload exists for
        // caller-controlled cancellation. Intentional NLF0018 exception per docs/analyzers/NLF0018.md.
#pragma warning disable NLF0018
        public async Task<AsyncSemaphoreLease> AcquireAsync(
            CancellationToken cancellationToken = default)
#pragma warning restore NLF0018
        {
            ArgumentNullException.ThrowIfNull(semaphore);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new AsyncSemaphoreLease(semaphore);
        }

        /// <summary>
        /// Waits up to <paramref name="timeout"/> for a slot on this semaphore and returns an
        /// <see cref="AsyncSemaphoreLease"/> whose disposal releases the slot. If the budget
        /// elapses before a slot is available, throws <see cref="TimeoutException"/> and no
        /// slot is acquired. Prefer
        /// <see cref="AcquireOrNullAsync(TimeSpan, CancellationToken)"/> when you want to handle
        /// the timeout case without exception flow.
        /// </summary>
        /// <param name="timeout">
        /// Maximum time to wait. Must be non-negative or <see cref="Timeout.InfiniteTimeSpan"/>;
        /// <see cref="TimeSpan.Zero"/> attempts the acquire without blocking.
        /// </param>
        /// <param name="cancellationToken">Token observed while waiting. Required.</param>
        /// <exception cref="ArgumentNullException">If the semaphore is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="timeout"/> is negative and is not <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </exception>
        /// <exception cref="TimeoutException">If <paramref name="timeout"/> elapses with no slot acquired.</exception>
        /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled while waiting.</exception>
        /// <exception cref="ObjectDisposedException">If the semaphore has been disposed.</exception>
        public async Task<AsyncSemaphoreLease> AcquireAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var lease = await semaphore
                .AcquireOrNullAsync(timeout, cancellationToken)
                .ConfigureAwait(false);

            return lease ?? throw new TimeoutException(
                $"Failed to acquire a semaphore slot within {timeout}.");
        }

        /// <summary>
        /// Waits up to <paramref name="timeout"/> for a slot on this semaphore. Returns the
        /// acquired <see cref="AsyncSemaphoreLease"/>, or <c>null</c> if the budget elapses
        /// before a slot is available (no exception is thrown on timeout). Cancellation still
        /// throws <see cref="OperationCanceledException"/>.
        /// </summary>
        /// <param name="timeout">
        /// Maximum time to wait. Must be non-negative or <see cref="Timeout.InfiniteTimeSpan"/>;
        /// <see cref="TimeSpan.Zero"/> attempts the acquire without blocking.
        /// </param>
        /// <param name="cancellationToken">Token observed while waiting. Required.</param>
        /// <exception cref="ArgumentNullException">If the semaphore is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="timeout"/> is negative and is not <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled while waiting.</exception>
        /// <exception cref="ObjectDisposedException">If the semaphore has been disposed.</exception>
        public async Task<AsyncSemaphoreLease?> AcquireOrNullAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(semaphore);

            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    timeout,
                    "Timeout must be non-negative or Timeout.InfiniteTimeSpan.");
            }

            var acquired = await semaphore
                .WaitAsync(timeout, cancellationToken)
                .ConfigureAwait(false);

            return acquired
                ? new AsyncSemaphoreLease(semaphore)
                : null;
        }
    }
}
