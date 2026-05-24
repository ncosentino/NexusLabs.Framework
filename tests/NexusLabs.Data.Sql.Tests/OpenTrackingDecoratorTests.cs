using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.Tests;

public sealed class OpenTrackingDecoratorTests
{
    private static Mock<IAsyncDbConnection> NewMockInner()
    {
        var inner = new Mock<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.Close());
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return inner;
    }

    [Fact]
    public void Ctor_RejectsNullArgs()
    {
        var tracker = new OpenConnectionTracker();
        var inner = new Mock<IAsyncDbConnection>().Object;

        Assert.Throws<ArgumentNullException>(
            () => new OpenTrackingDecorator(null!, tracker));
        Assert.Throws<ArgumentNullException>(
            () => new OpenTrackingDecorator(inner, null!));
    }

    [Fact]
    public async Task OpenAsync_RegistersEntry()
    {
        var tracker = new OpenConnectionTracker();
        await using var sut = new OpenTrackingDecorator(NewMockInner().Object, tracker);

        await sut.OpenAsync(CancellationToken.None);

        var open = tracker.GetOpenConnections();
        Assert.Single(open);
        Assert.False(string.IsNullOrEmpty(open[0].Callstack));
    }

    [Fact]
    public async Task Close_UnregistersEntry()
    {
        var tracker = new OpenConnectionTracker();
        await using var sut = new OpenTrackingDecorator(NewMockInner().Object, tracker);

        await sut.OpenAsync(CancellationToken.None);
        sut.Close();

        Assert.Empty(tracker.GetOpenConnections());
    }

    [Fact]
    public async Task DisposeAsync_UnregistersEntry()
    {
        var tracker = new OpenConnectionTracker();
        var sut = new OpenTrackingDecorator(NewMockInner().Object, tracker);

        await sut.OpenAsync(CancellationToken.None);
        await sut.DisposeAsync();

        Assert.Empty(tracker.GetOpenConnections());
    }

    [Fact]
    public async Task FailedOpen_LeavesNoEntry()
    {
        var tracker = new OpenConnectionTracker();
        var inner = new Mock<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("nope"));
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = new OpenTrackingDecorator(inner.Object, tracker);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.OpenAsync(CancellationToken.None));

        Assert.Empty(tracker.GetOpenConnections());
    }

    [Fact]
    public async Task MultipleConnections_TrackedIndependently()
    {
        var tracker = new OpenConnectionTracker();
        await using var a = new OpenTrackingDecorator(NewMockInner().Object, tracker);
        await using var b = new OpenTrackingDecorator(NewMockInner().Object, tracker);

        await a.OpenAsync(CancellationToken.None);
        await b.OpenAsync(CancellationToken.None);

        Assert.Equal(2, tracker.GetOpenConnections().Count);

        a.Close();
        Assert.Single(tracker.GetOpenConnections());

        b.Close();
        Assert.Empty(tracker.GetOpenConnections());
    }

    [Fact]
    public async Task GetOpenConnections_OrdersByOpenedAt()
    {
        var tracker = new OpenConnectionTracker();
        await using var a = new OpenTrackingDecorator(NewMockInner().Object, tracker);
        await using var b = new OpenTrackingDecorator(NewMockInner().Object, tracker);

        await a.OpenAsync(CancellationToken.None);
        await Task.Delay(20);
        await b.OpenAsync(CancellationToken.None);

        var entries = tracker.GetOpenConnections();
        Assert.Equal(2, entries.Count);
        Assert.True(entries[0].OpenedAt <= entries[1].OpenedAt);
    }
}
