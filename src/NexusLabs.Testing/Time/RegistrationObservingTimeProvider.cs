using Microsoft.Extensions.Time.Testing;

using NexusLabs.Framework;

namespace NexusLabs.Testing.Time;

/// <summary>
/// A <see cref="FakeTimeProvider"/> that reports when the code under test arms a timer, so a test
/// can advance the clock only once the registration it depends on has actually happened.
/// </summary>
/// <remarks>
/// <see cref="FakeTimeProvider.Advance"/> only fires timers that are already registered. Code that
/// arms a delay on a task the test cannot observe may register after the advance and then wait
/// forever on a clock that never moves again. Waiting for the registration removes the race
/// instead of racing it faster.
/// <para>
/// Every override delegates to the base implementation. Returning a different timer would defeat
/// virtual time entirely: every delay in the system under test would complete on its own schedule,
/// so tests would pass without ever exercising the delay.
/// </para>
/// </remarks>
public sealed class RegistrationObservingTimeProvider : FakeTimeProvider
{
    private readonly Lock _sync = new();
    private readonly List<Waiter> _waiters = [];

    private int _createTimerCalls;
    private int _changeCalls;
    private int _armCount;

    /// <summary>
    /// Gets the number of <see cref="CreateTimer"/> calls observed, for any due time.
    /// </summary>
    public int CreateTimerCalls
    {
        get
        {
            lock (_sync)
            {
                return _createTimerCalls;
            }
        }
    }

    /// <summary>
    /// Gets the number of times an existing timer was re-armed through <see cref="ITimer.Change"/>.
    /// </summary>
    public int ChangeCalls
    {
        get
        {
            lock (_sync)
            {
                return _changeCalls;
            }
        }
    }

    /// <summary>
    /// Gets the number of times a timer was armed with a finite due time, whether at creation or
    /// through a later change. This is the count a test should wait on before advancing.
    /// </summary>
    public int ArmCount
    {
        get
        {
            lock (_sync)
            {
                return _armCount;
            }
        }
    }

    /// <inheritdoc />
    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        var inner = base.CreateTimer(callback, state, dueTime, period);

        List<Waiter>? released;
        lock (_sync)
        {
            _createTimerCalls++;
            released = RecordArm(dueTime);
        }

        Release(released);
        return new ObservingTimer(inner, this);
    }

    /// <summary>
    /// Waits until at least <paramref name="count"/> timers have been armed with a finite due time.
    /// </summary>
    /// <param name="count">The number of armed timers to wait for.</param>
    /// <param name="timeout">The maximum real time to wait for the registrations.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>
    /// An outcome describing whether the registrations were observed. A timeout and a cancellation
    /// both surface as a failed outcome rather than an exception.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is less than one.
    /// </exception>
    public async Task<WaitOutcome> WaitForArmedTimersAsync(
        int count,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var startedAt = TimeProvider.System.GetTimestamp();
        var condition = $"at least {count} armed timer(s)";
        Waiter waiter;

        lock (_sync)
        {
            if (_armCount >= count)
            {
                return WaitOutcome.Success(
                    condition,
                    attempts: 1,
                    TimeProvider.System.GetElapsedTime(startedAt),
                    TimeSpan.Zero);
            }

            waiter = new Waiter(count);
            _waiters.Add(waiter);
        }

        try
        {
            await waiter.Completion.Task
                .WaitAsync(timeout, cancellationToken)
                .ConfigureAwait(false);

            return WaitOutcome.Success(
                condition,
                attempts: 1,
                TimeProvider.System.GetElapsedTime(startedAt),
                TimeSpan.Zero);
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
        {
            lock (_sync)
            {
                _waiters.Remove(waiter);
                return WaitOutcome.Failure(
                    condition,
                    attempts: 1,
                    TimeProvider.System.GetElapsedTime(startedAt),
                    TimeSpan.Zero,
                    ex is TimeoutException
                        ? $"Only {_armCount} timer(s) were armed before the {timeout} timeout."
                        : $"The wait was cancelled after {_armCount} armed timer(s).");
            }
        }
    }

    private static void Release(List<Waiter>? released)
    {
        if (released is null)
        {
            return;
        }

        foreach (var waiter in released)
        {
            waiter.Completion.TrySetResult();
        }
    }

    private List<Waiter>? RecordArm(TimeSpan dueTime)
    {
        if (dueTime == Timeout.InfiniteTimeSpan)
        {
            return null;
        }

        _armCount++;

        List<Waiter>? released = null;
        for (var i = _waiters.Count - 1; i >= 0; i--)
        {
            if (_waiters[i].Threshold > _armCount)
            {
                continue;
            }

            released ??= [];
            released.Add(_waiters[i]);
            _waiters.RemoveAt(i);
        }

        return released;
    }

    private void OnChange(TimeSpan dueTime)
    {
        List<Waiter>? released;
        lock (_sync)
        {
            _changeCalls++;
            released = RecordArm(dueTime);
        }

        Release(released);
    }

    private sealed class Waiter(int threshold)
    {
        public int Threshold { get; } = threshold;

        public TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ObservingTimer : ITimer
    {
        [TransfersOwnership]
        private readonly ITimer _inner;
        private readonly RegistrationObservingTimeProvider _owner;

        public ObservingTimer(ITimer inner, RegistrationObservingTimeProvider owner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(owner);

            _inner = inner;
            _owner = owner;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            var changed = _inner.Change(dueTime, period);
            if (changed)
            {
                _owner.OnChange(dueTime);
            }

            return changed;
        }

        public void Dispose() => _inner.Dispose();

        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
