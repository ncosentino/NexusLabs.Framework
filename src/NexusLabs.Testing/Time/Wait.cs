using System.Runtime.CompilerServices;

namespace NexusLabs.Testing.Time;

/// <summary>
/// Waits for a condition to become true, bounded by a real-time deadline.
/// </summary>
public static class Wait
{
    /// <summary>
    /// The real delay between evaluations used when the caller does not specify one.
    /// </summary>
    public static readonly TimeSpan DefaultPollDelay = TimeSpan.FromMilliseconds(1);

    /// <summary>
    /// Polls <paramref name="predicate"/> until it returns <see langword="true"/>, the timeout
    /// elapses, or the token is cancelled.
    /// </summary>
    /// <param name="predicate">The condition to observe. Must not block.</param>
    /// <param name="timeout">The maximum real time to wait.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <param name="pollDelay">
    /// Real delay between evaluations. Pass <see cref="TimeSpan.Zero"/> to yield instead of
    /// delaying, which converges faster but occupies a thread for the whole wait.
    /// </param>
    /// <param name="onBeforePollAsync">
    /// Invoked before each re-evaluation with the 1-based attempt number. This is the seam that
    /// lets a caller drive whatever the condition is waiting on, such as a controllable clock.
    /// </param>
    /// <param name="condition">Captured automatically from the call site; do not supply.</param>
    /// <returns>
    /// An outcome describing whether the condition held, and enough context to explain why not.
    /// A failed condition and a cancelled token both surface as a failed outcome rather than an
    /// exception.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The deadline is measured with <see cref="TimeProvider.System"/> and is deliberately not
    /// configurable. A caller driving a controllable clock would otherwise be able to supply that
    /// same clock here, and each pump would consume its own deadline budget: one one-minute
    /// advance would spend a full minute of the allowance before the condition was re-checked.
    /// </remarks>
    public static async ValueTask<WaitOutcome> UntilAsync(
        Func<bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        TimeSpan? pollDelay = null,
        Func<int, CancellationToken, ValueTask>? onBeforePollAsync = null,
        [CallerArgumentExpression(nameof(predicate))] string? condition = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var description = condition ?? "<predicate>";
        var delay = pollDelay ?? DefaultPollDelay;
        var startedAt = TimeProvider.System.GetTimestamp();
        var attempts = 0;

        while (true)
        {
            attempts++;
            if (predicate())
            {
                return WaitOutcome.Success(
                    description,
                    attempts,
                    TimeProvider.System.GetElapsedTime(startedAt),
                    TimeSpan.Zero);
            }

            var elapsed = TimeProvider.System.GetElapsedTime(startedAt);
            if (cancellationToken.IsCancellationRequested)
            {
                return Cancelled(description, attempts, elapsed);
            }

            if (elapsed >= timeout)
            {
                return WaitOutcome.Failure(
                    description,
                    attempts,
                    elapsed,
                    TimeSpan.Zero,
                    $"The {timeout} timeout elapsed first.");
            }

            if (onBeforePollAsync is not null)
            {
                await onBeforePollAsync(attempts, cancellationToken).ConfigureAwait(false);
            }

            if (delay <= TimeSpan.Zero)
            {
                await Task.Yield();
                continue;
            }

            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return Cancelled(
                    description,
                    attempts,
                    TimeProvider.System.GetElapsedTime(startedAt));
            }
        }
    }

    private static WaitOutcome Cancelled(string condition, int attempts, TimeSpan elapsed) =>
        WaitOutcome.Failure(
            condition,
            attempts,
            elapsed,
            TimeSpan.Zero,
            "The wait was cancelled.");
}
