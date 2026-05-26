using System;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Framework.Tests;

public sealed class TriedExDisposeTests
{
    [Fact]
    private void Dispose_NonDisposableValue_NoOp()
    {
        var tried = new TriedEx<int>(42);

        // Must not throw, must not corrupt state.
        tried.Dispose();
    }

    [Fact]
    private void Dispose_SuccessTrue_DisposableValue_Disposes()
    {
        var spy = new DisposableSpy();
        var tried = new TriedEx<DisposableSpy>(spy);

        tried.Dispose();

        Assert.Equal(1, spy.DisposeCount);
    }

    [Fact]
    private void Dispose_SuccessFalse_DisposableErrorWouldBe_DoesNotDisposeError()
    {
        // Sanity: even if exception type implements IDisposable (here we use it via custom),
        // Dispose only acts on the Value branch.
        var error = new DisposableException();
        var tried = new TriedEx<DisposableSpy>(error);

        tried.Dispose();

        Assert.Equal(0, error.DisposeCount);
    }

    [Fact]
    private void UsingStatement_DisposableValue_DisposesAtScopeExit()
    {
        var spy = new DisposableSpy();
        using (var tried = new TriedEx<DisposableSpy>(spy))
        {
            Assert.Equal(0, spy.DisposeCount);
        }

        Assert.Equal(1, spy.DisposeCount);
    }

    [Fact]
    private async Task DisposeAsync_PrefersIAsyncDisposable_OverSyncIDisposable()
    {
        var spy = new BothDisposableSpy();
        var tried = new TriedEx<BothDisposableSpy>(spy);

        await tried.DisposeAsync();

        Assert.Equal(1, spy.AsyncDisposeCount);
        Assert.Equal(0, spy.SyncDisposeCount);
    }

    [Fact]
    private async Task DisposeAsync_OnlySyncDisposable_FallsBackToSync()
    {
        var spy = new DisposableSpy();
        var tried = new TriedEx<DisposableSpy>(spy);

        await tried.DisposeAsync();

        Assert.Equal(1, spy.DisposeCount);
    }

    [Fact]
    private async Task DisposeAsync_NonDisposableValue_NoOp()
    {
        var tried = new TriedEx<int>(42);

        await tried.DisposeAsync();
    }

    [Fact]
    private async Task AwaitUsingStatement_AsyncDisposable_DisposesAsynchronously()
    {
        var spy = new BothDisposableSpy();
        await using (var tried = new TriedEx<BothDisposableSpy>(spy))
        {
            Assert.Equal(0, spy.AsyncDisposeCount);
        }

        Assert.Equal(1, spy.AsyncDisposeCount);
        Assert.Equal(0, spy.SyncDisposeCount);
    }
}

public sealed class TriedNullExDisposeTests
{
    [Fact]
    private void Dispose_NullValue_NoOp()
    {
        var tried = new TriedNullEx<DisposableSpy?>((DisposableSpy?)null);

        tried.Dispose();
    }

    [Fact]
    private void Dispose_SuccessTrue_DisposableValue_Disposes()
    {
        var spy = new DisposableSpy();
        var tried = new TriedNullEx<DisposableSpy?>(spy);

        tried.Dispose();

        Assert.Equal(1, spy.DisposeCount);
    }

    [Fact]
    private void Dispose_SuccessFalse_DoesNotDispose()
    {
        var spy = new DisposableSpy();
        TriedNullEx<DisposableSpy?> tried = new InvalidOperationException("boom");

        tried.Dispose();

        Assert.Equal(0, spy.DisposeCount);
    }

    [Fact]
    private async Task DisposeAsync_PrefersIAsyncDisposable_OverSyncIDisposable()
    {
        var spy = new BothDisposableSpy();
        var tried = new TriedNullEx<BothDisposableSpy?>(spy);

        await tried.DisposeAsync();

        Assert.Equal(1, spy.AsyncDisposeCount);
        Assert.Equal(0, spy.SyncDisposeCount);
    }

    [Fact]
    private async Task DisposeAsync_NullValue_NoOp()
    {
        var tried = new TriedNullEx<DisposableSpy?>((DisposableSpy?)null);

        await tried.DisposeAsync();
    }

    [Fact]
    private async Task AwaitUsingStatement_AsyncDisposable_DisposesAsynchronously()
    {
        var spy = new BothDisposableSpy();
        await using (var tried = new TriedNullEx<BothDisposableSpy?>(spy))
        {
            Assert.Equal(0, spy.AsyncDisposeCount);
        }

        Assert.Equal(1, spy.AsyncDisposeCount);
    }
}

public sealed class TriedDisposeTests
{
    [Fact]
    private void Dispose_NonDisposableValue_NoOp()
    {
        var tried = new Tried<int>(42);

        tried.Dispose();
    }

    [Fact]
    private void Dispose_SuccessTrue_DisposableValue_Disposes()
    {
        var spy = new DisposableSpy();
        var tried = new Tried<DisposableSpy>(spy);

        tried.Dispose();

        Assert.Equal(1, spy.DisposeCount);
    }

    [Fact]
    private void Dispose_Failed_NoOp()
    {
        var tried = Tried<DisposableSpy>.Failed;

        tried.Dispose();
    }

    [Fact]
    private async Task DisposeAsync_PrefersIAsyncDisposable_OverSyncIDisposable()
    {
        var spy = new BothDisposableSpy();
        var tried = new Tried<BothDisposableSpy>(spy);

        await tried.DisposeAsync();

        Assert.Equal(1, spy.AsyncDisposeCount);
        Assert.Equal(0, spy.SyncDisposeCount);
    }

    [Fact]
    private async Task AwaitUsingStatement_AsyncDisposable_DisposesAsynchronously()
    {
        var spy = new BothDisposableSpy();
        await using (var tried = new Tried<BothDisposableSpy>(spy))
        {
            Assert.Equal(0, spy.AsyncDisposeCount);
        }

        Assert.Equal(1, spy.AsyncDisposeCount);
    }
}

internal sealed class DisposableSpy : IDisposable
{
    public int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;
}

internal sealed class BothDisposableSpy : IDisposable, IAsyncDisposable
{
    public int SyncDisposeCount { get; private set; }
    public int AsyncDisposeCount { get; private set; }

    public void Dispose() => SyncDisposeCount++;

    public ValueTask DisposeAsync()
    {
        AsyncDisposeCount++;
        return ValueTask.CompletedTask;
    }
}

internal sealed class DisposableException : Exception, IDisposable
{
    public int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;
}
