using Microsoft.Extensions.Time.Testing;

namespace NexusLabs.Testing.Time.Tests;

/// <summary>
/// Builds the scenario these primitives exist for: a delay that is armed only after the caller
/// has already advanced the clock, so the timer becomes due at a simulated time a single advance
/// never reaches.
/// </summary>
internal sealed class StrandedDelay : IDisposable
{
    private readonly TaskCompletionSource _gate =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private StrandedDelay(FakeTimeProvider clock, TimeSpan delay)
    {
        Clock = clock;
        Work = Task.Run(async () =>
        {
            await _gate.Task;
            await Task.Delay(delay, clock);
        });
    }

    public FakeTimeProvider Clock { get; }

    public Task Work { get; }

    /// <summary>
    /// Advances the clock first, then releases the code under test so its registration
    /// deterministically lands after the advance.
    /// </summary>
    public static StrandedDelay AdvanceBeforeRegistration(FakeTimeProvider clock, TimeSpan delay)
    {
        var scenario = new StrandedDelay(clock, delay);
        clock.Advance(delay);
        scenario._gate.SetResult();
        return scenario;
    }

    /// <summary>
    /// Releases the code under test without advancing, leaving the registration observable.
    /// </summary>
    public static StrandedDelay RegistrationOnly(FakeTimeProvider clock, TimeSpan delay)
    {
        var scenario = new StrandedDelay(clock, delay);
        scenario._gate.SetResult();
        return scenario;
    }

    public void Dispose() => _gate.TrySetResult();
}
