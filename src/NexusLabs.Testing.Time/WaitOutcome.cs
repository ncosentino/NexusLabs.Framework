namespace NexusLabs.Testing.Time;

/// <summary>
/// The result of waiting for a condition to become true.
/// </summary>
/// <remarks>
/// This is a result rather than an assertion or an exception so the primitive sits below any
/// particular test framework. Callers translate a failed outcome into their own failure type.
/// </remarks>
public readonly record struct WaitOutcome
{
    private WaitOutcome(
        bool succeeded,
        string condition,
        int attempts,
        TimeSpan elapsed,
        TimeSpan simulatedAdvance,
        string? failureReason)
    {
        Succeeded = succeeded;
        Condition = condition;
        Attempts = attempts;
        Elapsed = elapsed;
        SimulatedAdvance = simulatedAdvance;
        FailureReason = failureReason;
    }

    /// <summary>
    /// Gets a value indicating whether the condition was observed to be true before the wait ended.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the source text of the predicate, captured at the call site.
    /// </summary>
    public string Condition { get; }

    /// <summary>
    /// Gets the number of times the predicate was evaluated.
    /// </summary>
    public int Attempts { get; }

    /// <summary>
    /// Gets the real time that elapsed, measured against a monotonic clock.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Gets the total simulated time injected into the clock under test, or
    /// <see cref="TimeSpan.Zero"/> when the wait did not drive a clock.
    /// </summary>
    public TimeSpan SimulatedAdvance { get; }

    /// <summary>
    /// Gets the reason the wait ended without observing the condition, or <see langword="null"/>
    /// when <see cref="Succeeded"/> is <see langword="true"/>.
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// Builds a description suitable for a test failure message.
    /// </summary>
    public string Describe() =>
        Succeeded
            ? $"Condition '{Condition}' held after {Attempts} attempt(s) in {Elapsed}."
            : $"Condition '{Condition}' was never observed to be true. {FailureReason} " +
              $"Attempts: {Attempts}. Real elapsed: {Elapsed}. Simulated advance: {SimulatedAdvance}.";

    internal static WaitOutcome Success(
        string condition,
        int attempts,
        TimeSpan elapsed,
        TimeSpan simulatedAdvance) =>
        new(true, condition, attempts, elapsed, simulatedAdvance, failureReason: null);

    internal static WaitOutcome Failure(
        string condition,
        int attempts,
        TimeSpan elapsed,
        TimeSpan simulatedAdvance,
        string failureReason) =>
        new(false, condition, attempts, elapsed, simulatedAdvance, failureReason);
}
