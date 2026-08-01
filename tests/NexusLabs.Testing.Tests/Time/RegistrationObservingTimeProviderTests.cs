using Xunit;

namespace NexusLabs.Testing.Time.Tests;

public sealed class RegistrationObservingTimeProviderTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task WaitForArmedTimersAsync_ObservesTaskDelayRegistration()
    {
        var clock = new RegistrationObservingTimeProvider();
        using var scenario = StrandedDelay.RegistrationOnly(clock, Delay);

        var armed = await clock.WaitForArmedTimersAsync(1, Patience, TestContext.Current.CancellationToken);

        Assert.True(armed.Succeeded, armed.Describe());
        Assert.Equal(1, clock.CreateTimerCalls);
        Assert.Equal(1, clock.ArmCount);
        Assert.Equal(0, clock.ChangeCalls);

        clock.Advance(Delay);
        await scenario.Work.WaitAsync(Patience, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WaitForArmedTimersAsync_ThenOneAdvance_CompletesWithoutOvershooting()
    {
        var clock = new RegistrationObservingTimeProvider();
        using var scenario = StrandedDelay.RegistrationOnly(clock, Delay);

        await clock.WaitForArmedTimersAsync(1, Patience, TestContext.Current.CancellationToken);

        var startUtc = clock.GetUtcNow();
        clock.Advance(Delay);
        await scenario.Work.WaitAsync(Patience, TestContext.Current.CancellationToken);

        Assert.Equal(Delay, clock.GetUtcNow() - startUtc);
    }

    [Fact]
    public async Task ObservingRegistrations_DoesNotCompleteTheDelayEarly()
    {
        var clock = new RegistrationObservingTimeProvider();
        using var scenario = StrandedDelay.RegistrationOnly(clock, Delay);

        await clock.WaitForArmedTimersAsync(1, Patience, TestContext.Current.CancellationToken);
        await Task.WhenAny(scenario.Work, Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));

        Assert.False(
            scenario.Work.IsCompleted,
            "delegating to the base scheduler must preserve virtual time; completing here would " +
            "mean every delay in a system under test finishes without the clock moving");

        clock.Advance(Delay);
        await scenario.Work.WaitAsync(Patience, TestContext.Current.CancellationToken);
        Assert.True(scenario.Work.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task WaitForArmedTimersAsync_AlreadySatisfied_SucceedsImmediately()
    {
        var clock = new RegistrationObservingTimeProvider();
        using var scenario = StrandedDelay.RegistrationOnly(clock, Delay);

        await clock.WaitForArmedTimersAsync(1, Patience, TestContext.Current.CancellationToken);
        var second = await clock.WaitForArmedTimersAsync(1, Patience, TestContext.Current.CancellationToken);

        Assert.True(second.Succeeded, second.Describe());

        clock.Advance(Delay);
        await scenario.Work.WaitAsync(Patience, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WaitForArmedTimersAsync_NeverArmed_FailsWithTheObservedCount()
    {
        var clock = new RegistrationObservingTimeProvider();

        var armed = await clock.WaitForArmedTimersAsync(
            2,
            TimeSpan.FromMilliseconds(200),
            TestContext.Current.CancellationToken);

        Assert.False(armed.Succeeded);
        Assert.Contains("Only 0 timer(s) were armed", armed.FailureReason!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WaitForArmedTimersAsync_CancelledToken_ReturnsResultInsteadOfThrowing()
    {
        var clock = new RegistrationObservingTimeProvider();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var armed = await clock.WaitForArmedTimersAsync(1, Patience, cancellation.Token);

        Assert.False(armed.Succeeded);
        Assert.Contains("cancelled", armed.FailureReason!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateTimer_WithInfiniteDueTime_IsNotCountedAsArmedUntilChanged()
    {
        var clock = new RegistrationObservingTimeProvider();
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var timer = clock.CreateTimer(
            _ => fired.TrySetResult(),
            state: null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);

        Assert.Equal(1, clock.CreateTimerCalls);
        Assert.Equal(0, clock.ArmCount);

        Assert.True(timer.Change(Delay, Timeout.InfiniteTimeSpan));

        Assert.Equal(1, clock.ChangeCalls);
        Assert.Equal(1, clock.ArmCount);

        clock.Advance(Delay);
        await fired.Task.WaitAsync(Patience, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WaitForArmedTimersAsync_ObservesCancellationTokenSourceAndPeriodicTimer()
    {
        var ctsClock = new RegistrationObservingTimeProvider();
        using (var _ = new CancellationTokenSource(Delay, ctsClock))
        {
            var ctsArmed = await ctsClock.WaitForArmedTimersAsync(1, Patience, TestContext.Current.CancellationToken);
            Assert.True(ctsArmed.Succeeded, ctsArmed.Describe());
        }

        var periodicClock = new RegistrationObservingTimeProvider();
        using var periodic = new PeriodicTimer(Delay, periodicClock);
        var tick = Task.Run(
            async () => await periodic.WaitForNextTickAsync(TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);

        var periodicArmed = await periodicClock.WaitForArmedTimersAsync(1, Patience, TestContext.Current.CancellationToken);
        Assert.True(periodicArmed.Succeeded, periodicArmed.Describe());

        periodicClock.Advance(Delay);
        Assert.True(await tick.WaitAsync(Patience, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WaitForArmedTimersAsync_ReleasesConcurrentWaitersAtTheirOwnThresholds()
    {
        var clock = new RegistrationObservingTimeProvider();

        var firstWaiter = clock.WaitForArmedTimersAsync(1, Patience, TestContext.Current.CancellationToken);
        var secondWaiter = clock.WaitForArmedTimersAsync(2, Patience, TestContext.Current.CancellationToken);

        using var one = clock.CreateTimer(_ => { }, state: null, Delay, Timeout.InfiniteTimeSpan);

        var firstOutcome = await firstWaiter;
        Assert.True(firstOutcome.Succeeded, firstOutcome.Describe());
        Assert.False(secondWaiter.IsCompleted, "the second waiter needs a second armed timer");

        using var two = clock.CreateTimer(_ => { }, state: null, Delay, Timeout.InfiniteTimeSpan);

        var secondOutcome = await secondWaiter;
        Assert.True(secondOutcome.Succeeded, secondOutcome.Describe());
        Assert.Equal(2, clock.ArmCount);
    }

    [Fact]
    public async Task WaitForArmedTimersAsync_CountBelowOne_Throws()
    {
        var clock = new RegistrationObservingTimeProvider();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await clock.WaitForArmedTimersAsync(0, Patience, TestContext.Current.CancellationToken));
    }
}
