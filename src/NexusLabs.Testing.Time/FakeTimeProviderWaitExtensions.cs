using System.Runtime.CompilerServices;

using Microsoft.Extensions.Time.Testing;

namespace NexusLabs.Testing.Time;

/// <summary>
/// Waiting helpers for tests that drive a <see cref="FakeTimeProvider"/>.
/// </summary>
public static class FakeTimeProviderWaitExtensions
{
    /// <summary>
    /// The simulated-time budget used when the caller does not specify one.
    /// </summary>
    public static readonly TimeSpan DefaultMaxSimulatedAdvance = TimeSpan.FromDays(1);

    /// <summary>
    /// Advances <paramref name="timeProvider"/> repeatedly until <paramref name="predicate"/>
    /// holds, the real timeout elapses, or the simulated-time budget is exhausted.
    /// </summary>
    /// <param name="timeProvider">The controllable clock to pump.</param>
    /// <param name="predicate">The condition to observe.</param>
    /// <param name="increment">How much simulated time each pump injects.</param>
    /// <param name="timeout">The maximum real time to wait.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <param name="maxSimulatedAdvance">
    /// An upper bound on total injected simulated time. Without it a pump can move the clock by
    /// simulated years, firing unrelated long-horizon timers the test never intended to trigger.
    /// Exhausting the budget ends the wait immediately rather than spinning out the remaining
    /// real deadline.
    /// </param>
    /// <param name="pollDelay">
    /// Real delay between pumps. Defaults to <see cref="Wait.DefaultPollDelay"/>.
    /// </param>
    /// <param name="condition">Captured automatically from the call site; do not supply.</param>
    /// <returns>An outcome describing whether the condition held.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="timeProvider"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="increment"/> is not positive, which would pump forever without moving the
    /// clock.
    /// </exception>
    /// <remarks>
    /// A single <see cref="FakeTimeProvider.Advance"/> only fires timers that are already
    /// registered. Code that arms its delay on a task the test cannot observe can register after
    /// that advance, leaving the timer due at a simulated time the test never reaches. Pumping
    /// re-arms whatever registered late.
    /// <para>
    /// This converges rather than synchronises, so it can still lose the race under load. Prefer
    /// <see cref="RegistrationObservingTimeProvider"/> whenever the number of expected timer
    /// registrations is known: waiting for the registration itself is deterministic.
    /// </para>
    /// </remarks>
    public static async ValueTask<WaitOutcome> AdvanceUntilAsync(
        this FakeTimeProvider timeProvider,
        Func<bool> predicate,
        TimeSpan increment,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        TimeSpan? maxSimulatedAdvance = null,
        TimeSpan? pollDelay = null,
        [CallerArgumentExpression(nameof(predicate))] string? condition = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(increment, TimeSpan.Zero);

        var budget = maxSimulatedAdvance ?? DefaultMaxSimulatedAdvance;
        var advanced = TimeSpan.Zero;
        var budgetExhausted = false;
        var observed = false;

        var outcome = await Wait.UntilAsync(
            () =>
            {
                if (predicate())
                {
                    observed = true;
                    return true;
                }

                // Once the clock can no longer move there is nothing left for this wait to do,
                // so it reports rather than spinning out the remaining real deadline.
                return budgetExhausted;
            },
            timeout,
            cancellationToken,
            pollDelay,
            (_, _) =>
            {
                if (advanced + increment > budget)
                {
                    budgetExhausted = true;
                    return ValueTask.CompletedTask;
                }

                advanced += increment;
                timeProvider.Advance(increment);
                return ValueTask.CompletedTask;
            },
            condition).ConfigureAwait(false);

        if (observed)
        {
            return WaitOutcome.Success(
                outcome.Condition,
                outcome.Attempts,
                outcome.Elapsed,
                advanced);
        }

        var reason = budgetExhausted
            ? $"The {budget} simulated-time budget was exhausted in {increment} steps."
            : outcome.FailureReason ?? "The wait ended without observing the condition.";

        return WaitOutcome.Failure(
            outcome.Condition,
            outcome.Attempts,
            outcome.Elapsed,
            advanced,
            reason);
    }
}
