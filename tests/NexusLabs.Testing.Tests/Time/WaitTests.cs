using Xunit;

namespace NexusLabs.Testing.Time.Tests;

public sealed class WaitTests
{
    [Fact]
    public async Task UntilAsync_ConditionAlreadyTrue_SucceedsOnFirstAttempt()
    {
        var outcome = await Wait.UntilAsync(
            () => true,
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.True(outcome.Succeeded);
        Assert.Equal(1, outcome.Attempts);
        Assert.Null(outcome.FailureReason);
    }

    [Fact]
    public async Task UntilAsync_ConditionBecomesTrue_Succeeds()
    {
        var flips = 0;

        var outcome = await Wait.UntilAsync(
            () => ++flips >= 3,
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.True(outcome.Succeeded, outcome.Describe());
        Assert.Equal(3, outcome.Attempts);
    }

    [Fact]
    public async Task UntilAsync_NeverTrue_FailsOnTheDeadlineRatherThanHanging()
    {
        var timeout = TimeSpan.FromMilliseconds(200);

        var outcome = await Wait.UntilAsync(
            () => false,
            timeout,
            TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Contains("timeout elapsed", outcome.FailureReason!, StringComparison.Ordinal);
        Assert.True(
            outcome.Elapsed >= timeout,
            $"expected at least {timeout} to elapse, got {outcome.Elapsed}");
    }

    [Fact]
    public async Task UntilAsync_FailureCarriesThePredicateSourceText()
    {
        var ready = false;

        var outcome = await Wait.UntilAsync(
            () => ready,
            TimeSpan.FromMilliseconds(50),
            TestContext.Current.CancellationToken);

        Assert.Equal("() => ready", outcome.Condition);
        Assert.Contains("() => ready", outcome.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UntilAsync_CancelledToken_ReturnsResultInsteadOfThrowing()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var outcome = await Wait.UntilAsync(
            () => false,
            TimeSpan.FromSeconds(5),
            cancellation.Token);

        Assert.False(outcome.Succeeded);
        Assert.Contains("cancelled", outcome.FailureReason!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UntilAsync_CancelledMidWait_ReturnsResultInsteadOfThrowing()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));

        var outcome = await Wait.UntilAsync(
            () => false,
            TimeSpan.FromSeconds(30),
            cancellation.Token,
            pollDelay: TimeSpan.FromMilliseconds(5));

        Assert.False(outcome.Succeeded);
        Assert.Contains("cancelled", outcome.FailureReason!, StringComparison.Ordinal);
        Assert.True(
            outcome.Elapsed < TimeSpan.FromSeconds(20),
            $"cancellation should end the wait early, but it took {outcome.Elapsed}");
    }

    [Fact]
    public async Task UntilAsync_InvokesTheCallerSeamBeforeEachRetry()
    {
        var polls = new List<int>();
        var attempts = 0;

        var outcome = await Wait.UntilAsync(
            () => ++attempts >= 3,
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken,
            onBeforePollAsync: (attempt, _) =>
            {
                polls.Add(attempt);
                return ValueTask.CompletedTask;
            });

        Assert.True(outcome.Succeeded, outcome.Describe());
        Assert.Equal([1, 2], polls);
    }

    [Fact]
    public async Task UntilAsync_NullPredicate_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await Wait.UntilAsync(
                null!,
                TimeSpan.FromSeconds(1),
                TestContext.Current.CancellationToken));
    }
}
