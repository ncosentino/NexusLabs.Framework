namespace NexusLabs.Framework.IO;

/// <summary>
/// Executes a delete operation, optionally wrapping it in a caller-supplied resilience policy
/// (retry, backoff, timeout, jitter, ...). The signature deliberately matches the common
/// <c>Execute(operation, cancellationToken)</c> shape used by resilience pipelines, so a policy's
/// execute method can be supplied directly as a method group with no adapter code:
/// <code>
/// var options = new TemporaryDirectoryOptions { DeleteExecutor = myResiliencePolicy.ExecuteAsync };
/// </code>
/// </summary>
/// <remarks>
/// When no executor is supplied the delete is attempted exactly once. Any resilience mechanism
/// works equally well — supply a custom lambda such as <c>(op, ct) =&gt; op(ct)</c> for a single
/// attempt, or your own retry loop.
/// </remarks>
/// <param name="operation">The delete operation to execute (and potentially retry).</param>
/// <param name="cancellationToken">A token to observe while executing the operation.</param>
/// <returns>A task that completes when the operation and any retries finish.</returns>
public delegate ValueTask ResilientDeleteExecutor(
    Func<CancellationToken, ValueTask> operation,
    CancellationToken cancellationToken);
