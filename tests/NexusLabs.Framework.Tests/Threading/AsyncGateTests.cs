using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NexusLabs.Framework.Threading;

using Xunit;

namespace NexusLabs.Framework.Tests.Threading;

public sealed class AsyncGateTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public void Ctor_Default_GateIsClosed()
    {
        using var gate = new AsyncGate();

        Assert.False(gate.IsSet, "Expected a default gate to start closed.");
    }

    [Fact]
    public void Ctor_IsSetTrue_GateIsOpen()
    {
        using var gate = new AsyncGate(isSet: true);

        Assert.True(gate.IsSet, "Expected a gate constructed with isSet:true to start open.");
    }

    [Fact]
    public async Task WaitAsync_AlreadyOpen_CompletesSynchronously()
    {
        using var gate = new AsyncGate(isSet: true);

        var wait = gate.WaitAsync(_ct);

        Assert.True(
            wait.IsCompletedSuccessfully,
            "Expected WaitAsync on an already-open gate to complete synchronously.");
        await wait;
    }

    [Fact]
    public async Task WaitAsync_ClosedGate_StaysPendingUntilSet()
    {
        using var gate = new AsyncGate();

        var wait = gate.WaitAsync(_ct);
        Assert.False(wait.IsCompleted, "Expected the waiter to be pending while the gate is closed.");

        gate.Set();

        await wait;
        Assert.True(wait.IsCompletedSuccessfully, "Expected the waiter to complete once the gate opened.");
    }

    [Fact]
    public void Set_OpensGate()
    {
        using var gate = new AsyncGate();

        gate.Set();

        Assert.True(gate.IsSet, "Expected the gate to be open after Set.");
    }

    [Fact]
    public async Task Set_ReleasesAllParkedWaiters()
    {
        using var gate = new AsyncGate();
        const int waiterCount = 8;
        var parked = 0;
        var released = 0;

        async Task Probe()
        {
            Interlocked.Increment(ref parked);
            await gate.WaitAsync(_ct);
            Interlocked.Increment(ref released);
        }

        var waiters = Enumerable
            .Range(0, waiterCount)
            .Select(_ => Task.Run(Probe, _ct))
            .ToArray();

        var allParked = await PollUntilAsync(() => Volatile.Read(ref parked) == waiterCount);
        Assert.True(allParked, $"Expected all {waiterCount} callers to park on the gate.");
        Assert.Equal(0, Volatile.Read(ref released));

        gate.Set();

        await Task.WhenAll(waiters);
        Assert.Equal(waiterCount, released);
    }

    [Fact]
    public void Set_CalledMultipleTimes_RemainsOpen()
    {
        using var gate = new AsyncGate();

        gate.Set();
        gate.Set();
        gate.Set();

        Assert.True(gate.IsSet, "Expected repeated Set calls to leave the gate open.");
    }

    [Fact]
    public async Task Set_IsThreadSafe_UnderConcurrentCallers()
    {
        using var gate = new AsyncGate();
        var waiter = gate.WaitAsync(_ct);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var setters = Enumerable
            .Range(0, 64)
            .Select(_ => Task.Run(
                async () =>
                {
                    await start.Task;
                    gate.Set();
                },
                _ct))
            .ToArray();

        start.SetResult();
        await Task.WhenAll(setters);

        Assert.True(gate.IsSet, "Expected the gate to be open after concurrent Set calls.");
        await waiter;
        Assert.True(
            waiter.IsCompletedSuccessfully,
            "Expected the parked waiter to be released by concurrent Set calls.");
    }

    [Fact]
    public async Task Reset_ClosesOpenGate()
    {
        using var gate = new AsyncGate(isSet: true);

        gate.Reset();

        Assert.False(gate.IsSet, "Expected the gate to be closed after Reset.");
        var wait = gate.WaitAsync(_ct);
        Assert.False(wait.IsCompleted, "Expected a waiter to park again after Reset.");

        gate.Set();
        await wait;
    }

    [Fact]
    public async Task Reset_AfterSet_ReblocksNewWaiters_PreviousWaiterStaysCompleted()
    {
        using var gate = new AsyncGate();
        var firstWait = gate.WaitAsync(_ct);
        gate.Set();
        await firstWait;
        Assert.True(
            firstWait.IsCompletedSuccessfully,
            "Expected the first waiter to complete once the gate opened.");

        gate.Reset();
        Assert.False(gate.IsSet, "Expected the gate to be closed after Reset.");

        var secondWait = gate.WaitAsync(_ct);
        Assert.False(secondWait.IsCompleted, "Expected a new waiter to park after Reset.");

        gate.Set();
        await secondWait;
        Assert.True(
            secondWait.IsCompletedSuccessfully,
            "Expected the second waiter to complete after the gate reopened.");
    }

    [Fact]
    public void Reset_OnClosedGate_IsNoOp()
    {
        using var gate = new AsyncGate();

        gate.Reset();

        Assert.False(gate.IsSet, "Expected Reset on an already-closed gate to leave it closed.");
    }

    [Fact]
    public async Task SetReset_RepeatedCycles_EachCycleParksThenReleases()
    {
        using var gate = new AsyncGate();

        for (var cycle = 0; cycle < 5; cycle++)
        {
            var wait = gate.WaitAsync(_ct);
            Assert.False(
                wait.IsCompleted,
                $"Expected the waiter to park on cycle {cycle} while the gate is closed.");

            gate.Set();
            await wait;
            Assert.True(
                wait.IsCompletedSuccessfully,
                $"Expected the waiter to be released on cycle {cycle} after Set.");
            Assert.True(gate.IsSet, $"Expected the gate to be open on cycle {cycle} after Set.");

            gate.Reset();
            Assert.False(gate.IsSet, $"Expected the gate to be closed on cycle {cycle} after Reset.");
        }
    }

    [Fact]
    public async Task WaitAsync_CancelledWhileWaiting_ThrowsOperationCanceled()
    {
        using var gate = new AsyncGate();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);

        var pending = gate.WaitAsync(cts.Token);
        Assert.False(pending.IsCompleted, "Expected the waiter to be pending before cancellation.");

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task WaitAsync_PreCancelledToken_ClosedGate_ThrowsOperationCanceled()
    {
        using var gate = new AsyncGate();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task WaitAsync_PreCancelledToken_OpenGate_StillCompletes()
    {
        using var gate = new AsyncGate(isSet: true);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        cts.Cancel();

        var wait = gate.WaitAsync(cts.Token);

        await wait;
        Assert.True(
            wait.IsCompletedSuccessfully,
            "Expected an already-open gate to complete even when the token is already cancelled.");
    }

    [Fact]
    public async Task WaitAsync_SeparateCalls_ReturnIndependentWaiters()
    {
        using var gate = new AsyncGate();
        using var ctsA = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        using var ctsB = CancellationTokenSource.CreateLinkedTokenSource(_ct);

        var waitA = gate.WaitAsync(ctsA.Token);
        var waitB = gate.WaitAsync(ctsB.Token);

        Assert.NotSame(waitA, waitB);
        Assert.False(waitA.IsCompleted, "Expected the first waiter to be pending.");
        Assert.False(waitB.IsCompleted, "Expected the second waiter to be pending.");

        ctsA.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitA);
        Assert.False(
            waitB.IsCompleted,
            "Expected the second waiter to stay pending after the first waiter's token was cancelled.");

        gate.Set();
        await waitB;
        Assert.True(
            waitB.IsCompletedSuccessfully,
            "Expected the second waiter to complete once the gate opened.");
    }

    [Fact]
    public async Task WaitAsync_AfterReset_ReturnsWaiterDistinctFromPreResetWaiter()
    {
        using var gate = new AsyncGate(isSet: true);

        var beforeReset = gate.WaitAsync(_ct);
        await beforeReset;
        Assert.True(
            beforeReset.IsCompletedSuccessfully,
            "Expected the pre-reset waiter to be completed on an open gate.");

        gate.Reset();

        var afterReset = gate.WaitAsync(_ct);
        Assert.NotSame(beforeReset, afterReset);
        Assert.False(
            afterReset.IsCompleted,
            "Expected the post-reset waiter to park on the now-closed gate.");

        gate.Set();
        await afterReset;
        Assert.True(
            afterReset.IsCompletedSuccessfully,
            "Expected the post-reset waiter to complete after the gate reopened.");
    }

    [Fact]
    public async Task Dispose_CancelsPendingWaiters()
    {
        var gate = new AsyncGate();
        var waiters = Enumerable
            .Range(0, 4)
            .Select(_ => gate.WaitAsync(_ct))
            .ToArray();
        Assert.All(
            waiters,
            w => Assert.False(w.IsCompleted, "Expected each waiter to be pending before dispose."));

        gate.Dispose();

        foreach (var waiter in waiters)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
        }
    }

    [Fact]
    public async Task Dispose_AfterSet_LeavesReleasedWaitersCompleted()
    {
        var gate = new AsyncGate();
        var wait = gate.WaitAsync(_ct);
        gate.Set();
        await wait;

        gate.Dispose();

        Assert.True(
            wait.IsCompletedSuccessfully,
            "Expected a waiter released before dispose to stay successfully completed.");
    }

    [Fact]
    public async Task Dispose_WithReleasedAndPendingWaiters_CancelsOnlyThePendingOne()
    {
        var gate = new AsyncGate();

        var releasedWaiter = gate.WaitAsync(_ct);
        gate.Set();
        await releasedWaiter;
        Assert.True(
            releasedWaiter.IsCompletedSuccessfully,
            "Expected the first waiter to be released by Set before the gate was reset.");

        gate.Reset();
        var pendingWaiter = gate.WaitAsync(_ct);
        Assert.False(
            pendingWaiter.IsCompleted,
            "Expected the post-reset waiter to be parked before dispose.");

        gate.Dispose();

        Assert.True(
            releasedWaiter.IsCompletedSuccessfully,
            "Expected the already-released waiter to stay successfully completed after dispose.");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingWaiter);
    }

    [Fact]
    public void Dispose_IsIdempotent_Sequential()
    {
        var gate = new AsyncGate();

        gate.Dispose();
        gate.Dispose();
        gate.Dispose();

        Assert.Throws<ObjectDisposedException>(() => gate.Set());
    }

    [Fact]
    public async Task Dispose_IsIdempotent_UnderConcurrency()
    {
        var gate = new AsyncGate();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var disposers = Enumerable
            .Range(0, 64)
            .Select(_ => Task.Run(
                async () =>
                {
                    await start.Task;
                    gate.Dispose();
                },
                _ct))
            .ToArray();

        start.SetResult();
        await Task.WhenAll(disposers);

        Assert.Throws<ObjectDisposedException>(() => gate.Set());
    }

    [Fact]
    public async Task WaitAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var gate = new AsyncGate();
        gate.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await gate.WaitAsync(_ct));
    }

    [Fact]
    public void Set_AfterDispose_ThrowsObjectDisposed()
    {
        var gate = new AsyncGate();
        gate.Dispose();

        Assert.Throws<ObjectDisposedException>(() => gate.Set());
    }

    [Fact]
    public void Reset_AfterDispose_ThrowsObjectDisposed()
    {
        var gate = new AsyncGate();
        gate.Dispose();

        Assert.Throws<ObjectDisposedException>(() => gate.Reset());
    }

    [Fact]
    public void IsSet_AfterDispose_ThrowsObjectDisposed()
    {
        var gate = new AsyncGate();
        gate.Dispose();

        Assert.Throws<ObjectDisposedException>(() => { _ = gate.IsSet; });
    }

    private static async Task<bool> PollUntilAsync(Func<bool> predicate)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
            {
                return true;
            }

            await Task.Yield();
        }

        return predicate();
    }
}
