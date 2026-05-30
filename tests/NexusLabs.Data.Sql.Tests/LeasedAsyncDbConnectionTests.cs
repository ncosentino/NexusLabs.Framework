using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.Tests;

public sealed class LeasedAsyncDbConnectionTests : IDisposable
{
    private readonly MockRepository _mocks = new(MockBehavior.Strict);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public void Dispose() => _mocks.VerifyAll();

    [Fact]
    public void Ctor_RejectsNullInner()
    {
        using var sem = new SemaphoreSlim(1, 1);
        Assert.Throws<ArgumentNullException>(
            () => new LeasedAsyncDbConnection(null!, sem, Timeout.InfiniteTimeSpan));
    }

    [Fact]
    public void Ctor_RejectsNullSemaphore()
    {
        var inner = _mocks.Create<IAsyncDbConnection>().Object;
        Assert.Throws<ArgumentNullException>(
            () => new LeasedAsyncDbConnection(inner, null!, Timeout.InfiniteTimeSpan));
    }

    [Fact]
    public void Ctor_RejectsNegativeAcquisitionTimeout()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>().Object;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LeasedAsyncDbConnection(inner, sem, TimeSpan.FromMilliseconds(-2)));
    }

    [Fact]
    public void Ctor_AcceptsInfiniteAcquisitionTimeout()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        using var sut = new LeasedAsyncDbConnection(
            inner.Object,
            sem,
            Timeout.InfiniteTimeSpan);

        Assert.NotNull(sut);
    }

    [Fact]
    public void Ctor_AcceptsZeroAcquisitionTimeout()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        using var sut = new LeasedAsyncDbConnection(
            inner.Object,
            sem,
            TimeSpan.Zero);

        Assert.NotNull(sut);
    }

    [Fact]
    public async Task OpenAsync_DelegatesToInner_AndAcquiresLease()
    {
        using var sem = new SemaphoreSlim(2, 2);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);
        await sut.OpenAsync(_ct);

        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task Close_DelegatesToInner_AndReleasesLease()
    {
        using var sem = new SemaphoreSlim(2, 2);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.Close());
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);
        await sut.OpenAsync(_ct);
        Assert.Equal(1, sem.CurrentCount);

        sut.Close();

        inner.Verify(c => c.Close(), Times.Once);
        Assert.Equal(2, sem.CurrentCount);
    }

    [Fact]
    public async Task DisposeAsync_DelegatesToInner_AndReleasesLease()
    {
        using var sem = new SemaphoreSlim(2, 2);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);
        await sut.OpenAsync(_ct);
        Assert.Equal(1, sem.CurrentCount);

        await sut.DisposeAsync();

        inner.Verify(c => c.DisposeAsync(), Times.Once);
        Assert.Equal(2, sem.CurrentCount);
    }

    [Fact]
    public async Task OpenAsync_WhenInnerThrows_LeaseIsReleased_AndExceptionPropagates()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("nope"));
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.OpenAsync(_ct));

        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task Close_BeforeAnyOpen_DoesNotReleaseLeaseHeldByAnotherInstance()
    {
        using var sem = new SemaphoreSlim(1, 1);

        var blockerInner = _mocks.Create<IAsyncDbConnection>();
        blockerInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        blockerInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var blocker = new LeasedAsyncDbConnection(blockerInner.Object, sem, Timeout.InfiniteTimeSpan);
        await blocker.OpenAsync(_ct);
        Assert.Equal(0, sem.CurrentCount);

        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.Close());
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);

        sut.Close();

        Assert.Equal(0, sem.CurrentCount);
        inner.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent_InnerDisposedOnce_LeaseReleasedOnce()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);
        await sut.OpenAsync(_ct);

        await sut.DisposeAsync();
        await sut.DisposeAsync();
        await sut.DisposeAsync();

        inner.Verify(c => c.DisposeAsync(), Times.Once);
        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task CloseThenDispose_DoesNotDoubleRelease()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.Close());
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);
        await sut.OpenAsync(_ct);

        sut.Close();
        Assert.Equal(1, sem.CurrentCount);

        await sut.DisposeAsync();
        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task OpenAsync_HonorsCancellation_WhileWaitingOnLease()
    {
        using var sem = new SemaphoreSlim(1, 1);

        var blockerInner = _mocks.Create<IAsyncDbConnection>();
        blockerInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        blockerInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var blocker = new LeasedAsyncDbConnection(blockerInner.Object, sem, Timeout.InfiniteTimeSpan);
        await blocker.OpenAsync(_ct);

        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var pending = sut.OpenAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, sem.CurrentCount);

        await blocker.DisposeAsync();
    }

    [Fact]
    public async Task OpenAsync_CalledTwice_ThrowsInvalidOperationException_NoCapacityLeak()
    {
        using var sem = new SemaphoreSlim(2, 2);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);
        await sut.OpenAsync(_ct);
        Assert.Equal(1, sem.CurrentCount);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.OpenAsync(_ct));

        Assert.Equal(1, sem.CurrentCount);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_PoolExhausted_ThrowsConnectionPoolExhausted_WithMatchingAcquisitionTimeout()
    {
        var budget = TimeSpan.FromMilliseconds(50);
        using var sem = new SemaphoreSlim(1, 1);

        var blockerInner = _mocks.Create<IAsyncDbConnection>();
        blockerInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        blockerInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var blocker = new LeasedAsyncDbConnection(blockerInner.Object, sem, Timeout.InfiniteTimeSpan);
        await blocker.OpenAsync(_ct);
        Assert.Equal(0, sem.CurrentCount);

        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, budget);

        var ex = await Assert.ThrowsAsync<ConnectionPoolExhaustedException>(
            () => sut.OpenAsync(_ct));

        Assert.Equal(budget, ex.AcquisitionTimeout);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OpenAsync_PoolExhausted_DoesNotConsumeSlot_BlockerReleaseAllowsReuse()
    {
        var budget = TimeSpan.FromMilliseconds(50);
        using var sem = new SemaphoreSlim(1, 1);

        var blockerInner = _mocks.Create<IAsyncDbConnection>();
        blockerInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        blockerInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var blocker = new LeasedAsyncDbConnection(blockerInner.Object, sem, Timeout.InfiniteTimeSpan);
        await blocker.OpenAsync(_ct);

        var failingInner = _mocks.Create<IAsyncDbConnection>();
        failingInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using (var failing = new LeasedAsyncDbConnection(failingInner.Object, sem, budget))
        {
            await Assert.ThrowsAsync<ConnectionPoolExhaustedException>(
                () => failing.OpenAsync(_ct));
        }
        Assert.Equal(0, sem.CurrentCount);

        await blocker.DisposeAsync();
        Assert.Equal(1, sem.CurrentCount);

        var nextInner = _mocks.Create<IAsyncDbConnection>();
        nextInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        nextInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var next = new LeasedAsyncDbConnection(nextInner.Object, sem, budget);
        await next.OpenAsync(_ct);
        Assert.Equal(0, sem.CurrentCount);
    }

    [Fact]
    public async Task OpenAsync_HonorsCancellation_BeforeBudgetElapses_ThrowsOCE_NotPoolExhausted()
    {
        using var sem = new SemaphoreSlim(1, 1);

        var blockerInner = _mocks.Create<IAsyncDbConnection>();
        blockerInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        blockerInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var blocker = new LeasedAsyncDbConnection(blockerInner.Object, sem, Timeout.InfiniteTimeSpan);
        await blocker.OpenAsync(_ct);

        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, TimeSpan.FromSeconds(30));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var pending = sut.OpenAsync(cts.Token);
        cts.Cancel();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.IsNotType<ConnectionPoolExhaustedException>(ex);
        Assert.Equal(0, sem.CurrentCount);

        await blocker.DisposeAsync();
    }

    [Fact]
    public async Task OpenAsync_AfterClose_AcquiresNewLease_Succeeds()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.Close());
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);
        await sut.OpenAsync(_ct);
        Assert.Equal(0, sem.CurrentCount);

        sut.Close();
        Assert.Equal(1, sem.CurrentCount);

        await sut.OpenAsync(_ct);

        Assert.Equal(0, sem.CurrentCount);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task WithLease_TimeoutOverload_ProducesLeasedConnection_AcquiringSlotOnOpen()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = inner.Object.WithLease(sem, Timeout.InfiniteTimeSpan);
        Assert.IsType<LeasedAsyncDbConnection>(sut);

        await sut.OpenAsync(_ct);
        Assert.Equal(0, sem.CurrentCount);
    }

    [Fact]
    public async Task WithLease_TimeoutOverload_ExhaustedPool_ThrowsConnectionPoolExhausted()
    {
        var budget = TimeSpan.FromMilliseconds(50);
        using var sem = new SemaphoreSlim(1, 1);

        var blockerInner = _mocks.Create<IAsyncDbConnection>();
        blockerInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        blockerInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var blocker = blockerInner.Object.WithLease(sem, Timeout.InfiniteTimeSpan);
        await blocker.OpenAsync(_ct);

        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = inner.Object.WithLease(sem, budget);

        var ex = await Assert.ThrowsAsync<ConnectionPoolExhaustedException>(
            () => sut.OpenAsync(_ct));
        Assert.Equal(budget, ex.AcquisitionTimeout);
    }

    [Fact]
    public async Task OpenAsync_ConcurrentRacePastEarlyGuard_RetainsOnlyOneLease_NoSlotLeak()
    {
        const int N = 8;
        using var sem = new SemaphoreSlim(N, N);
        using var entered = new SemaphoreSlim(0, N);
        var releaseGate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var inner = _mocks.Create<IAsyncDbConnection>();
        inner
            .Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                entered.Release();
                await releaseGate.Task.ConfigureAwait(false);
            });
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);

        var openTasks = new Task[N];
        for (var i = 0; i < N; i++)
        {
            openTasks[i] = Task.Run(() => sut.OpenAsync(_ct), _ct);
        }

        for (var i = 0; i < N; i++)
        {
            await entered.WaitAsync(_ct);
        }

        Assert.Equal(0, sem.CurrentCount);

        releaseGate.SetResult();

        await Task.WhenAll(openTasks);

        Assert.Equal(N - 1, sem.CurrentCount);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Exactly(N));

        await sut.DisposeAsync();

        Assert.Equal(N, sem.CurrentCount);
    }

    [Fact]
    public async Task PropertyAccess_DelegatesToInner()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.SetupGet(c => c.ConnectionTimeout).Returns(42);
        inner.SetupGet(c => c.Database).Returns("mydb");
        inner.SetupGet(c => c.State).Returns(System.Data.ConnectionState.Open);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem, Timeout.InfiniteTimeSpan);

        Assert.Equal(42, sut.ConnectionTimeout);
        Assert.Equal("mydb", sut.Database);
        Assert.Equal(System.Data.ConnectionState.Open, sut.State);
    }
}
