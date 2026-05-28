using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NexusLabs.Framework.Threading;

using Xunit;

namespace NexusLabs.Framework.Tests.Threading;

public sealed class AsyncSemaphoreLeaseTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task AcquireAsync_NullSemaphore_Throws()
    {
        SemaphoreSlim? sem = null;
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sem!.AcquireAsync(_ct));
    }

    [Fact]
    public async Task AcquireAsync_TakesOneSlot()
    {
        using var sem = new SemaphoreSlim(2, 2);
        Assert.Equal(2, sem.CurrentCount);

        using var lease = await sem.AcquireAsync();

        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task Dispose_ReleasesSlot()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var lease = await sem.AcquireAsync();
        Assert.Equal(0, sem.CurrentCount);

        lease.Dispose();

        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task Dispose_IsIdempotent_Sequential()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var lease = await sem.AcquireAsync();

        lease.Dispose();
        lease.Dispose();
        lease.Dispose();

        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task Dispose_IsIdempotent_Concurrent()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var lease = await sem.AcquireAsync();

        var disposeCount = 64;
        var start = new ManualResetEventSlim(initialState: false);
        var tasks = Enumerable.Range(0, disposeCount).Select(_ => Task.Run(() =>
        {
            start.Wait();
            lease.Dispose();
        })).ToArray();

        start.Set();
        await Task.WhenAll(tasks);

        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_BlocksWhenAtCapacity_AndReleaseAllowsAcquire()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var first = await sem.AcquireAsync();

        var secondTask = sem.AcquireAsync();
        Assert.False(secondTask.IsCompleted);

        first.Dispose();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var second = await secondTask.WaitAsync(cts.Token);
        Assert.Equal(0, sem.CurrentCount);
        second.Dispose();
        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_HonorsCancellation_WhileWaiting()
    {
        using var sem = new SemaphoreSlim(1, 1);
        using var blockingLease = await sem.AcquireAsync();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var pending = sem.AcquireAsync(cts.Token);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, sem.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_PreCancelled_DoesNotConsumeSlot()
    {
        using var sem = new SemaphoreSlim(1, 1);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sem.AcquireAsync(cts.Token));

        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_TimeSpan_NullSemaphore_Throws()
    {
        SemaphoreSlim? sem = null;
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sem!.AcquireAsync(TimeSpan.FromSeconds(1), _ct));
    }

    [Fact]
    public async Task AcquireAsync_TimeSpan_NegativeTimeout_Throws()
    {
        using var sem = new SemaphoreSlim(1, 1);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sem.AcquireAsync(TimeSpan.FromMilliseconds(-2), _ct));
    }

    [Fact]
    public async Task AcquireAsync_TimeSpan_HappyPath_ReturnsLeaseAndDecrementsCount()
    {
        using var sem = new SemaphoreSlim(2, 2);

        using var lease = await sem.AcquireAsync(TimeSpan.FromSeconds(5), _ct);

        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_TimeSpan_BudgetElapses_ThrowsTimeoutException_AndDoesNotConsumeSlot()
    {
        using var sem = new SemaphoreSlim(1, 1);
        using var blocker = await sem.AcquireAsync(_ct);

        await Assert.ThrowsAsync<TimeoutException>(
            () => sem.AcquireAsync(TimeSpan.FromMilliseconds(50), _ct));

        Assert.Equal(0, sem.CurrentCount);

        blocker.Dispose();
        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_TimeSpan_HonorsCancellation_ThrowsOCE_NoSlotConsumed()
    {
        using var sem = new SemaphoreSlim(1, 1);
        using var blocker = await sem.AcquireAsync(_ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var pending = sem.AcquireAsync(TimeSpan.FromSeconds(30), cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, sem.CurrentCount);
    }

    [Fact]
    public async Task TryAcquireAsync_NullSemaphore_Throws()
    {
        SemaphoreSlim? sem = null;
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sem!.TryAcquireAsync(TimeSpan.FromSeconds(1), _ct));
    }

    [Fact]
    public async Task TryAcquireAsync_NegativeTimeout_Throws()
    {
        using var sem = new SemaphoreSlim(1, 1);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sem.TryAcquireAsync(TimeSpan.FromMilliseconds(-2), _ct));
    }

    [Fact]
    public async Task TryAcquireAsync_HappyPath_ReturnsLease_TakesOneSlot()
    {
        using var sem = new SemaphoreSlim(2, 2);

        using var lease = await sem.TryAcquireAsync(TimeSpan.FromSeconds(5), _ct);

        Assert.NotNull(lease);
        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task TryAcquireAsync_BudgetElapses_ReturnsNull_DoesNotConsumeSlot()
    {
        using var sem = new SemaphoreSlim(1, 1);
        using var blocker = await sem.AcquireAsync(_ct);

        var lease = await sem.TryAcquireAsync(TimeSpan.FromMilliseconds(50), _ct);

        Assert.Null(lease);
        Assert.Equal(0, sem.CurrentCount);
    }

    [Fact]
    public async Task TryAcquireAsync_Zero_WhenAtCapacity_ReturnsNull_WithoutBlocking()
    {
        using var sem = new SemaphoreSlim(1, 1);
        using var blocker = await sem.AcquireAsync(_ct);

        var lease = await sem.TryAcquireAsync(TimeSpan.Zero, _ct);

        Assert.Null(lease);
    }

    [Fact]
    public async Task TryAcquireAsync_HonorsCancellation_ThrowsOCE_NoSlotConsumed()
    {
        using var sem = new SemaphoreSlim(1, 1);
        using var blocker = await sem.AcquireAsync(_ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var pending = sem.TryAcquireAsync(TimeSpan.FromSeconds(30), cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, sem.CurrentCount);
    }
}
