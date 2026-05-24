using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.Tests;

public sealed class LeasedAsyncDbConnectionTests
{
    [Fact]
    public void Ctor_RejectsNullInner()
    {
        using var sem = new SemaphoreSlim(1, 1);
        Assert.Throws<ArgumentNullException>(
            () => new LeasedAsyncDbConnection(null!, sem));
    }

    [Fact]
    public void Ctor_RejectsNullSemaphore()
    {
        var inner = new Mock<IAsyncDbConnection>().Object;
        Assert.Throws<ArgumentNullException>(
            () => new LeasedAsyncDbConnection(inner, null!));
    }

    [Fact]
    public async Task OpenAsync_DelegatesToInner_AndAcquiresLease()
    {
        using var sem = new SemaphoreSlim(2, 2);
        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem);
        await sut.OpenAsync(CancellationToken.None);

        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task Close_DelegatesToInner_AndReleasesLease()
    {
        using var sem = new SemaphoreSlim(2, 2);
        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.Close());
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem);
        await sut.OpenAsync(CancellationToken.None);
        Assert.Equal(1, sem.CurrentCount);

        sut.Close();

        inner.Verify(c => c.Close(), Times.Once);
        Assert.Equal(2, sem.CurrentCount);
    }

    [Fact]
    public async Task DisposeAsync_DelegatesToInner_AndReleasesLease()
    {
        using var sem = new SemaphoreSlim(2, 2);
        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var sut = new LeasedAsyncDbConnection(inner.Object, sem);
        await sut.OpenAsync(CancellationToken.None);
        Assert.Equal(1, sem.CurrentCount);

        await sut.DisposeAsync();

        inner.Verify(c => c.DisposeAsync(), Times.Once);
        Assert.Equal(2, sem.CurrentCount);
    }

    [Fact]
    public async Task OpenAsync_WhenInnerThrows_LeaseIsReleased_AndExceptionPropagates()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("nope"));
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.OpenAsync(CancellationToken.None));

        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task Close_BeforeAnyOpen_DoesNotReleaseLeaseHeldByAnotherInstance()
    {
        using var sem = new SemaphoreSlim(1, 1);

        var blockerInner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        blockerInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        blockerInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var blocker = new LeasedAsyncDbConnection(blockerInner.Object, sem);
        await blocker.OpenAsync(CancellationToken.None);
        Assert.Equal(0, sem.CurrentCount);

        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.Close());
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem);

        sut.Close();

        Assert.Equal(0, sem.CurrentCount);
        inner.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent_InnerDisposedOnce_LeaseReleasedOnce()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var sut = new LeasedAsyncDbConnection(inner.Object, sem);
        await sut.OpenAsync(CancellationToken.None);

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
        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.Close());
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var sut = new LeasedAsyncDbConnection(inner.Object, sem);
        await sut.OpenAsync(CancellationToken.None);

        sut.Close();
        Assert.Equal(1, sem.CurrentCount);

        await sut.DisposeAsync();
        Assert.Equal(1, sem.CurrentCount);
    }

    [Fact]
    public async Task OpenAsync_HonorsCancellation_WhileWaitingOnLease()
    {
        using var sem = new SemaphoreSlim(1, 1);

        var blockerInner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        blockerInner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        blockerInner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var blocker = new LeasedAsyncDbConnection(blockerInner.Object, sem);
        await blocker.OpenAsync(CancellationToken.None);

        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem);

        using var cts = new CancellationTokenSource();
        var pending = sut.OpenAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, sem.CurrentCount);

        await blocker.DisposeAsync();
    }

    [Fact]
    public async Task OpenAsync_CalledTwice_ReleasesPriorLease_NoCapacityLeak()
    {
        using var sem = new SemaphoreSlim(2, 2);
        var inner = new Mock<IAsyncDbConnection>(MockBehavior.Strict);
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem);
        await sut.OpenAsync(CancellationToken.None);
        Assert.Equal(1, sem.CurrentCount);

        await sut.OpenAsync(CancellationToken.None);

        Assert.Equal(1, sem.CurrentCount);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PropertyAccess_DelegatesToInner()
    {
        using var sem = new SemaphoreSlim(1, 1);
        var inner = new Mock<IAsyncDbConnection>();
        inner.SetupGet(c => c.ConnectionTimeout).Returns(42);
        inner.SetupGet(c => c.Database).Returns("mydb");
        inner.SetupGet(c => c.State).Returns(System.Data.ConnectionState.Open);

        await using var sut = new LeasedAsyncDbConnection(inner.Object, sem);

        Assert.Equal(42, sut.ConnectionTimeout);
        Assert.Equal("mydb", sut.Database);
        Assert.Equal(System.Data.ConnectionState.Open, sut.State);
    }
}
