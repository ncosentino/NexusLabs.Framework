using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace NexusLabs.Testing.Time.Tests;

public sealed class FakeTimeProviderWaitExtensionsTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task Advance_LandingBeforeRegistration_StrandsTheDelay()
    {
        var clock = new FakeTimeProvider();
        using var scenario = StrandedDelay.AdvanceBeforeRegistration(clock, Delay);

        await Task.WhenAny(scenario.Work, Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));

        Assert.False(
            scenario.Work.IsCompleted,
            "a single advance that precedes registration should leave the delay waiting; " +
            "this test guards the hazard the pump exists to recover from");
    }

    [Fact]
    public async Task AdvanceUntilAsync_RecoversAStrandedDelay()
    {
        var clock = new FakeTimeProvider();
        using var scenario = StrandedDelay.AdvanceBeforeRegistration(clock, Delay);

        var outcome = await clock.AdvanceUntilAsync(
            () => scenario.Work.IsCompleted,
            Delay,
            TimeSpan.FromSeconds(30),
            TestContext.Current.CancellationToken,
            maxSimulatedAdvance: TimeSpan.FromHours(1));

        Assert.True(outcome.Succeeded, outcome.Describe());
        Assert.True(outcome.SimulatedAdvance > TimeSpan.Zero);
    }

    [Fact]
    public async Task AdvanceUntilAsync_NeverTrue_StopsAtTheSimulatedBudget()
    {
        var clock = new FakeTimeProvider();
        var startUtc = clock.GetUtcNow();
        var budget = TimeSpan.FromMinutes(10);
        var timeout = TimeSpan.FromSeconds(30);

        var outcome = await clock.AdvanceUntilAsync(
            () => false,
            Delay,
            timeout,
            TestContext.Current.CancellationToken,
            maxSimulatedAdvance: budget,
            pollDelay: TimeSpan.Zero);

        Assert.False(outcome.Succeeded);
        Assert.Equal(budget, clock.GetUtcNow() - startUtc);
        Assert.Equal(budget, outcome.SimulatedAdvance);
        Assert.Contains("simulated-time budget", outcome.FailureReason!, StringComparison.Ordinal);
        Assert.True(
            outcome.Elapsed < timeout,
            $"an exhausted budget should end the wait early, but it ran for {outcome.Elapsed}");
    }

    [Fact]
    public async Task AdvanceUntilAsync_NeverTrue_StopsOnTheRealDeadline()
    {
        var clock = new FakeTimeProvider();
        var timeout = TimeSpan.FromMilliseconds(200);

        var outcome = await clock.AdvanceUntilAsync(
            () => false,
            TimeSpan.FromMilliseconds(1),
            timeout,
            TestContext.Current.CancellationToken,
            maxSimulatedAdvance: TimeSpan.FromDays(365));

        Assert.False(outcome.Succeeded);
        Assert.Contains("timeout elapsed", outcome.FailureReason!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdvanceUntilAsync_FailureCarriesThePredicateSourceText()
    {
        var clock = new FakeTimeProvider();
        var done = false;

        var outcome = await clock.AdvanceUntilAsync(
            () => done,
            Delay,
            TimeSpan.FromMilliseconds(50),
            TestContext.Current.CancellationToken);

        Assert.Equal("() => done", outcome.Condition);
    }

    [Fact]
    public async Task AdvanceUntilAsync_CancelledToken_ReturnsResultInsteadOfThrowing()
    {
        var clock = new FakeTimeProvider();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var outcome = await clock.AdvanceUntilAsync(
            () => false,
            Delay,
            TimeSpan.FromSeconds(5),
            cancellation.Token);

        Assert.False(outcome.Succeeded);
        Assert.Contains("cancelled", outcome.FailureReason!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdvanceUntilAsync_NonPositiveIncrement_Throws()
    {
        var clock = new FakeTimeProvider();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await clock.AdvanceUntilAsync(
                () => true,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AdvanceUntilAsync_NullClock_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await FakeTimeProviderWaitExtensions.AdvanceUntilAsync(
                null!,
                () => true,
                Delay,
                TimeSpan.FromSeconds(1),
                TestContext.Current.CancellationToken));
    }
}
