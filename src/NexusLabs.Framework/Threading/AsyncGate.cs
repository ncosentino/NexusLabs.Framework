using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Threading;

/// <summary>
/// An async, manual-reset gate: an awaitable signal that callers park on until it is
/// opened. Mirrors the shape of <see cref="ManualResetEventSlim"/> (<see cref="Set"/>,
/// <see cref="Reset"/>, <see cref="IsSet"/>) but exposes an awaitable
/// <see cref="WaitAsync(CancellationToken)"/> instead of a blocking wait, so no
/// thread-pool thread is held while waiting.
/// <code>
/// using var gate = new AsyncGate();
/// // worker A:
/// await gate.WaitAsync(cancellationToken); // parks until the gate is opened
/// // coordinator:
/// gate.Set();                              // releases every current and future waiter
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The gate is built on a <see cref="TaskCompletionSource"/> created with
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/>, so a waiter's
/// continuation never runs inline on the thread that calls <see cref="Set"/>. This
/// removes the most common foot-guns of hand-rolling the pattern (forgetting the
/// option and deadlocking when signalling from inside a lock, or distorting concurrency
/// measurements by inlining work onto the signalling thread).
/// </para>
/// <para>
/// Waiters are released by calling <see cref="Set"/> — not by disposing. This is the
/// key difference from a resource lease such as <see cref="AsyncSemaphoreLease"/>,
/// where disposal is the release. An <see cref="AsyncGate"/> is the opposite:
/// <see cref="Set"/> completes parked waiters successfully, while <see cref="Dispose"/>
/// is a scope-exit teardown that cancels anyone still parked. Do not rely on
/// <c>using</c> alone to release waiters: a gate disposed without ever being
/// <see cref="Set"/> faults its waiters rather than completing them.
/// </para>
/// <para>
/// Concretely, <see cref="Dispose"/> cancels every pending
/// <see cref="WaitAsync(CancellationToken)"/> so no caller is left parked forever:
/// abandoned waiters observe an <see cref="OperationCanceledException"/>, never a normal
/// completion. Waiters already released by a prior <see cref="Set"/> keep their
/// successful result and are unaffected. After disposal every member except
/// <see cref="Dispose"/> throws <see cref="ObjectDisposedException"/>.
/// <see cref="Dispose"/> itself is idempotent and thread-safe.
/// </para>
/// </remarks>
public sealed class AsyncGate : IDisposable
{
    private readonly Lock _gate = new();
    private TaskCompletionSource _signal;
    private bool _disposed;

    /// <summary>
    /// Creates a gate in the closed (unsignaled) state. Callers of
    /// <see cref="WaitAsync(CancellationToken)"/> park until <see cref="Set"/> is called.
    /// </summary>
    public AsyncGate()
        : this(isSet: false)
    {
    }

    /// <summary>
    /// Creates a gate in the specified initial state.
    /// </summary>
    /// <param name="isSet">
    /// <see langword="true"/> to start opened, so <see cref="WaitAsync(CancellationToken)"/>
    /// completes immediately until <see cref="Reset"/> is called; <see langword="false"/>
    /// to start closed.
    /// </param>
    public AsyncGate(bool isSet)
    {
        _signal = CreateSignal();
        if (isSet)
        {
            _signal.SetResult();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the gate is currently open. While open,
    /// <see cref="WaitAsync(CancellationToken)"/> completes synchronously.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the gate has been disposed.</exception>
    public bool IsSet
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                return _signal.Task.IsCompletedSuccessfully;
            }
        }
    }

    /// <summary>
    /// Waits asynchronously until the gate is opened. If the gate is already open the
    /// returned task is already completed. While the gate is closed the returned task
    /// stays pending until <see cref="Set"/> opens it, <paramref name="cancellationToken"/>
    /// is cancelled, or the gate is disposed.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token observed while waiting. Cancellation faults the returned task with an
    /// <see cref="OperationCanceledException"/>; it does not open the gate for other waiters.
    /// </param>
    /// <returns>A task that completes when the gate is opened.</returns>
    /// <exception cref="ObjectDisposedException">If the gate has been disposed.</exception>
    /// <exception cref="OperationCanceledException">
    /// Surfaced through the returned task if <paramref name="cancellationToken"/> is
    /// cancelled while waiting, or if the gate is disposed while waiting.
    /// </exception>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        Task signalTask;
        lock (_gate)
        {
            ThrowIfDisposed();
            signalTask = _signal.Task;
        }

        return signalTask.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Opens the gate, releasing every current and future waiter until <see cref="Reset"/>
    /// is called. Idempotent: calling <see cref="Set"/> on an already-open gate is a no-op.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the gate has been disposed.</exception>
    public void Set()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            _signal.TrySetResult();
        }
    }

    /// <summary>
    /// Closes the gate so that subsequent callers of <see cref="WaitAsync(CancellationToken)"/>
    /// park again. Waiters already released by a prior <see cref="Set"/> are unaffected.
    /// Idempotent: calling <see cref="Reset"/> on an already-closed gate is a no-op.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the gate has been disposed.</exception>
    public void Reset()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_signal.Task.IsCompletedSuccessfully)
            {
                _signal = CreateSignal();
            }
        }
    }

    /// <summary>
    /// Tears the gate down, cancelling every pending <see cref="WaitAsync(CancellationToken)"/>
    /// so no caller is left parked. This is not a release: pending waiters observe an
    /// <see cref="OperationCanceledException"/> rather than completing successfully — call
    /// <see cref="Set"/> to release waiters. Waiters already released by a prior
    /// <see cref="Set"/> are unaffected. Idempotent and thread-safe.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _signal.TrySetCanceled();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static TaskCompletionSource CreateSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
