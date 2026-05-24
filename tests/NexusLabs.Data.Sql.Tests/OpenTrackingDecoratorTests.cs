using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Time.Testing;

using Moq;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.Tests;

public sealed class OpenTrackingDecoratorTests : IDisposable
{
    private static readonly DateTimeOffset DefaultStart =
        new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly MockRepository _mocks = new(MockBehavior.Strict);

    public void Dispose() => _mocks.VerifyAll();

    private static FakeTimeProvider NewFakeTimeProvider() =>
        new(startDateTime: DefaultStart);

    private Mock<IAsyncDbConnection> NewMockInner(bool expectClose = false)
    {
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        if (expectClose)
        {
            inner.Setup(c => c.Close());
        }
        return inner;
    }

    [Fact]
    public void Ctor_RejectsNullArgs()
    {
        var tracker = new OpenConnectionTracker();
        var inner = _mocks.Create<IAsyncDbConnection>().Object;
        var timeProvider = NewFakeTimeProvider();

        Assert.Throws<ArgumentNullException>(
            () => new OpenTrackingDecorator(null!, tracker, timeProvider));
        Assert.Throws<ArgumentNullException>(
            () => new OpenTrackingDecorator(inner, null!, timeProvider));
        Assert.Throws<ArgumentNullException>(
            () => new OpenTrackingDecorator(inner, tracker, null!));
    }

    [Fact]
    public async Task OpenAsync_RegistersEntry()
    {
        var tracker = new OpenConnectionTracker();
        var timeProvider = NewFakeTimeProvider();
        await using var sut = new OpenTrackingDecorator(NewMockInner().Object, tracker, timeProvider);

        await sut.OpenAsync(CancellationToken.None);

        var open = tracker.GetOpenConnections();
        Assert.Single(open);
        Assert.False(string.IsNullOrEmpty(open[0].Callstack));
    }

    [Fact]
    public async Task OpenAsync_UsesTimeProviderForTimestamp()
    {
        var tracker = new OpenConnectionTracker();
        var timeProvider = NewFakeTimeProvider();
        await using var sut = new OpenTrackingDecorator(NewMockInner().Object, tracker, timeProvider);

        await sut.OpenAsync(CancellationToken.None);

        var open = tracker.GetOpenConnections();
        Assert.Single(open);
        Assert.Equal(DefaultStart, open[0].OpenedAt);
    }

    [Fact]
    public async Task Close_UnregistersEntry()
    {
        var tracker = new OpenConnectionTracker();
        var timeProvider = NewFakeTimeProvider();
        await using var sut = new OpenTrackingDecorator(NewMockInner(expectClose: true).Object, tracker, timeProvider);

        await sut.OpenAsync(CancellationToken.None);
        sut.Close();

        Assert.Empty(tracker.GetOpenConnections());
    }

    [Fact]
    public async Task DisposeAsync_UnregistersEntry()
    {
        var tracker = new OpenConnectionTracker();
        var timeProvider = NewFakeTimeProvider();
        var sut = new OpenTrackingDecorator(NewMockInner().Object, tracker, timeProvider);

        await sut.OpenAsync(CancellationToken.None);
        await sut.DisposeAsync();

        Assert.Empty(tracker.GetOpenConnections());
    }

    [Fact]
    public async Task FailedOpen_LeavesNoEntry()
    {
        var tracker = new OpenConnectionTracker();
        var timeProvider = NewFakeTimeProvider();
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("nope"));
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        await using var sut = new OpenTrackingDecorator(inner.Object, tracker, timeProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.OpenAsync(CancellationToken.None));

        Assert.Empty(tracker.GetOpenConnections());
    }

    [Fact]
    public async Task MultipleConnections_TrackedIndependently()
    {
        var tracker = new OpenConnectionTracker();
        var timeProvider = NewFakeTimeProvider();
        await using var a = new OpenTrackingDecorator(NewMockInner(expectClose: true).Object, tracker, timeProvider);
        await using var b = new OpenTrackingDecorator(NewMockInner(expectClose: true).Object, tracker, timeProvider);

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
        var timeProvider = NewFakeTimeProvider();
        await using var a = new OpenTrackingDecorator(NewMockInner().Object, tracker, timeProvider);
        await using var b = new OpenTrackingDecorator(NewMockInner().Object, tracker, timeProvider);

        await a.OpenAsync(CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromMinutes(5));
        await b.OpenAsync(CancellationToken.None);

        var entries = tracker.GetOpenConnections();
        Assert.Equal(2, entries.Count);
        Assert.Equal(DefaultStart, entries[0].OpenedAt);
        Assert.Equal(DefaultStart.AddMinutes(5), entries[1].OpenedAt);
    }
}
